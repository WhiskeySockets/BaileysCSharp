using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Signal;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Utils
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
                result.Device = Convert.ToUInt32(userAgentDevice[1]);
            }

            result.DomainType = result.Server == "lid" ? 1 : 0;

            return result;
        }


        public static string JidNormalizedUser(string jid)
        {
            var result = JidDecode(jid);
            if (result == null)
                return "";
            return JidEncode(result.User, result.Server);
        }

        public static string JidEncode(string user, string server, uint? device = null, int? agent = null)
        {
            if (device == 0)
                device = null;
            return $"{user ?? ""}{(agent != null ? $"_{agent}" : "")}{(device != null ? $":{device}" : "")}@{server}";
        }


        public static string JidToSignalSenderKeyName(string group, string user)
        {
            var addr = new ProtocolAddress(JidDecode(user));
            return $"{group}::{addr}";
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
        
        public static bool IsJidNewsletter(string id)
        {
            return id.EndsWith("@newsletter");
        }

        public static bool IsJidGroup(string id)
        {
            return id.EndsWith("@g.us");
        }

    }

}
