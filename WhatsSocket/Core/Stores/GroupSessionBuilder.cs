using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsSocket.Core.Stores
{
    public class GroupSessionBuilder
    {
        public GroupSessionBuilder(SenderKeyStore senderKeyStore)
        {
            SenderKeyStore = senderKeyStore;
        }

        public SenderKeyStore SenderKeyStore { get; }


        public void Process(string senderKeyName, SenderKeyDistributionMessage message)
        {
            var senderKeyRecord = SenderKeyStore.Get(senderKeyName);
            senderKeyRecord.AddSenderKeyState(message.Id, message.Iteration, message.ChainKey.ToByteArray(), message.SigningKey.ToByteArray());
            SenderKeyStore.StoreSenderKey(senderKeyName, senderKeyRecord);
        }
    }
}
