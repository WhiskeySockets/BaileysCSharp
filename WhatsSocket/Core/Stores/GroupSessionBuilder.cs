using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models.SenderKeys;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Stores
{
    public class GroupSessionBuilder
    {
        public GroupSessionBuilder(BaseKeyStore keys)
        {
            Keys = keys;
        }

        public BaseKeyStore Keys { get; }

        public void Process(string senderKeyName, SenderKeyDistributionMessage message)
        {
            var senderKeyRecord = Keys.Get<SenderKeyRecord>(senderKeyName);
            senderKeyRecord.AddSenderKeyState(message.Id, message.Iteration, message.ChainKey.ToByteArray(), message.SigningKey.ToByteArray());
            Keys.Set(senderKeyName, senderKeyRecord);
        }
    }
}
