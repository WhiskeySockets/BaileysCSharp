using Google.Protobuf;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Encodings;

namespace WhatsSocket.Core.Helper
{
    public static class EndodingHelper
    {
        public static byte[] UInt8ToBigEndianBytes(this int value)
        {
            return new byte[] { Convert.ToByte(value) };
        }

        public static byte[] UInt16ToBigEndianBytes(this int value)
        {
            byte[] bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public static ByteString ToByteString(this byte[] buffer)
        {
            return ByteString.CopyFrom(buffer);
        }
        public static string ToBase64(this byte[] buffer)
        {
            return Convert.ToBase64String(buffer);
        }

        public static byte[] EncodeBigEndian(this int number, int t = 4)
        {
            var buffer = BitConverter.GetBytes(number).Take(t).Reverse().ToArray();

            return buffer;
        }

        public static byte[] StringToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
        }


    }
}
