using Google.Protobuf;
using Newtonsoft.Json;
using Proto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.NoSQL;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace WhatsSocket.Core.Stores
{


    public class SessionStore
    {
        public EventEmitter Ev { get; }
        [JsonIgnore]
        public AuthenticationCreds? Creds { get; set; }

        private string _sessions;
        public SessionStore(string path, AuthenticationCreds? creds, EventEmitter ev)
        {
            Creds = creds;
            Sessions = new Dictionary<string, SessionRecord>();
            _sessions = Path.Combine(path, "data", "sessions");
            Directory.CreateDirectory(_sessions);
            var files = Directory.GetFiles(_sessions);
            foreach (var item in files)
            {
                var file = new FileInfo(item);
                var id = file.Name.Replace(".json", "");
                Sessions[id] = JsonConvert.DeserializeObject<SessionRecord>(File.ReadAllText(item));

            }
            Ev = ev;

        }

        public Dictionary<string, SessionRecord> Sessions { get; set; }


    }
}
