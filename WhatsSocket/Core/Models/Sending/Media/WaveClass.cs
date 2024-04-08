using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsSocket.Core.Models.Sending.Media
{

    /// <summary>
    /// Summary description for WaveFile.
    /// </summary>
    public class WaveFile
    {
        /// <summary>
        /// The Riff header is 12 bytes long
        /// </summary>
        public class RiffBlock
        {
            public RiffBlock()
            {
                m_RiffID = new byte[4];
                m_RiffFormat = new byte[4];
            }

            public void ReadRiff(FileStream inFS)
            {
                inFS.Read(m_RiffID, 0, 4);

                Debug.Assert(m_RiffID[0] == 82, "Riff ID Not Valid");

                BinaryReader binRead = new BinaryReader(inFS);

                m_RiffSize = binRead.ReadUInt32();

                inFS.Read(m_RiffFormat, 0, 4);
            }

            public byte[] RiffID
            {
                get { return m_RiffID; }
            }

            public uint RiffSize
            {
                get { return (m_RiffSize); }
            }

            public byte[] RiffFormat
            {
                get { return m_RiffFormat; }
            }

            private byte[] m_RiffID;
            private uint m_RiffSize;
            private byte[] m_RiffFormat;
        }

        /// <summary>
        /// The Format header is 24 bytes long
        /// </summary>
        public class FmtBlock
        {
            public FmtBlock()
            {
                m_FmtID = new byte[4];
            }

            public void ReadFmt(FileStream inFS)
            {
                inFS.Read(m_FmtID, 0, 4);

                Debug.Assert(m_FmtID[0] == 102, "Format ID Not Valid");

                BinaryReader binRead = new BinaryReader(inFS);

                m_FmtSize = binRead.ReadUInt32();
                m_FmtTag = binRead.ReadUInt16();
                m_Channels = binRead.ReadUInt16();
                m_SamplesPerSec = binRead.ReadUInt32();
                m_AverageBytesPerSec = binRead.ReadUInt32();
                m_BlockAlign = binRead.ReadUInt16();
                m_BitsPerSample = binRead.ReadUInt16();

                // This accounts for the variable format header size 
                // 12 bytes of Riff Header, 4 bytes for FormatId, 4 bytes for FormatSize & the Actual size of the Format Header 
                inFS.Seek(m_FmtSize + 20, System.IO.SeekOrigin.Begin);
            }

            public byte[] FmtID
            {
                get { return m_FmtID; }
            }

            public uint FmtSize
            {
                get { return m_FmtSize; }
            }

            public ushort FmtTag
            {
                get { return m_FmtTag; }
            }

            public ushort Channels
            {
                get { return m_Channels; }
            }

            public uint SamplesPerSec
            {
                get { return m_SamplesPerSec; }
            }

            public uint AverageBytesPerSec
            {
                get { return m_AverageBytesPerSec; }
            }

            public ushort BlockAlign
            {
                get { return m_BlockAlign; }
            }

            public ushort BitsPerSample
            {
                get { return m_BitsPerSample; }
            }

            private byte[] m_FmtID;
            private uint m_FmtSize;
            private ushort m_FmtTag;
            private ushort m_Channels;
            private uint m_SamplesPerSec;
            private uint m_AverageBytesPerSec;
            private ushort m_BlockAlign;
            private ushort m_BitsPerSample;
        }

        /// <summary>
        /// The Data block is 8 bytes + ???? long
        /// </summary>
        public class DataBlock
        {
            public DataBlock()
            {
                m_DataID = new byte[4];
            }

            public void ReadData(FileStream inFS)
            {
                inFS.Read(m_DataID, 0, 4);

                Debug.Assert(m_DataID[0] == 100, "Data ID Not Valid");

                BinaryReader binRead = new BinaryReader(inFS);

                m_DataSize = binRead.ReadUInt32();

                m_Data = new Int16[m_DataSize];

                inFS.Seek(40, System.IO.SeekOrigin.Begin);

                m_NumSamples = (int)(m_DataSize / 2);

                for (int i = 0; i < m_NumSamples; i++)
                {
                    m_Data[i] = binRead.ReadInt16();
                }
            }

            public byte[] DataID
            {
                get { return m_DataID; }
            }

            public uint DataSize
            {
                get { return m_DataSize; }
            }

            public Int16 this[int pos]
            {
                get { return m_Data[pos]; }
            }

            public int NumSamples
            {
                get { return m_NumSamples; }
            }

            private byte[] m_DataID;
            private uint m_DataSize;
            private Int16[] m_Data;
            private int m_NumSamples;
        }

        public WaveFile(String inFilepath)
        {
            m_Filepath = inFilepath;
            m_FileInfo = new FileInfo(inFilepath);
            m_FileStream = m_FileInfo.OpenRead();

            m_Riff = new RiffBlock();
            m_Fmt = new FmtBlock();
            m_Data = new DataBlock();
        }

        public void Read()
        {
            m_Riff.ReadRiff(m_FileStream);
            m_Fmt.ReadFmt(m_FileStream);
            m_Data.ReadData(m_FileStream);
        }

        public DataBlock Data
        {
            get { return m_Data; }
        }

        public FmtBlock Format
        {
            get { return m_Fmt; }
        }

        public RiffBlock Riff
        {
            get { return m_Riff; }
        }

        private string m_Filepath;
        private FileInfo m_FileInfo;
        private FileStream m_FileStream;

        private RiffBlock m_Riff;
        private FmtBlock m_Fmt;
        private DataBlock m_Data;
    }
}
