using Proto;
using SkiaSharp;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models.Sending.Interfaces;
using static Proto.Message.Types;
using static System.Net.Mime.MediaTypeNames;

namespace WhatsSocket.Core.Models.Sending.Media
{
    public class DocumentMessageContent : AnyMediaMessageContent
    {
        private MemoryStream document;

        public DocumentMessageContent()
        {
            Property = "DocumentMessage";
        }

        public Stream Document
        {
            get => document;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            if (value is MemoryStream memoryStream)
            {
                document = memoryStream;
            }
            else
            {
                document = new MemoryStream();
                value.CopyTo(document);
            }
            document.Position = 0;
            FileLength = (ulong)document.Length;
        }
        public override async Task Process()
        {

        }

        public string FileName { get; set; }
        public string Mimetype { get; set; }
        public ulong FileLength { get; set; }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new DocumentMessage()
            {
                ContextInfo = ContextInfo,
                Mimetype = Mimetype,
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            if (!string.IsNullOrWhiteSpace(FileName))
            {
                image.FileName = FileName;
            }
            return image;
        }

    }
}
