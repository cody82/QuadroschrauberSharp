using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuadroschrauberSharp.Hardware;
using System.Threading;

namespace QuadroschrauberSharp
{
    class MultiWiiSchrauber : MultiWiiSharp.MultiWii
    {
        public MultiWiiSchrauber(string port, Action<MultiWiiSharp.MultiWiiMessage> onmessage)
            :base(port, onmessage)
        {
        }

        public void Control(ControlRequest control)
        {
            SendRC(1000 + (int)(control.yaw * 1000), 
                1000 + (int)(control.pitch * 1000), 
                1000 + (int)(control.roll * 1000), 
                1000 + (int)(control.throttle * 1000));
        }
    }

    class Program
    {
        public static MultiWiiSchrauber MW;
        static void Main(string[] args)
        {
            Quadroschrauber quadroschrauber = new Quadroschrauber();

            Webservice service = new Webservice();

            quadroschrauber.service = service;

            if (false)
            {
                quadroschrauber.Init();

                quadroschrauber.Run();
            }
            else if (false)
            {
                H264 h264 = new H264();
                while (true)
                {
                    Console.WriteLine(">");
                    Console.ReadLine();
                    byte[] data = h264.ReadNAL();
                    foreach (var s in service.WebSocket.GetAllSessions())
                    {
                        s.Send(new ArraySegment<byte>(data));
                    }
                }
            }
            else
            {
                MultiWiiSchrauber mw = new MultiWiiSchrauber("COM3", a => {

                    if (a is MultiWiiSharp.RawImuMessage)
                    {
                        MultiWiiSharp.RawImuMessage imu = (MultiWiiSharp.RawImuMessage)a;
                        var sessions = service.WebSocket.GetAllSessions();
                        if (sessions.Any())
                        {
                            var t = new Telemetry()
                            {
                                Ticks = Environment.TickCount,
                                AccelX = imu.Acc[0],
                                AccelY = imu.Acc[1],
                                AccelZ = imu.Acc[2],
                                GyroX = imu.Gyro[0],
                                GyroY = imu.Gyro[1],
                                GyroZ = imu.Gyro[2],
                                Hz = 0,
                                MotorFront = 0,
                                MotorBack = 0,
                                MotorLeft = 0,
                                MotorRight = 0,
                                Load = 0,
                                RemoteActive = false,
                                RemotePitch = 0,
                                RemoteRoll = 0,
                                RemoteThrottle = 0,
                                RemoteYaw = 0,
                                MinFrameTime = 0,
                                MaxFrameTime = 0,
                                GC0 = GC.CollectionCount(0),
                                GC1 = GC.CollectionCount(1)
                            };

                            string data = service.WebSocket.JsonSerialize(t);

                            foreach (var s in sessions)
                            {
                                s.Send(data);
                            }
                        }
                    }
                });
                mw.Open();
                MW = mw;
                while (true)
                {
                    string line = Console.ReadLine();
                    mw.SendIdent();
                }
            }

        }
    }
}
