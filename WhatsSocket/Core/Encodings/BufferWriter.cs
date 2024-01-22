using System.Text;
using static WhatsSocket.Core.Helper.Constants;

namespace WhatsSocket.Core.Encodings
{
    public class BufferWriter
    {
        public static BufferWriter EncodeBinaryNode(BinaryNode node)
        {
            var writer = new BufferWriter();
            writer.EncodeBinaryNode(node);
            return writer;
        }

        MemoryStream Buffer;
        BinaryNode Node { get; set; }

        public void EncodeBinaryNode(BinaryNode node, MemoryStream buffer = null)
        {
            Buffer = buffer ?? new MemoryStream();
            if (buffer == null)
                PushByte(0);
            Node = node;


            var validAttributes = Node.attrs.Where(x => !string.IsNullOrEmpty(x.Key)).Select(x => x.Key).ToList();
            WriteListStart((2 * validAttributes.Count) + 1 + (Node.content != null ? 1 : 0));
            WriteString(Node.tag);

            foreach (var item in validAttributes)
            {
                if (Node.attrs[item] is string)
                {
                    WriteString(item);
                    WriteString(node.attrs[item]);
                }
            }

            if (Node.content is string str)
            {
                WriteString(str);
            }
            else if (Node.content is byte[] bytes)
            {
                WriteByteLength(bytes.Length);
                PushBytes(bytes);
            }
            else if (Node.content is BinaryNode[] binaryNodes)
            {
                WriteListStart(binaryNodes.Length);
                foreach (var item in binaryNodes)
                {
                    EncodeBinaryNode(item, Buffer);
                }
            }

        }


        private void PushByte(int value)
        {
            var @byte = Convert.ToByte(value & 0xff);
            Buffer.WriteByte(@byte);
        }

        private void PushInt(int value, int n, bool littleEndian = false)
        {
            for (var i = 0; i < n; i++)
            {
                var curShift = littleEndian ? i : n - 1 - i;
                var byte_value = (byte)((value >> (curShift * 8)) & 0xff);
                Buffer.WriteByte(byte_value);
            }
        }

        private void PushBytes(params byte[] bytes)
        {
            foreach (var item in bytes)
            {
                PushByte(item);
            }
        }
        private void PushBytes(params int[] bytes)
        {
            foreach (var item in bytes)
            {
                PushByte(item);
            }
        }


        private void PushInt16(int value)
        {
            PushBytes((value >> 8) & 0xff, value & 0xff);
        }
        private void PushInt20(int value)
        {
            PushBytes((value >> 16) & 0x0f, (value >> 8) & 0xff, value & 0xff);
        }


        private void WriteListStart(int listSize)
        {
            if (listSize == 0)
            {
                PushByte(TAGS.LIST_EMPTY);
            }
            else if (listSize < 256)
            {
                PushBytes(TAGS.LIST_8, listSize);
            }
            else
            {
                PushByte(TAGS.LIST_16);
                PushInt16((short)listSize);
            }

        }

        private void WriteByteLength(int length)
        {
            if ((long)length >= 4294967296)
            {
                throw new Exception("string too large to encode: " + length);
            }

            if (length >= 1 << 20)
            {
                PushByte(TAGS.BINARY_32);
                PushInt(length, 4); // 32 bit integer
            }
            else if (length >= 256)
            {
                PushByte(TAGS.BINARY_20);
                PushInt20(length);
            }
            else
            {
                PushByte(TAGS.BINARY_8);
                PushByte(length);
            }
        }
        private void WriteStringRaw(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteByteLength(bytes.Length);
            PushBytes(bytes);
        }
        private void WriteJid(FullJid decodedJid)
        {
            if (decodedJid.Device.HasValue)
            {
                PushByte(TAGS.AD_JID);
                PushByte(decodedJid.DomainType ?? 0);
                PushByte(decodedJid.Device ?? 0);
                WriteString(decodedJid.User);
            }
            else
            {
                PushByte(TAGS.JID_PAIR);
                if (decodedJid.User.Length > 0)
                {
                    WriteString(decodedJid.User);
                }
                else
                {
                    PushByte(TAGS.LIST_EMPTY);
                }
                WriteString(decodedJid.Server);
            }
        }



        public int PackNibble(char @char)
        {
            switch (@char)
            {
                case '-':
                    return 10;
                case '.':
                    return 11;
                case '\0':
                    return 15;
                default:
                    if (@char >= '0' && @char <= '9')
                    {
                        var result = @char - '0';
                        return result;
                    }
                    throw new Exception($"invalid byte for nibble \"{@char}\"");
            }
        }

        public int PackHex(char @char)
        {
            if (@char >= '0' && @char <= '9')
            {
                return @char - '0';
            }

            if (@char >= 'A' && @char <= 'F')
            {
                return 10 + @char - 'A';
            }

            if (@char >= 'a' && @char <= 'f')
            {
                return 10 + @char - 'a';
            }

            if (@char == '\0')
            {
                return 15;
            }

            throw new Exception($"Invalid hex char \"{@char}\"");
        }


        private void WritePackedBytes(string str, string type)
        {
            if (str.Length > TAGS.PACKED_MAX)
            {
                throw new Exception("Too many bytes to pack");
            }

            PushByte(type == "nibble" ? TAGS.NIBBLE_8 : TAGS.HEX_8);

            var roundedLength = (int)Math.Ceiling(str.Length / 2.0);
            if (str.Length % 2 != 0)
            {
                roundedLength |= 128;
            }
            PushByte(roundedLength);


            Func<char, int> packFunction = type == "nibble" ? PackNibble : PackHex;
            Func<char, char, int> packBytePair = (char v1, char v2) =>
            {
                return (packFunction(v1) << 4) | packFunction(v2);
            };


            var strLengthHalf = (int)Math.Floor(str.Length / 2.0);
            for (var i = 0; i < strLengthHalf; i++)
            {
                PushByte(packBytePair(str[2 * i], str[2 * i + 1]));

            }

            if (str.Length % 2 != 0)
            {
                PushByte(packBytePair(str[str.Length - 1], '\x00'));

            }
        }
        private bool IsNibble(string str)
        {
            if (str.Length > TAGS.PACKED_MAX)
            {
                return false;
            }

            for (var i = 0; i < str.Length; i++)
            {
                var @char = str[i];

                var isInNibbleRange = @char >= '0' && @char <= '9';
                if (!isInNibbleRange && @char != '-' && @char != '.')
                {
                    return false;
                }
            }

            return true;
        }


        private bool IsHex(string str)
        {
            if (str.Length > TAGS.PACKED_MAX)
            {
                return false;
            }

            for (var i = 0; i < str.Length; i++)
            {
                var @char = str[i];
                var isInNibbleRange = @char >= '0' && @char <= '9';
                if (!isInNibbleRange && !(@char >= 'A' && @char <= 'F') && !(@char >= 'a' && @char <= 'f'))
                {
                    return false;
                }
            }

            return true;
        }

        private void WriteString(string str)
        {
            if (TOKEN_MAP.ContainsKey(str))
            {
                var tokenIndex = TOKEN_MAP[str];
                if (tokenIndex.ContainsKey("dict"))
                {
                    PushByte(TAGS.DICTIONARY_0 + tokenIndex["dict"]);
                }
                PushByte(tokenIndex["index"]);
            }
            else if (IsNibble(str))
            {
                WritePackedBytes(str, "nibble");
            }
            else if (IsHex(str))
            {
                WritePackedBytes(str, "hex");
            }
            else if (str != null)
            {
                var decodedJid = JidDecode(str);
                if (decodedJid != null)
                {
                    WriteJid(decodedJid);
                }
                else
                {
                    WriteStringRaw(str);
                }
            }
        }

        //private void WriteLineStart(int listSize)
        //{
        //    if (listSize == 0)
        //    {
        //        PushByte(TAGS.LIST_EMPTY);
        //    }
        //    else if (listSize < 256)
        //    {
        //        PushBytes(TAGS.LIST_8, listSize);
        //    }
        //    else
        //    {
        //        PushByte(TAGS.LIST_16);
        //        PushInt16(listSize);
        //    }
        //}



        private FullJid? JidDecode(string jid)
        {
            var sepIndex = jid.IndexOf('@');
            if (sepIndex < 0)
                return null;

            FullJid result = new FullJid();

            result.Server = jid.Substring(sepIndex + 1);


            var userCombined = jid.Substring(0, sepIndex);

            var userAgentDevice = userCombined.Split(':');
            result.User = userAgentDevice[0];


            if (userAgentDevice.Length > 1)
            {
                result.Device = Convert.ToInt32(userAgentDevice[1]);
            }

            result.DomainType = result.Server == "lid" ? 1 : 0;

            return result;
        }

        public byte[] ToByteArray()
        {
            return Buffer.ToArray();
        }
    }
}
