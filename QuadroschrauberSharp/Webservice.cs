using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using SuperWebSocket;


namespace QuadroschrauberSharp
{
    [Route("/telemetry/{Start}")]
    public class TelemetryRequest : IReturn<TelemetryList>
    {
        public long Start { get; set; }
    }

    [Authenticate]
    [Route("/control/{Throttle}")]
    public class ControlRequest : RemoteInput, IReturn<ControlResult>
    {
        //public float Throttle { get; set; }
    }

    [Route("/calibrate/")]
    public class CalibrationRequest : IReturnVoid
    {
    }

    public class ControlResult
    {
    }

    public class Telemetry
    {
        //public string Type { get { return "Telemetry"; } set { } }
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
        public float Load { get; set; }
        public float RemoteYaw { get; set; }
        public float RemotePitch { get; set; }
        public float RemoteRoll { get; set; }
        public float RemoteThrottle { get; set; }
        public bool RemoteActive { get; set; }
        public int MinFrameTime { get; set; }
        public int MaxFrameTime { get; set; }
        public int GC0 { get; set; }
        public int GC1 { get; set; }
    }

    [Authenticate]
    [Route("/setconfig/")]
    public class ControllerConfig : IReturn<ControllerConfig>
    {
        //public string Type { get{return "ControllerConfig";} set {} }
        public long Ticks { get; set; }
        public float GainX { get; set; }
        public float GainY { get; set; }
        public float GainZ { get; set; }
        public float RemoteGainX { get; set; }
        public float RemoteGainY { get; set; }
        public float RemoteGainZ { get; set; }
        public float InnerPX { get; set; }
        public float InnerPY { get; set; }
        public float InnerPZ { get; set; }
        public float InnerIX { get; set; }
        public float InnerIY { get; set; }
        public float InnerIZ { get; set; }
        public float InnerImaxX { get; set; }
        public float InnerImaxY { get; set; }
        public float InnerImaxZ { get; set; }
        public float InnerDX { get; set; }
        public float InnerDY { get; set; }
        public float InnerDZ { get; set; }
        public float OuterPX { get; set; }
        public float OuterPY { get; set; }
        public float OuterPZ { get; set; }
        public float OuterIX { get; set; }
        public float OuterIY { get; set; }
        public float OuterIZ { get; set; }
        public float OuterImaxX { get; set; }
        public float OuterImaxY { get; set; }
        public float OuterImaxZ { get; set; }
        public float OuterDX { get; set; }
        public float OuterDY { get; set; }
        public float OuterDZ { get; set; }

        public static ControllerConfig Load(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string json = sr.ReadToEnd();
                return (ControllerConfig)JsonSerializer.DeserializeFromString(json, typeof(ControllerConfig));
            }
        }

        public void Save(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                JsonSerializer.SerializeToWriter(this, sw);
            }
        }

        public static ControllerConfig Get(Quadroschrauber q)
        {
            Controller c = q.Controller;

            return new ControllerConfig()
            {
                Ticks = Environment.TickCount,
                GainX = c.gain.x,
                GainY = c.gain.y,
                GainZ = c.gain.z,
                RemoteGainX = c.remote_gain.x,
                RemoteGainY = c.remote_gain.y,
                RemoteGainZ = c.remote_gain.z,
                InnerPX = c.inner.P.x,
                InnerPY = c.inner.P.y,
                InnerPZ = c.inner.P.z,
                InnerIX = c.inner.I.x,
                InnerIY = c.inner.I.y,
                InnerIZ = c.inner.I.z,
                InnerImaxX = c.inner.I_max.x,
                InnerImaxY = c.inner.I_max.y,
                InnerImaxZ = c.inner.I_max.z,
                InnerDX = c.inner.D.x,
                InnerDY = c.inner.D.y,
                InnerDZ = c.inner.D.z,
                OuterPX = c.outer.P.x,
                OuterPY = c.outer.P.y,
                OuterPZ = c.outer.P.z,
                OuterIX = c.outer.I.x,
                OuterIY = c.outer.I.y,
                OuterIZ = c.outer.I.z,
                OuterImaxX = c.outer.I_max.x,
                OuterImaxY = c.outer.I_max.y,
                OuterImaxZ = c.outer.I_max.z,
                OuterDX = c.outer.D.x,
                OuterDY = c.outer.D.y,
                OuterDZ = c.outer.D.z
            };
        }

        public void Set(Quadroschrauber q)
        {
            Controller c = q.Controller;

            c.gain.x = GainX;
            c.gain.y = GainY;
            c.gain.z = GainZ;
            c.remote_gain.x = RemoteGainX;
            c.remote_gain.y = RemoteGainY;
            c.remote_gain.z = RemoteGainZ;
            c.inner.P.x = InnerPX;
            c.inner.P.y = InnerPY;
            c.inner.P.z = InnerPZ;
            c.inner.I.x = InnerIX;
            c.inner.I.y = InnerIY;
            c.inner.I.z = InnerIZ;
            c.inner.I_max.x = InnerImaxX;
            c.inner.I_max.y = InnerImaxY;
            c.inner.I_max.z = InnerImaxZ;
            c.inner.D.x = InnerDX;
            c.inner.D.y = InnerDY;
            c.inner.D.z = InnerDZ;
            c.outer.P.x = OuterPX;
            c.outer.P.y = OuterPY;
            c.outer.P.z = OuterPZ;
            c.outer.I.x = OuterIX;
            c.outer.I.y = OuterIY;
            c.outer.I.z = OuterIZ;
            c.outer.I_max.x = OuterImaxX;
            c.outer.I_max.y = OuterImaxY;
            c.outer.I_max.z = OuterImaxZ;
            c.outer.D.x = OuterDX;
            c.outer.D.y = OuterDY;
            c.outer.D.z = OuterDZ;

        }
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

    [Route("/config/")]
    public class ConfigRequest : IReturn<ControllerConfig>
    {
    }

    [Route("/camera/")]
    public class CameraRequest : IReturn<IStreamWriter>
    {
    }

    public class ConfigService : Service
    {
        public ControllerConfig Get(ConfigRequest request)
        {
            Quadroschrauber q = Quadroschrauber.Instance;

            return ControllerConfig.Get(q);
        }

        public ControllerConfig Post(ControllerConfig request)
        {
            Quadroschrauber q = Quadroschrauber.Instance;

            request.Set(q);

            request.Save("quadroschrauber_controller.json");

            return ControllerConfig.Get(q);
        }

        public void Get(CalibrationRequest request)
        {
            Quadroschrauber q = Quadroschrauber.Instance;

            q.Calibrate();
        }
    }

    public class CameraService : Service
    {
        public IStreamWriter Get(CameraRequest request)
        {
            return new RaspiCam();
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
        InMemoryAuthRepository userRep;
        public MemoryCacheClient CacheClient;
        public AuthFeature AuthFeature;
        public List<AuthUserSession> Sessions = new List<AuthUserSession>();

        public AppHost()
            : base("Quadroschrauber ServiceStack", typeof(AppHost).Assembly) { }
        public override void Configure(Funq.Container container)
        {
            Plugins.Add(AuthFeature = new AuthFeature(() => { var s = new AuthUserSession(); Sessions.Add(s); return s; },
                new IAuthProvider[] { new BasicAuthProvider(), new CredentialsAuthProvider() }));

            container.Register<ICacheClient>(CacheClient = new MemoryCacheClient());
            userRep = new InMemoryAuthRepository();
            container.Register<IUserAuthRepository>(userRep);

            userRep.Clear();
            CreateUser(1, "cody", "cody@l33t.de", "cody");

            Routes
                .Add<TelemetryRequest>("/telemetry/{Start}")
                .Add<TelemetryRequest>("/control/{Throttle}");
        }


        private void CreateUser(int id, string username, string email, string password, List<string> roles = null, List<string> permissions = null)
        {
            string hash;
            string salt;
            new SaltedHash().GetHashAndSaltString(password, out hash, out salt);

            userRep.CreateUserAuth(new UserAuth
            {
                Id = id,
                DisplayName = "DisplayName",
                Email = email,
                UserName = username,
                FirstName = "FirstName",
                LastName = "LastName",
                PasswordHash = hash,
                Salt = salt,
                Roles = roles,
                Permissions = permissions
            }, password);
        }
    }

    public class Webservice
    {
        public WebSocketServer WebSocket;
        public AppHost appHost;
        public Webservice()
        {
            appHost = new AppHost();
            appHost.Init();
            try
            {
                appHost.Start("http://*:8080/");
            }
            catch
            {
                appHost.Dispose();
                appHost = new AppHost();
                appHost.Init();
                appHost.Start("http://localhost:8080/");
            }

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
            WebSocket.NewSessionConnected += WebSocket_NewSessionConnected;
        }

        void WebSocket_NewSessionConnected(WebSocketSession session)
        {
            Console.WriteLine("Websocket connected: " + session.RemoteEndPoint);
            Console.WriteLine("Cookies: ");
            string ss_id = null;
            string ss_pid = null;
            foreach (string s in session.Cookies.Keys)
            {
                string s2 = session.Cookies[s];
                Console.WriteLine(s + ": " + s2);
                if (s == "ss-id")
                    ss_id = s2;
                else if (s == "ss-pid")
                    ss_pid = s2;
            }

            bool auth = false;
            foreach (var s in appHost.Sessions)
            {
                //Console.WriteLine("Id: " + s.Id);
                //Console.WriteLine("UserAuthId: " + s.UserAuthId);
                if (s.Id == ss_id)
                {
                    auth = true;
                }
            }

            Console.WriteLine("Path: " + session.Path);
            Console.WriteLine("Origin: " + session.Origin);
            Console.WriteLine("SessionID: " + session.SessionID);
            Console.WriteLine("CurrentToken: " + session.CurrentToken);
            Console.WriteLine("Host: " + session.Host);

            if (!auth && session.Path != "/")
            {
                Console.WriteLine("Auth failed -> disconnecting Websocket.");
                session.CloseWithHandshake("Unauthorized");
            }
        }

        void WebSocket_NewMessageReceived(WebSocketSession session, string value)
        {
            ControlRequest control = (ControlRequest)WebSocket.JsonDeserialize(value, typeof(ControlRequest));
            Quadroschrauber.Instance.Control(control);
        }
    }
}
