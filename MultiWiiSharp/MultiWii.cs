using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiWiiSharp
{
    public enum MultiWiiMessageType
    {
        MSP_IDENT = 100,
        MSP_STATUS = 101,
        MSP_RAW_IMU = 102,
        MSP_SERVO = 103,
        MSP_MOTOR = 104,
        MSP_RC = 105,
        MSP_RAW_GPS = 106,
        MSP_COMP_GPS = 107,
        MSP_ATTITUDE = 108,
        MSP_ALTITUDE = 109,
        MSP_BAT = 110,
        MSP_RC_TUNING = 111,
        MSP_PID = 112,
        MSP_BOX = 113,
        MSP_MISC = 114,
        MSP_MOTOR_PINS = 115,
        MSP_BOXNAMES = 116,
        MSP_PIDNAMES = 117,

        MSP_SET_RAW_RC = 200,
        MSP_SET_RAW_GPS = 201,
        MSP_SET_PID = 202,
        MSP_SET_BOX = 203,
        MSP_SET_RC_TUNING = 204,
        MSP_ACC_CALIBRATION = 205,
        MSP_MAG_CALIBRATION = 206,
        MSP_SET_MISC = 207,
        MSP_RESET_CONF = 208,
        MSP_SELECT_SETTING = 210,

        MSP_BIND = 240,

        MSP_EEPROM_WRITE = 250,

        MSP_DEBUGMSG = 253,
        MSP_DEBUG = 254,

        // Additional baseflight commands that are not compatible with MultiWii
        MSP_UID = 160,
        MSP_ACC_TRIM = 240,
        MSP_SET_ACC_TRIM = 239,
        MSP_GPSSVINFO = 164 // get Signal Strength (only U-Blox)
    }

    public class MultiWiiMessage
    {
        public MultiWiiMessage(MultiWiiMessageType type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public MultiWiiMessageType Type { get; set; }
        public byte[] Data { get; set; }
    }

    public class RawImuMessage : MultiWiiMessage
    {
        public RawImuMessage(byte[] data)
            : base(MultiWiiMessageType.MSP_RAW_IMU, data)
        {
            for (int i = 0; i < 3; ++i)
            {
                Acc[i] = BitConverter.ToInt16(data, i * 2);
                Gyro[i] = BitConverter.ToInt16(data, 6 + i * 2);
                Mag[i] = BitConverter.ToInt16(data, 12 + i * 2);
            }
        }

        public short[] Acc = new short[3];
        public short[] Gyro = new short[3];
        public short[] Mag = new short[3];
    }

    public class BatMessage : MultiWiiMessage
    {
        public BatMessage(byte[] data)
            : base(MultiWiiMessageType.MSP_BAT, data)
        {
            VBat = data[0];
            Power = BitConverter.ToInt16(data, 1);
        }

        public int VBat;
        public int Power;

        public decimal Volts
        {
            get
            {
                return VBat * 0.1m;
            }
        }

        public override string ToString()
        {
            return Volts.ToString() + "V";
        }
    }

    public class MultiWii
    {
        SerialPort port;
        string portname;
        Task readtask;
        Task writetask;
        object writelock = new object();
        Action<MultiWiiMessage> onmessage;

        public MultiWii(string port, Action<MultiWiiMessage> onmessage)
        {
            this.port = new SerialPort(port, 115200);
            portname = port;
            this.onmessage = onmessage;
        }

        public void Open()
        {
            port.Open();
            StartReadTask();
            StartWriteTask();
            SendIdent();
        }

        byte ReadByte()
        {
            int b;

            b = port.ReadByte();

            if (b == -1)
                throw new Exception("EOF");

            return (byte)b;
        }

        MultiWiiMessage ReadMessage()
        {
            byte b;
            while (true)
            {
                b = ReadByte();
                if (b != (byte)'$')
                    continue;

                b = ReadByte();
                if (b != (byte)'M')
                    continue;

                b = ReadByte();
                char dir = (char)b;
                if (dir != '<' && dir != '>' && dir != '!')
                    continue;

                if (dir == '!')
                    Console.WriteLine("!");

                int checksum = 0;
                int length = ReadByte();
                MultiWiiMessageType type = (MultiWiiMessageType)ReadByte();
                byte[] data = new byte[length];
                checksum ^= data.Length;
                checksum ^= (int)type;
                for(int i=0;i<length;++i)
                {
                    data[i] = ReadByte();
                    checksum ^= data[i];
                }
                int checksum2 = ReadByte();
                if (checksum2 != checksum)
                    continue;

                Console.WriteLine(dir + " " + type.ToString() + " " + (data.Any() ? (data.Select(x => x.ToString("00")).Aggregate((a, b2) => a + " " + b2)) : ""));

                if (type == MultiWiiMessageType.MSP_RAW_IMU)
                    return new RawImuMessage(data);
                if (type == MultiWiiMessageType.MSP_BAT)
                    return new BatMessage(data);
                
                return new MultiWiiMessage(type, data);
            }
        }

        void StartReadTask()
        {
            readtask = Task.Factory.StartNew(new Action(() =>
            {
                while (true)
                {
                    MultiWiiMessage msg = ReadMessage();
                    Console.WriteLine(msg.ToString());
                    onmessage(msg);
                }

                }), TaskCreationOptions.LongRunning);
        }

        void StartWriteTask()
        {
            writetask = Task.Factory.StartNew(new Action(() =>
            {
                int i = 0;
                while (true)
                {
                    lock (writelock)
                    {
                        Send(MultiWiiMessageType.MSP_RAW_IMU, new byte[] { });
                        if (i++ % 10 == 0)
                        {
                            Send(MultiWiiMessageType.MSP_BAT, new byte[] { });
                        }
                    }
                    Thread.Sleep(100);
                }
            }), TaskCreationOptions.LongRunning);
        }

        public void SendIdent()
        {
            Send(MultiWiiMessageType.MSP_IDENT, new byte[] { });
        }

        public void Calibrate()
        {
        }

        public void SendRC(int yaw, int pitch, int roll, int throttle)
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

            Send(MultiWiiMessageType.MSP_SET_RAW_RC, data);
        }

        public void Arm()
        {
            SendRC(1500, 1500, 1500, 1000);
            Thread.Sleep(100);
            SendRC(2000, 1500, 1500, 1000);
            Thread.Sleep(500);
            SendRC(1500, 1500, 1500, 1000);
        }

        public void Disarm()
        {
            SendRC(1500, 1500, 1500, 1000);
            Thread.Sleep(100);
            SendRC(1000, 1500, 1500, 1000);
            Thread.Sleep(500);
            SendRC(1500, 1500, 1500, 1000);
        }

        public void Send(MultiWiiMessageType type, byte[] data)
        {
            lock (writelock)
            {
                int checksum = 0;
                port.Write("$M<");
                port.Write(new byte[] { (byte)data.Length, (byte)type }, 0, 2);
                port.Write(data, 0, data.Length);
                checksum ^= data.Length;
                checksum ^= (int)type;
                foreach (byte b in data)
                    checksum ^= b;
                port.Write(new byte[] { (byte)checksum }, 0, 1);
            }
        }
    }
}
