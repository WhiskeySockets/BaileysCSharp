using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Helper
{
    public class HashAntiTampering
    {
        public HashAntiTampering(string salt)
        {
            Salt = salt;
        }

        public string Salt { get; }

        public byte[] SubstarctThenAdd(byte[] e, List<byte[]> t, List<byte[]> r)
        {
            return Add(Substract(e, r), t);
        }

        private byte[] Add(byte[] e, List<byte[]> t)
        {
            foreach (var item in t)
            {
                e = _addSingle(e, item);
            }
            return e;
        }

        private byte[] _addSingle(byte[] e, byte[] t)
        {
            var n = CryptoUtils.HKDF(t, 128, [], Encoding.UTF8.GetBytes(Salt));
            return PerformPointwiseWithOverflow(e, n, AddFunc);
        }

        private ushort AddFunc(ushort e, ushort t)
        {
            return (ushort)(e + t);
        }

        private byte[] PerformPointwiseWithOverflow(byte[] e, byte[] t, Func<ushort, ushort, ushort> r)
        {
            var a = new byte[e.Length];
            for (var i = 0; i < e.Length; i += 2)
            {
                a.SetUint16(i, r(e.GetUint16(i), t.GetUint16(i)));
            }
            return a;
        }

        private byte[] Substract(byte[] e, List<byte[]> t)
        {
            foreach (var item in t)
            {
                e = _subtractSingle(e, item);
            }
            return e;
        }

        private byte[] _subtractSingle(byte[] e, byte[] t)
        {
            var n = CryptoUtils.HKDF(t, 128, [], Encoding.UTF8.GetBytes(Salt));
            return PerformPointwiseWithOverflow(e, n, SubFunc);
        }

        private ushort SubFunc(ushort e, ushort t)
        {
            return (ushort)(e - t);
        }
    }
}
