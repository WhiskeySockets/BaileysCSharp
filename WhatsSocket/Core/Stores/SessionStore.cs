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
        //[JsonIgnore]
        //public KeyStore Keys { get; set; }
        [JsonIgnore]
        public SenderKeyStore SenderKeys { get; }
        [JsonIgnore]
        public AppStateSyncVersionStore AppStateSyncVersionStore { get; }

        [JsonIgnore]

        public AppStateSyncKeyStore AppStateSyncKeyStore { get; }


        public EventEmitter Ev { get; }
        [JsonIgnore]
        public AuthenticationCreds? Creds { get; set; }



        public KeyStore Keys { get; set; }

        private string _sessions;
        public SessionStore(string path, AuthenticationCreds? creds, EventEmitter ev)
        {
            Creds = creds;

            Keys = new KeyStore(path);

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
            SenderKeys = new SenderKeyStore(Path.Combine(path, "data", "sender-keys"), ev);
            //Keys = new KeyStore(Path.Combine(path, "keys"), Config, ev);
            AppStateSyncVersionStore = new AppStateSyncVersionStore(Path.Combine(path, "data", "app-state-sync-version"), ev);
            AppStateSyncKeyStore = new AppStateSyncKeyStore(Path.Combine(path, "data", "app-state-sync-key"), ev);

        }

        public Dictionary<string, SessionRecord> Sessions { get; set; }


        public void Set(string id, SessionRecord? key)
        {
            var file = Path.Combine(_sessions, id + ".json");
            if (key == null)
            {
                Sessions.Remove(id);
                if (File.Exists(file))
                {
                    //File.Copy(file, file + ".used");
                }
            }
            else
            {
                Sessions[id] = key;
                File.WriteAllText(file, JsonConvert.SerializeObject(key, Formatting.Indented));
            }
            Ev.Emit(this);
        }


        internal SessionRecord? Get(string id)
        {
            if (Sessions.ContainsKey(id))
            {
                return Sessions[id];
            }
            return null;
        }

        public bool IsTrustedIdentity(string fqAddr, ByteString identityKey)
        {
            return true;
        }

        internal KeyPair LoadPreKey(uint preKeyId)
        {
            var result = Keys.Get<PreKeyPair>(preKeyId.ToString());
            if (result == null)
                return null;
            return result.Key;
        }

        internal KeyPair LoadSignedPreKey(uint signedPreKeyId)
        {
            return Creds.SignedPreKey.KeyPair;
        }

        internal KeyPair GetOurIdentity()
        {
            return new KeyPair()
            {
                Private = Creds.SignedIdentityKey.Private,
                Public = AuthenticationUtils.GenerateSignalPubKey(Creds.SignedIdentityKey.Public),
            };
        }

        internal void RemovePreKey(uint preKeyId)
        {
            Keys.Set<PreKeyPair>(preKeyId.ToString(), null);
        }

        internal void StoreSenderKey(string senderName, SenderKeyRecord senderMsg)
        {
            SenderKeys.StoreSenderKey(senderName, senderMsg);
        }

        internal SenderKeyRecord LoadSenderKey(string senderName)
        {
            return SenderKeys.Get(senderName);
        }
    }
}
