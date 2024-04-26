using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.NoSQL;
using System.Text.Json.Serialization;

namespace BaileysCSharp.LibSignal
{
    //SessionRecord
    [FolderPrefix("session")]
    public class SessionRecord
    {
        [JsonPropertyName("_sessions")]
        public Dictionary<string, Session> Sessions { get; set; }


        [JsonPropertyName("version")]
        public string Version { get; set; }

        public SessionRecord()
        {
            Sessions = new Dictionary<string, Session>();
            Version = "v1";
        }

        public Session? GetSession(string key)
        {
            if (Sessions.ContainsKey(key))
                return Sessions[key];
            return null;
        }

        public Session? GetOpenSession()
        {
            foreach (var kvp in Sessions)
            {
                if (!IsClosed(kvp.Value))
                    return kvp.Value;
            }
            return null;
        }

        public void CloseSession(Session session)
        {
            if (!IsClosed(session))
            {
                session.IndexInfo.Closed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        public bool IsClosed(Session session)
        {
            if (session == null)
                return false;
            return session.IndexInfo.Closed >= 0;
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

        internal List<Session> GetSessions()
        {
            var sesions = Sessions.Values.ToList();
            return sesions.OrderByDescending(x => x.IndexInfo.Used).ToList();
        }
    }
}
