using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Service;

namespace QuadroschrauberSharp
{
    public class RaspiCam : IStreamWriter
    {
        Process process;
        Task readtask;

        public RaspiCam()
        {
            Start();
        }

        public void Start()
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo("raspivid", "-w 640 -h 270 -t 100000 -o -")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            //Task.Factory.StartNew(ReadTask, TaskCreationOptions.LongRunning);
            process.Start();
        }

        void ReadTask()
        {
            byte[] buffer = new byte[4096];
            int i;
            int count = 0;
            while ((i = process.StandardOutput.Read()) != -1)
            {
                byte b = (byte)i;
                buffer[count++] = b;
                if (count == buffer.Length)
                {
                    BufferReady(buffer);
                    count = 0;
                }
            }
        }

        private void BufferReady(byte[] buffer)
        {
        }

        public void WriteTo(Stream responseStream)
        {
            byte[] buffer = new byte[4096];
            int i;
            int count = 0;
            while ((i = process.StandardOutput.Read()) != -1)
            {
                byte b = (byte)i;
                buffer[count++] = b;
                if (count == buffer.Length)
                {
                    responseStream.Write(buffer, 0, count);
                    count = 0;
                }
            }
        }
    }

    public class H264
    {
        //FileStream fs = new FileStream("c:\\temp\\test.h264", FileMode.Open);
        //FileStream fs = new FileStream("c:\\temp\\mozilla_story.264", FileMode.Open);
        FileStream fs = new FileStream("c:\\temp\\test.264", FileMode.Open);
        public H264()
        {
            /*int i = 0;
            while (true)
            {
                byte[] data = FindNAL();
                if (data.Length > 0)
                {
                    Console.WriteLine(i++.ToString() + " " + data.Length + " " + data[0]);
                }
            }
            fs.Seek(0, SeekOrigin.Begin);*/
        }

        public byte[] ReadNAL()
        {
            byte[] data;
            //for (data = FindNAL(); (data[3] != 37/*keyframe*/ && data[3] != 33) || data.Length < 10000; data = FindNAL()) ;
            for (data = FindNAL(); data.Length == 0; data = FindNAL()) ;
            return data;
        }

        protected byte[] FindNAL()
        {
            byte[] buffer = new byte[4];
            fs.Read(buffer, 0, 4);
            List<byte> data = new List<byte>();
            //List<byte> data = new List<byte>(new byte[] { 0, 0, 1 });

            while (buffer[0] != 0 || buffer[1] != 0 || buffer[2] != 0 || buffer[3] != 1)
            {
                data.Add(buffer[0]);
                buffer[0] = buffer[1];
                buffer[1] = buffer[2];
                buffer[2] = buffer[3];
                fs.Read(buffer, 3, 1);
            }

            byte[] buffer2 = data.ToArray();
            for (int i = 0; i < buffer2.Length - 2; ++i)
            {
                if (buffer2[i + 0] == 0 && buffer2[i + 1] == 0 && (buffer2[i + 2] == 0 || buffer2[i + 2] == 1))
                {
                    throw new Exception("BUG");
                }
            }
            return buffer2;
        }
    }
}
