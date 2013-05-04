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

            quadroschrauber.Init();

            quadroschrauber.Run();
        }
    }
}
