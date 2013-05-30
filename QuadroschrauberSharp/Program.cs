using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuadroschrauberSharp.Hardware;

namespace QuadroschrauberSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Quadroschrauber quadroschrauber = new Quadroschrauber();

            Webservice service = new Webservice();

            quadroschrauber.service = service;

            if (true)
            {
                quadroschrauber.Init();

                quadroschrauber.Run();
            }
            else
            {
                H264 h264 = new H264();
                while(true)
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
            Console.ReadLine();

        }
    }
}
