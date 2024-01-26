using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Models.Sessions
{

    public class SessionRecord
    {
        [JsonProperty("_sessions")]
        public Dictionary<string, Session> Sessions { get; set; }


        [JsonProperty("version")]
        public string Version { get; set; }

        public SessionRecord()
        {
            Sessions = new Dictionary<string, Session>();
            Version = "v1";
        }

        internal Session? getSession(string key)
        {
            if (Sessions.ContainsKey(key))
                return Sessions[key];
            return null;
        }

        internal Session? GetOpenSession()
        {
            foreach (var kvp in Sessions)
            {
                if (kvp.Value.IndexInfo.Closed != -1)
                    return kvp.Value;
            }
            return null;
        }

        internal void CloseSession(Session session)
        {
            if (!IsClosed(session))
            {
                session.IndexInfo.Closed = DateTime.UtcNow.AsEpoch();
            }
        }

        public bool IsClosed(Session session)
        {
            if (session == null)
                return false;
            return session.IndexInfo.Closed != -1;
        }

        internal void SetSession(Session session)
        {
            Sessions[session.IndexInfo.BaseKey.ToBase64()] = session;
        }

        internal void RemoveOldSessions()
        {
            if (Sessions.Count > 40)
            {
                var oldestKey = "";
                Session? oldestSession = null;
                foreach (var kvp in Sessions)
                {
                    if (kvp.Value.IndexInfo.Closed != -1)
                    {
                        if (oldestSession == null || kvp.Value.IndexInfo.Closed < oldestSession.IndexInfo.Closed)
                        {
                            oldestKey = kvp.Key;
                            oldestSession = kvp.Value;
                        }
                    }
                }
                if (oldestSession != null)
                {
                    Sessions.Remove(oldestKey);
                }
            }
        }
    }


    public delegate void SessionStoreChangeArgs(SessionStore store);
    public class SessionStore
    {
        public event SessionStoreChangeArgs OnStoreChange;

        [JsonIgnore]
        public KeyStore Keys { get; set; }
        [JsonIgnore]
        public AuthenticationCreds Creds { get; set; }

        private string _sessions;
        public SessionStore(string path)
        {
            _sessions = path;
            Directory.CreateDirectory(_sessions);
            Sessions = new Dictionary<string, SessionRecord>();
            var files = Directory.GetFiles(_sessions);
            foreach (var item in files)
            {

            }
        }

        public void LoadKeyandCreds(KeyStore keys, AuthenticationCreds creds)
        {
            Keys = keys;
            Creds = creds;
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
                    File.Copy(file, file + ".dlt");
                }
            }
            else
            {
                Sessions[id] = key;
                File.WriteAllText(file, JsonConvert.SerializeObject(key, Formatting.Indented));
            }
            OnStoreChange?.Invoke(this);
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
            return Keys.Get((int)preKeyId);
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

        internal void RemovePreKey(int preKeyId)
        {
            Keys.Set(preKeyId, null);
        }
    }
}
