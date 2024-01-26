using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Encodings
{
    public static class JidUtils
    {
        public static FullJid? JidDecode(string jid)
        {
            if (string.IsNullOrEmpty(jid))
                return null;

            var sepIndex = jid.IndexOf('@');
            if (sepIndex < 0)
                return null;

            FullJid result = new FullJid();

            result.Server = jid.Substring(sepIndex + 1);


            var userCombined = jid.Substring(0, sepIndex);

            var userAgentDevice = userCombined.Split(':');
            result.User = userAgentDevice[0];


            if (userAgentDevice.Length > 1)
            {
                result.Device = Convert.ToInt32(userAgentDevice[1]);
            }

            result.DomainType = result.Server == "lid" ? 1 : 0;

            return result;
        }

        public static bool AreJidsSameUser(string? id1, string? id2)
        {
            return JidDecode(id1)?.User == JidDecode(id2)?.User;
        }

        public static bool IsJidUser(string id)
        {
            return id.EndsWith("@s.whatsapp.net");
        }

        public static bool IsLidUser(string id)
        {
            return id.EndsWith("@lid");
        }

        public static bool IsBroadcast(string id)
        {
            return id.EndsWith("@broadcast");
        }

        public static bool IsJidStatusBroadcast(string id)
        {
            return id == "status@broadcast";
        }

        public static bool IsJidGroup(string id)
        {
            return id.EndsWith("@g.us");
        }

    }

}
