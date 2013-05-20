using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using ServiceStack.Text;

namespace QuadroschrauberSharp.Hardware
{
    public class Mbed
    {
        SerialPort port;
        Task task;

        public Mbed()
        {
            port = new SerialPort("/dev/ttyACM1");
            port.Open();
            task = Task.Factory.StartNew(ReadTask, TaskCreationOptions.LongRunning);
        }

        void ReadTask()
        {
            string line;
            while ((line = port.ReadLine()) != null)
            {
                JsonObject json = JsonObject.Parse(line);
                /*foreach (var x in json)
                {
                    Console.WriteLine(x.Key + ": " + x.Value);
                }*/
                Received = json;
                ReceivedJsonString = line;
            }
        }

        public JsonObject Received;
        public string ReceivedJsonString;
    }
}
