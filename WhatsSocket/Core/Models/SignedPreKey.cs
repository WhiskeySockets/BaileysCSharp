using Newtonsoft.Json;

namespace WhatsSocket.Core.Models
{
    public class SignedPreKey : PreKeyPair
    {
        public byte[] Signature { get; set; }
    }
    


}
