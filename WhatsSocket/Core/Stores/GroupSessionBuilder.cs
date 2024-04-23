using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models.SenderKeys;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Signal;
using BaileysCSharp.LibSignal;

namespace BaileysCSharp.Core.Stores
{
    public class GroupSessionBuilder
    {
        public GroupSessionBuilder(SignalStorage store)
        {
            Store = store;
        }

        public SignalStorage Store { get; }

        public void Process(string senderKeyName, SenderKeyDistributionMessage message)
        {
            var senderKeyRecord = Store.LoadSenderKey(senderKeyName);
            senderKeyRecord.AddSenderKeyState(message.Id, message.Iteration, message.ChainKey.ToByteArray(), message.SigningKey.ToByteArray());
            Store.StoreSenderKey(senderKeyName, senderKeyRecord);
        }


        public SenderKeyDistributionMessage Create(string senderKeyName)
        {
            var senderKeyRecord = Store.LoadSenderKey(senderKeyName);
            if (senderKeyRecord.IsEmpty)
            {
                Random rnd = new Random();
                uint keyId = 1228456509;
                byte[] senderKey = Convert.FromBase64String("mOsB3jVwAzTOjq08DpXJpWM4La6IvqyLSTpzSeZQjGo=");//KeyHelper.GenerateSenderKey();
                var signingKey = new KeyPair()
                {
                    Public = Convert.FromBase64String("BetS2PNKwvg7xDFgSg62k0kZFsq3QGb9Fx60+ikazut6"),
                    Private = Convert.FromBase64String("UA/qH1oY2WOmrLN13nfvXvzpybe4FByP5znZ1AQeM2M=")
                }; //KeyHelper.GenerateSenderSigningKey();

                senderKeyRecord.SetSenderKeyState(keyId,0,senderKey,signingKey);

                Store.StoreSenderKey(senderKeyName, senderKeyRecord);
            }
            var state = senderKeyRecord.GetSenderStateKey(0);

            return new SenderKeyDistributionMessage()
            {
                Id = state.SenderKeyId,
                Iteration = state.SenderChainKey.Iteration,
                ChainKey = state.SenderChainKey.Seed.ToByteString(),
                SigningKey = state.GetSigningKeyPublic().ToByteString(),
            };
        }
    }
}
