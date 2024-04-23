using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Models
{
    internal class MutationKey
    {
        public byte[] IndexKey { get; set; }
        public byte[] ValueEncryptionKey { get; set; }
        public byte[] ValueMacKey { get; set; }
        public byte[] SnapshotMacKey { get; set; }
        public byte[] PatchMacKey { get; set; }
    }
}
