using Google.Protobuf;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Stores;
using static System.Runtime.InteropServices.JavaScript.JSType;
using WhatsSocket.Exceptions;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Utils
{
    public class HashGenerator
    {
        public HashGenerator(AppStateSyncVersion initialState)
        {
            InitialState = initialState;
            addBuffs = new List<byte[]>();
            subBuffs = new List<byte[]>();
        }

        public AppStateSyncVersion InitialState { get; }

        public List<byte[]> addBuffs { get; set; }
        public List<byte[]> subBuffs { get; set; }

        internal AppStateSyncVersion Finish()
        {
            var hashArrayBuffer = new byte[InitialState.Hash.Length];
            var LT_HASH_ANTI_TAMPERING = new HashAntiTampering("WhatsApp Patch Integrity");

            var result = LT_HASH_ANTI_TAMPERING.SubstarctThenAdd(hashArrayBuffer, addBuffs, subBuffs);

            return new AppStateSyncVersion()
            {
                Hash = result,
                IndexValueMap = InitialState.IndexValueMap,
            };
        }

        internal void Mix(ByteString indexMac, byte[] valueMac, SyncdMutation.Types.SyncdOperation operation)
        {
            var indexMacBase64 = indexMac.ToBase64();

            byte[] prevOp = null;

            if (InitialState.IndexValueMap.ContainsKey(indexMacBase64))
            {
                prevOp = InitialState.IndexValueMap[indexMacBase64];
            }

            if (operation == SyncdMutation.Types.SyncdOperation.Remove)
            {
                if (prevOp == null)
                {
                    var data = new Dictionary<string, string>() { [indexMacBase64] = Encoding.UTF8.GetString(valueMac) };
                    throw new Boom("tried remove, but no previous op", new BoomData(data));
                }

                InitialState.IndexValueMap.Remove(indexMacBase64);
            }
            else
            {
                addBuffs.Add(valueMac);

                if (InitialState.IndexValueMap.ContainsKey(indexMacBase64))
                {

                }

                InitialState.IndexValueMap[indexMacBase64] = valueMac;
            }


            if (prevOp != null)
            {
                subBuffs.Add(prevOp);
            }
        }
    }
}
