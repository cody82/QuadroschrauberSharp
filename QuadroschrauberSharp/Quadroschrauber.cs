using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuadroschrauberSharp.Hardware;

namespace QuadroschrauberSharp
{
    public class Quadroschrauber
    {
        public static Quadroschrauber Instance;

        public Motor MotorFront;
        public Motor MotorBack;
        public Motor MotorLeft;
        public Motor MotorRight;

        public I2C I2C;
        public MPU6050 mpu;
        public IMU_MPU6050 imu;
        public Controller Controller = new Controller();

        public Spektrum Remote;

        public Quadroschrauber()
        {
            Instance = this;
        }

        public void Init()
        {
            I2C = new I2C(1);

            MotorFront = new MotorServoBlaster(0);
            MotorBack = new MotorServoBlaster(3);
            MotorLeft = new MotorServoBlaster(1);
            MotorRight = new MotorServoBlaster(2);

            Remote = new Spektrum();

            mpu = new MPU6050(I2C, 0x69);
            imu = new IMU_MPU6050(mpu);
            imu.Init(false);
        }
        

        public int Hz;
        public void Run()
        {
            int lastticks = Environment.TickCount;
            int frames = 0;
            int lastframes = lastticks;
            while (true)
            {
                int ticks = Environment.TickCount;
                int delta = ticks - lastticks;
                if (delta > 0)
                {
                    Tick(delta * 1000);
                    frames++;
                    if (ticks - lastframes > 1000)
                    {
                        lastframes = ticks;
                        Console.WriteLine("FPS: " + frames);
                        Hz = frames;
                        frames = 0;
                    }
                }
                lastticks = ticks;
                Thread.Sleep(1);
            }
        }

        public float GetSystemLoad()
        {
            using (StreamReader rs = new StreamReader("/proc/loadavg"))
            {
                string s = rs.ReadToEnd();
                return float.Parse(s.Split(' ')[0]);
            }
        }


        public Queue<Telemetry> SensorQueue = new Queue<Telemetry>(101);
        int queuecounter = 0;
        public Webservice service;
        long framecounter = 0;

        public void Tick(int microseconds)
        {
            float dtime = (float)microseconds / 1000000.0f;
            imu.Update(dtime);
            GetSensorData(dtime, SensorInput);
            if (framecounter++ == 100)
                imu.Calibrate();

            if (Remote.Input.active)
            {
                // RC has higher priority than web-interface
                Controller.Update(dtime, Remote.Input, SensorInput, MotorOutput);
            }
            else
            {
                Controller.Update(dtime, control, SensorInput, MotorOutput);
            }
            SetMotors(MotorOutput);

            queuecounter += microseconds;



            if (queuecounter >= 50000)
            {
                queuecounter %= 50000;


                var t = new Telemetry()
                {
                    Ticks = Environment.TickCount,
                    AccelX = SensorInput.accel.x,
                    AccelY = SensorInput.accel.y,
                    AccelZ = SensorInput.accel.z,
                    GyroX = SensorInput.gyro.x,
                    GyroY = SensorInput.gyro.y,
                    GyroZ = SensorInput.gyro.z,
                    Hz = Hz,
                    MotorFront = MotorOutput.motor_front,
                    MotorBack = MotorOutput.motor_back,
                    MotorLeft = MotorOutput.motor_left,
                    MotorRight = MotorOutput.motor_right,
                    Load = GetSystemLoad(),
                    RemoteActive = Remote.Input.active,
                    RemotePitch = Remote.Input.pitch,
                    RemoteRoll = Remote.Input.roll,
                    RemoteThrottle = Remote.Input.throttle,
                    RemoteYaw = Remote.Input.yaw
                };
                var sessions = service.WebSocket.GetAllSessions();
                if (sessions.Any())
                {
                    string data = service.WebSocket.JsonSerialize(t);

                    foreach (var s in sessions)
                    {
                        s.Send(data);
                    }
                }


                lock (SensorQueue)
                {
                    SensorQueue.Enqueue(t);

                    if (SensorQueue.Count > 101)
                        SensorQueue.Dequeue();
                }
            }
        }

        ControlRequest control = new ControlRequest();
        MotorOutput MotorOutput = new MotorOutput();
        SensorInput SensorInput = new SensorInput();

        void SetMotors(MotorOutput output)
        {
            MotorBack.Set(MotorOutput.motor_back);
            MotorFront.Set(MotorOutput.motor_front);
            MotorLeft.Set(MotorOutput.motor_left);
            MotorRight.Set(MotorOutput.motor_right);
        }

        void GetSensorData(float dtime, SensorInput output)
        {
            output.accel = imu.GetAccel();
            output.gyro = imu.GetGyro();
        }

        public void Control(ControlRequest request)
        {
            request.active = true;
            control = request;
        }

        public void Calibrate()
        {
            imu.Calibrate();
        }
    }
}
