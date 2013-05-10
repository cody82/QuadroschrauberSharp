using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
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
        public float Load { get; set; }
        public float RemoteYaw { get; set; }
        public float RemotePitch { get; set; }
        public float RemoteRoll { get; set; }
        public float RemoteThrottle { get; set; }
        public bool RemoteActive { get; set; }
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
