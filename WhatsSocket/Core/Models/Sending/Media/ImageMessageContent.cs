using Proto;
using SkiaSharp;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models.Sending.Interfaces;
using static Proto.Message.Types;

namespace WhatsSocket.Core.Models.Sending.Media
{
    public class ImageMessageContent : AnyMediaMessageContent, IWithDimentions
    {
        private Stream image;

        public ImageMessageContent()
        {
            Property = "ImageMessage";
        }

        public Stream Image
        {
            get => image;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            if (value is MemoryStream memoryStream)
            {
                image = memoryStream;
            }
            else
            {
                memoryStream = new MemoryStream();
                value.CopyTo(memoryStream);
                image = memoryStream;

            }
            image.Position = 0;
            FileLength = (ulong)image.Length;
        }
        public override async Task Process()
        {
            byte[] copy = new byte[Image.Length];
            await image.ReadAsync(copy, 0, copy.Length);
            image.Position = 0;
            using (var bitmap = SKBitmap.Decode(copy))
            {
                Height = (uint)bitmap.Height;
                Width = (uint)bitmap.Width;
                using (var resized = bitmap.Resize(new SKSizeI(32, 32), SKFilterQuality.None))
                {
                    using (var stream = new MemoryStream())
                    {
                        resized.Encode(stream, SKEncodedImageFormat.Jpeg, 50);
                        JpegThumbnail = stream.ToArray();
                    }
                }
            }
        }

        public string Caption { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public ulong FileLength { get; set; }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new ImageMessage()
            {
                ContextInfo = ContextInfo,
                Width = Width,
                Height = Height,
                Mimetype = "image/jpeg",
                JpegThumbnail = JpegThumbnail.ToByteString(),
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            if (!string.IsNullOrWhiteSpace(Caption))
            {
                image.Caption = Caption;
            }
            return image;
        }

    }
}
