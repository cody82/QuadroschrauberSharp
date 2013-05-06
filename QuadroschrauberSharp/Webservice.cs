using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;


using SuperWebSocket;


namespace QuadroschrauberSharp
{
    [Route("/telemetry/{Start}")]
    public class TelemetryRequest : IReturn<TelemetryList>
    {
        public long Start { get; set; }
    }

    [Route("/control/{Throttle}")]
    public class ControlRequest : IReturn<ControlResult>
    {
        public float Throttle { get; set; }
    }

    public class ControlResult
    {
    }

    public class Telemetry
    {
        public long Ticks { get; set; }
        public float GyroX { get; set; }
        public float GyroY { get; set; }
        public float GyroZ { get; set; }
        public float AccelX { get; set; }
        public float AccelY { get; set; }
        public float AccelZ { get; set; }
        public float MotorFront { get; set; }
        public float MotorBack { get; set; }
        public float MotorLeft { get; set; }
        public float MotorRight { get; set; }
        public int Hz { get; set; }
    }

    public class TelemetryList
    {
        public List<Telemetry> List { get; set; }
    }
    /*
    public class InfoService : IService<TelemetryRequest>
    {
        public object Execute(TelemetryRequest request)
        {
            Quadroschrauber q = Quadroschrauber.Instance;

            Telemetry t = new Telemetry()
            {
            };

            return new HttpResult(t)
            {
                Headers = {
                    { "Access-Control-Allow-Origin", "*" },
                    //{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" } ,
                    //{ "Access-Control-Allow-Headers", "Content-Type" }
            }
            };
        }
    }
    */
    public class TelemetryService : Service
    {
        public TelemetryList Get(TelemetryRequest request)
        {
            Quadroschrauber q = Quadroschrauber.Instance;

            lock (q.SensorQueue)
            {
                TelemetryList t = new TelemetryList()
                {
                    List = q.SensorQueue.Where(x => x.Ticks >= request.Start).ToList()
                };
                return t;
            }
        }
    }

    public class ControlService : Service
    {
        public ControlResult Get(ControlRequest request)
        {
            Quadroschrauber q = Quadroschrauber.Instance;

            q.Control(request);

            return new ControlResult();
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost()
            : base("Quadroschrauber ServiceStack", typeof(AppHost).Assembly) { }
        public override void Configure(Funq.Container container)
        {
            Routes
                .Add<TelemetryRequest>("/telemetry/{Start}")
                .Add<TelemetryRequest>("/control/{Throttle}");
        }
    }

    public class Webservice
    {
        public WebSocketServer WebSocket;
        public Webservice()
        {
            var appHost = new AppHost();
            appHost.Init();
            appHost.Start("http://*:8080/");

            WebSocket = new WebSocketServer();

            //Setup the appServer
            if (!WebSocket.Setup(8081)) //Setup with listening port
            {
                throw new Exception("WebSocketServer cant listen on port " + 8081);
            }
            if (!WebSocket.Start()) //Setup with listening port
            {
                throw new Exception("WebSocketServer cant start");
            }

            WebSocket.NewMessageReceived += WebSocket_NewMessageReceived;
        }

        void WebSocket_NewMessageReceived(WebSocketSession session, string value)
        {
            ControlRequest control = (ControlRequest)WebSocket.JsonDeserialize(value, typeof(ControlRequest));
            Quadroschrauber.Instance.Control(control);
        }
    }
}
