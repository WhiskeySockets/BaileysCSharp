using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.Zlib;
using System;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.WABinary;
using static WhatsSocket.Core.WABinary.Constants;

namespace WhatsSocket.Core.Utils
{
    public class BufferReader
    {
        public static BinaryNode DecodeDecompressedBinaryNode(byte[] buffer)
        {
            buffer = DecompressIfRequired(buffer);
            var reader = new BufferReader();
            var node = reader.DecodeDecompressedBinaryNode(new MemoryStream(buffer));
            return node;
        }


        private static byte[] DecompressIfRequired(byte[] buffer)
        {
            if ((2 & buffer[0]) != 0)
            {
                return Deflate(buffer.Skip(1).ToArray());

                //using (MemoryStream memoryStream = new MemoryStream(buffer, 1, buffer.Length - 1))
                //using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                //using (MemoryStream decompressedStream = new MemoryStream())
                //{
                //    deflateStream.CopyTo(decompressedStream);
                //    return decompressedStream.ToArray();
                //}
            }
            else
            {
                return buffer[1..];
            }
        }


        public static byte[] Deflate(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer, 0, buffer.Length))
            using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                deflateStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }
        public static byte[] Inflate(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer, 0, buffer.Length))
            using (var deflateStream = new ZInputStream(memoryStream))
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                deflateStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }

        public int IndexRef { get; set; }
        public BufferReader()
        {
        }

        private Stream InternalStream { get; set; }
        private void CheckEOS(int length)
        {
            if (IndexRef + length > InternalStream.Length)
                throw new EndOfStreamException();
        }

        private byte Next()
        {
            byte[] buffer = new byte[1];
            InternalStream.Read(buffer);
            IndexRef++;
            return buffer[0];
        }

        private byte ReadByte()
        {
            CheckEOS(1);
            return Next();
        }
        private byte[] ReadBytes(int size)
        {
            CheckEOS(size);
            var buffer = new byte[size];
            InternalStream.Read(buffer, 0, size);
            IndexRef = IndexRef + size;
            return buffer;
        }

        private int ReadListSize(byte tag)
        {
            switch (tag)
            {
                case TAGS.LIST_EMPTY:
                    return 0;
                case TAGS.LIST_8:
                    return ReadByte();
                case TAGS.LIST_16:
                    return ReadInt(2);
                default:
                    throw new InvalidOperationException("invalid tag for list size: " + tag);
            }

        }

        private int ReadInt(int n, bool littleEndian = false)
        {
            CheckEOS(1);
            int val = 0;

            for (var i = 0; i < n; i++)
            {
                var shift = littleEndian ? i : n - 1 - i;

                val |= Next() << shift * 8;

            }
            return val;
        }

        private string ReadString(byte tag)
        {
            if (tag >= 1 && tag < SINGLE_BYTE_TOKENS.Length)
            {
                return SINGLE_BYTE_TOKENS[tag] ?? "";

            }

            switch (tag)
            {
                case TAGS.DICTIONARY_0:
                case TAGS.DICTIONARY_1:
                case TAGS.DICTIONARY_2:
                case TAGS.DICTIONARY_3:
                    return GetTokenDouble(tag - TAGS.DICTIONARY_0, ReadByte());

                case TAGS.LIST_EMPTY:
                    return "";

                case TAGS.BINARY_8:
                    return ReadStringFromChars(ReadByte());

                case TAGS.BINARY_20:
                    return ReadStringFromChars(ReadInt20());

                case TAGS.BINARY_32:
                    return ReadStringFromChars(ReadInt(4));

                case TAGS.JID_PAIR:
                    return ReadJidPair();

                case TAGS.AD_JID:
                    return ReadAdJid();

                case TAGS.HEX_8:
                case TAGS.NIBBLE_8:
                    return ReadPacked8(tag);

                default:
                    throw new Exception("invalid string with tag: " + tag);

            }
        }

        private string ReadPacked8(int tag)
        {
            int startByte = ReadByte();
            string value = "";

            for (int i = 0; i < (startByte & 127); i++)
            {
                int curByte = ReadByte();
                value += Convert.ToChar(UnpackByte(tag, (curByte & 0xf0) >> 4));
                value += Convert.ToChar(UnpackByte(tag, curByte & 0x0f));
            }

            if (startByte >> 7 != 0)
            {
                value = value.Substring(0, value.Length - 1);
            }

            return value;
        }

        private int UnpackByte(int tag, int value)
        {
            if (tag == TAGS.NIBBLE_8)
            {
                return UnpackNibble(value);
            }
            else if (tag == TAGS.HEX_8)
            {
                return UnpackHex(value);
            }
            else
            {
                throw new Exception("unknown tag: " + tag);
            }
        }
        private int UnpackHex(int value)
        {
            if (value >= 0 && value < 16)
            {
                return value < 10 ? '0' + value : 'A' + value - 10;
            }

            throw new Exception("invalid hex: " + value);
        }

        private int UnpackNibble(int value)
        {
            if (value >= 0 && value <= 9)
            {
                return '0' + value;
            }

            switch (value)
            {
                case 10:
                    return '-';
                case 11:
                    return '.';
                case 15:
                    return '\0';
                default:
                    throw new Exception("invalid nibble: " + value);
            }
        }

        private string ReadAdJid()
        {
            var agent = ReadByte();

            var device = ReadByte();

            var user = ReadString(ReadByte());
            return JidUtils.JidEncode(user, agent == 0 ? "s.whatsapp.net" : "lid", device);
        }


        private string ReadJidPair()
        {
            var i = ReadString(ReadByte());
            var j = ReadString(ReadByte());
            return $"{i}@{j}";
        }

        private string ReadStringFromChars(int length)
        {
            var buffer = ReadBytes(length);
            return Encoding.UTF8.GetString(buffer);
        }

        private int ReadInt20()
        {
            CheckEOS(3);
            return ((Next() & 15) << 16) + (Next() << 8) + Next();
        }

        private string GetTokenDouble(int index1, byte index2)
        {
            if (index1 > DOUBLE_BYTE_TOKENS.Length)
            {
                throw new Exception($"Invalid double token dict({index1})");
            }
            var dict = DOUBLE_BYTE_TOKENS[index1];
            if (index2 > dict.Length)
            {
                throw new Exception($"Invalid double token({index2})");
            }

            return dict[index2];
        }

        private bool IsListTag(byte tag)
        {
            return tag == TAGS.LIST_EMPTY || tag == TAGS.LIST_8 || tag == TAGS.LIST_16;
        }

        private BinaryNode[] ReadList(byte tag)
        {
            var size = ReadListSize(tag);
            var messages = new BinaryNode[size];
            for (int i = 0; i < messages.Length; i++)
            {
                messages[i] = DecodeDecompressedBinaryNode(InternalStream, IndexRef);
            }

            return messages;
        }

        private BinaryNode DecodeDecompressedBinaryNode(Stream buffer, int index = 0)
        {
            BinaryNode node = new BinaryNode();
            IndexRef = index;
            InternalStream = buffer;
            InternalStream.Position = IndexRef;

            var listSize = ReadListSize(ReadByte());
            node.tag = ReadString(ReadByte());

            var attributesLength = listSize - 1 >> 1;

            node.attrs = new Dictionary<string, string>();
            for (var i = 0; i < attributesLength; i++)
            {
                var key = ReadString(ReadByte());
                var value = ReadString(ReadByte());
                node.attrs[key] = value;

            }

            if (listSize % 2 == 0)
            {
                var tag = ReadByte();
                if (IsListTag(tag))
                {
                    node.content = ReadList(tag);
                }
                else
                {
                    switch (tag)
                    {
                        case TAGS.BINARY_8:
                            node.content = ReadBytes(ReadByte());
                            break;
                        case TAGS.BINARY_20:
                            node.content = ReadBytes(ReadInt20());
                            break;
                        case TAGS.BINARY_32:
                            node.content = ReadBytes(ReadInt(4));
                            break;
                    }
                }
            }

            return node;
        }

    }
}
