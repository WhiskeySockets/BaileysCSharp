using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Models
{
    public class MediaDecryptionKeyInfo
    {
        public byte[] IV { get; set; }
        public byte[] CipherKey { get; set; }
        public byte[] MacKey { get; set; }


        public override string ToString()
        {
            return $"IV: {IV.ToBase64()}\nCipherKey: {CipherKey.ToBase64()}\nMacKey: {MacKey.ToBase64()}";
        }
    }

    public class MediaDownloadOptions
    {
        public int StartByte { get; set; }
        public int EndByte { get; set; }
    }
}
