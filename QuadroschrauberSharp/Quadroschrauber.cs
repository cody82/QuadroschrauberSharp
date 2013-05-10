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
        public Controller Controller = new Controller();

        public Spektrum Remote;

        public Quadroschrauber()
        {
            Instance = this;
        }

        public void Init()
        {
            I2C = new I2C(1);

            MotorFront = new MotorServoBlaster(4);
            MotorBack = new MotorServoBlaster(7);
            MotorLeft = new MotorServoBlaster(5);
            MotorRight = new MotorServoBlaster(6);

            Remote = new Spektrum();

            mpu = new MPU6050(I2C, 0x69);

            Console.WriteLine("Initializing I2C devices...\n");
            mpu.initialize();

            
            // verify connection
            Console.WriteLine("Testing device connections...\n");
            Console.WriteLine(mpu.testConnection() ? "MPU6050 connection successful\n" : "MPU6050 connection failed\n");


            
            // load and configure the DMP
            Console.WriteLine("Initializing DMP...");
            devStatus = mpu.dmpInitialize();
            // make sure it worked (returns 0 if so)
            if (devStatus == 0)
            {
                // turn on the DMP, now that it's ready
                Console.WriteLine("Enabling DMP...");
                mpu.setDMPEnabled(true);

                // enable Arduino interrupt detection
                //Serial.println(F("Enabling interrupt detection (Arduino external interrupt 0)..."));
                //attachInterrupt(0, dmpDataReady, RISING);
                mpuIntStatus = mpu.getIntStatus();

                // set our DMP Ready flag so the main loop() function knows it's okay to use it
                Console.WriteLine("DMP ready!");
                dmpReady = true;

                // get expected DMP packet size for later comparison
                packetSize = mpu.dmpGetFIFOPacketSize();
            }
            else
            {
                // ERROR!
                // 1 = initial memory load failed
                // 2 = DMP configuration updates failed
                // (if it's going to break, usually the code will be 1)
                Console.WriteLine("DMP Initialization failed (code %d)", devStatus);
            }

            Console.WriteLine("Full Accel Range: " + mpu.getFullScaleAccelRange());
            Console.WriteLine("Full Gyro Range: " + mpu.getFullScaleGyroRange());
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

        bool dmpReady = false; // set true if DMP init was successful
byte mpuIntStatus; // holds actual interrupt status byte from MPU
byte devStatus; // return status after each device operation (0 = success, !0 = error)
ushort packetSize; // expected DMP packet size (default is 42 bytes)
ushort fifoCount; // count of all bytes currently in FIFO
byte[] fifoBuffer = new byte[64]; // FIFO storage buffer

    public Queue<Telemetry> SensorQueue = new Queue<Telemetry>(101);
    int queuecounter = 0;
    public Webservice service;
        public void Tick(int microseconds)
        {
            float dtime = (float)microseconds / 1000000.0f;
            var m = mpu.getMotion6();
            GetSensorData(dtime, m, SensorInput);
            
            Controller.Update(dtime, Remote.Input, SensorInput, MotorOutput);
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

            // these methods (and a few others) are also available
            //accelgyro.getAcceleration(&ax, &ay, &az);
            //accelgyro.getRotation(&gx, &gy, &gz);

            // display accel/gyro x/y/z values
            //Console.WriteLine("a/g: {0} {1} {2} {3} {4} {5}", m.ax, m.ay, m.az, m.gx, m.gy, m.gz);




            ushort fifoCount = mpu.getFIFOCount();

            if (fifoCount == 1024)
            {
                // reset so we can continue cleanly
                mpu.resetFIFO();
                Console.WriteLine("FIFO overflow!");

                // otherwise, check for DMP data ready interrupt (this should happen frequently)
            }
            else if (fifoCount >= 42)
            {
                // read a packet from FIFO
                //Console.WriteLine("fifo read: " + packetSize + "/" + fifoCount);
                mpu.getFIFOBytes(fifoBuffer, (byte)packetSize);

                Quaternion q = mpu.dmpGetQuaternion(fifoBuffer);
                VectorFloat gravity = mpu.dmpGetGravity(q);
                float[] ypr = new float[3];
                mpu.dmpGetYawPitchRoll(ypr, q, gravity);
                //Console.WriteLine("quat {0} {1} {2} {3} ", q.w, q.x, q.y, q.z);
                //Console.WriteLine("ypr {0} {1} {2} ", ypr[0] * 180 / Math.PI, ypr[1] * 180 / Math.PI, ypr[2] * 180 / Math.PI);


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
        void GetSensorData(float dtime, MPU6050.Motion6 motion, SensorInput output)
        {
            const float accel_factor = 1.0f / 16384.0f;
            output.accel = new VectorFloat(motion.ax, motion.ay, -motion.az) * accel_factor;
            const float gyro_factor = -(1.0f / 32768.0f * 2000.0f / 180.0f * (float)Math.PI);
            output.gyro = new VectorFloat(motion.gx, motion.gy, motion.gz) * gyro_factor;
        }

        internal void Control(ControlRequest request)
        {
            control = request;
        }
    }
}
