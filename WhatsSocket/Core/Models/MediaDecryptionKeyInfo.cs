using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsSocket.Core.Models
{
    public class MediaDecryptionKeyInfo
    {
        public byte[] IV { get; set; }
        public byte[] CipherKey { get; set; }
        public byte[] MacKey { get; set; }
    }

    public class MediaDownloadOptions
    {
        public int StartByte { get; set; }
        public int EndByte { get; set; }
    }
}
