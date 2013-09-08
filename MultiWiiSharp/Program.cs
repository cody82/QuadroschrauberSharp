using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace MultiWiiSharp
{
    class Program
    {
            static SerialPort port = new SerialPort("COM3", 115200);
            static void Send(byte op, byte[] data)
            {
                int checksum = 0;
                port.Write("$M<");
                port.Write(new byte[] { (byte)data.Length, op }, 0, 2);
                port.Write(data, 0, data.Length);
                checksum ^= data.Length;
                checksum ^= op;
                foreach (byte b in data)
                    checksum ^= b;
                port.Write(new byte[] { (byte)checksum }, 0, 1);
            }

            static byte MSP_SET_RAW_RC = 200;
            static byte MSP_IDENT = 100;

            static void Arm()
            {
                SendRC(1500, 1500, 1500, 1000);
                Thread.Sleep(100);
                SendRC(2000, 1500, 1500, 1000);
                Thread.Sleep(500);
                SendRC(1500, 1500, 1500, 1000);
            }

            static void Disarm()
            {
                SendRC(1500, 1500, 1500, 1000);
                Thread.Sleep(100);
                SendRC(1000, 1500, 1500, 1000);
                Thread.Sleep(500);
                SendRC(1500, 1500, 1500, 1000);
            }

            static void SendRC(int yaw, int pitch, int roll, int throttle)
            {
                byte[] data = new byte[16];
                data[0] = (byte)(roll & 0xFF);
                data[1] = (byte)((roll >> 8) & 0xFF);
                data[2] = (byte)(pitch & 0xFF);
                data[3] = (byte)((pitch >> 8) & 0xFF);
                data[4] = (byte)(yaw & 0xFF);
                data[5] = (byte)((yaw >> 8) & 0xFF);
                data[6] = (byte)(throttle & 0xFF);
                data[7] = (byte)((throttle >> 8) & 0xFF);

                Send(MSP_SET_RAW_RC, data);
            }
            static void SendIdent()
            {
                Send(MSP_IDENT, new byte[]{});
            }

        static void Main(string[] args)
        {
            port.Open();
            int i;

            Arm();
            Thread.Sleep(2000);
            Disarm();
            while ((i = port.ReadByte()) != -1)
            {
                Console.WriteLine(i);
            }
        }
    }
}
