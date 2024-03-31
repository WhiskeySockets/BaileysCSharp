using Google.Protobuf;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Utils
{
    public static class GenericUtils
    {
        public static Dictionary<string, string> ReduceBinaryNodeToDictionary(BinaryNode node, string tag)
        {
            var nodes = GetBinaryNodeChildren(node, tag);
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var item in nodes)
            {
                var key = item.getattr("name") ?? item.getattr("config_code");
                var value = item.getattr("value") ?? item.getattr("config_value");
                result[key] = value;
            }

            return result;

        }

        public static string GenerateMessageID()
        {
            var bytes = RandomBytes(6);
            return "BAE5" + BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }


        public static byte[]? GetBinaryNodeChildBuffer(BinaryNode node, string tag)
        {
            var child = GetBinaryNodeChild(node, tag);
            if (child != null)
                return child.ToByteArray();
            return null;
        }

        public static BinaryNode? GetBinaryNodeChild(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.FirstOrDefault(x => x.tag == tag);
            }
            return null;
        }
        public static BinaryNode[] GetBinaryNodeChildren(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.Where(x => x.tag == tag).ToArray();
            }
            return new BinaryNode[0];
        }

        public static BinaryNode[] GetAllBinaryNodeChildren(BinaryNode? message)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.ToArray();
            }
            return new BinaryNode[0];
        }

        public static string GetBinaryNodeChildString(BinaryNode node, string childTag)
        {
            var child = GetBinaryNodeChild(node, childTag)?.content;
            if (child is byte[] buffer)
            {
                return Encoding.UTF8.GetString(buffer);
            }
            return child?.ToString();
        }


        public static uint GetBinaryNodeChildUInt(BinaryNode node, string tag, int length)
        {
            var buffer = GetBinaryNodeChildBuffer(node, tag);
            if (buffer != null)
            {
                return BufferToUInt(buffer, length);
            }
            return 0;
        }


        public static uint BufferToUInt(byte[] e, int length)
        {
            uint a = 0;
            for (var i = 0; i < length; i++)
            {
                a = 256 * a + e[i];
            }
            return a;
        }


        public static byte[] RandomBytes(int size)
        {
            byte[] buffer = new byte[size];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return buffer;
        }


        public static byte[] EncodeWAMessage(Message message)
        {
            var buffer = message.ToByteArray();
            return WriteRandomPadMax16(buffer);
        }

        public static byte[] WriteRandomPadMax16(byte[] buffer)
        {
            var pad = RandomBytes(1);
            pad[0] &= 0xF;
            if (pad[0] == 0)
            {
                pad[0] = 0xF;
            }
            return buffer.Concat(pad).ToArray();
        }
    }
}
