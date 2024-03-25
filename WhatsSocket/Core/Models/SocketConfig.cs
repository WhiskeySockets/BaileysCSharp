using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Signal;
using WhatsSocket.Core.Stores;

namespace WhatsSocket.Core.Models
{
    public class SocketConfig
    {
        public string? ID { get; set; }
        public SocketConfig()
        {
            Logger = new Logger();
            Logger.Level = LogLevel.Verbose;
            AppStateMacVerification = new AppStateMacVerification();
            ConnectTimeoutMs = 20000;
            KeepAliveIntervalMs = 30000;
            DefaultQueryTimeoutMs = 60000;
            MarkOnlineOnConnect = true;
            FireInitQueries = true;
        }

        public int ConnectTimeoutMs { get; set; }
        public int KeepAliveIntervalMs { get; set; }
        public int DefaultQueryTimeoutMs { get; set; }
        public int QrTimeout { get; set; }
        public bool MarkOnlineOnConnect { get; set; }
        public bool FireInitQueries { get; set; }
        public Logger Logger { get; set; }
        public bool Mobile => false;//For Now only multi device api

        public AuthenticationState Auth { get; set; }
        public bool SyncFullHistory { get; set; }

        public AppStateMacVerification AppStateMacVerification { get; set; }

        public bool ShouldSyncHistoryMessage()
        {
            return true;
        }

        public bool ShouldIgnoreJid(string jid = "")
        {
            return false;
        }

        private static string Root
        {
            get
            {
                return Path.GetDirectoryName(typeof(BaseSocket).Assembly.Location);
            }
        }
        public SignalRepository MakeSignalRepository(EventEmitter ev)
        {
            return new SignalRepository(Auth);
        }

        internal MemoryStore MakeStore(EventEmitter ev, Logger logger)
        {
            return new MemoryStore(CacheRoot, ev, logger);
        }

        public string CacheRoot
        {
            get
            {
                var path = Path.Combine(Root, ID);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
    }
}
