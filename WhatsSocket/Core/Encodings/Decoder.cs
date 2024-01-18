using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WhatsSocket.Core.Encodings
{

    public class BufferDecoder
    {
        public static BinaryNode DecodeBinaryNode(byte[] buffer)
        {
            var decompressed = DecompressIfRequired(buffer);
            return DecodeDecompressedBinaryNode(decompressed);
        }

        private static byte[] DecompressIfRequired(byte[] buffer)
        {
            if ((2 & buffer[0]) != 0)
            {
                using (MemoryStream memoryStream = new MemoryStream(buffer, 1, buffer.Length - 1))
                using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    deflateStream.CopyTo(decompressedStream);
                    return decompressedStream.ToArray();
                }
            }
            else
            {
                return buffer[1..];
            }
        }

        private static BinaryNode DecodeDecompressedBinaryNode(byte[] buffer)
        {
            var reader = new BufferReader();
            var node = reader.DecodeDecompressedBinaryNode(new MemoryStream(buffer));
            return node;
        }

    }



}
