using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sending.Media;

namespace Proto
{
    public interface IContextable
    {
        ContextInfo ContextInfo { get; set; }
    }

    public interface IMediaMessage 
    {
        public string Url { get; set; }
        public string DirectPath { get; set; }
        public ByteString MediaKey { get; set; }
        public ByteString FileEncSha256 { get; set; }
        public ByteString FileSha256 { get; set; }
        public ulong FileLength { get; set; }
        public long MediaKeyTimestamp { get; set; }
    }

    public sealed partial class Message
    {
        internal void SetContextInfo(ContextInfo contextInfo)
        {
            var children = this.GetType().GetProperties();
            foreach (var item in children)
            {
                var value = item.GetValue(this, null);
                if (value != null && value is IContextable contextable)
                {
                    contextable.ContextInfo = contextInfo;
                }
            }
        }

        public void SetMediaMessage(IMediaMessage message, string property)
        {
            var children = this.GetType().GetProperties();
            foreach (var item in children)
            {
                if (item.PropertyType == message.GetType() && item.Name == property)
                {
                    item.SetValue(this, message);
                }
            }
        }


        public partial class Types
        {
            public sealed partial class ExtendedTextMessage : IContextable
            {

            }

            public sealed partial class AudioMessage : IContextable
            {

            }

            public sealed partial class ImageMessage : IContextable
            {

            }

            public sealed partial class DocumentMessage : IContextable
            {

            }
            public sealed partial class DocumentMessage : IContextable
            {

            }



            public sealed partial class ImageMessage : IMediaMessage
            {

            }

            public sealed partial class VideoMessage : IMediaMessage
            {
            }

            public sealed partial class AudioMessage : IMediaMessage
            {

            }

            public sealed partial class StickerMessage : IMediaMessage
            {

            }

            public sealed partial class DocumentMessage : IMediaMessage
            {

            }
        }

    }

}
