using FFMpegCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities;
using Proto;
using SkiaSharp;
using System.Drawing;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models.Sending.Interfaces;
using WhatsSocket.Core.Utils;
using static Proto.Message.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WhatsSocket.Core.Models.Sending.Media
{
    public class VideoMessageContent : AnyMediaMessageContent
    {
        private MemoryStream video;
        public VideoMessageContent()
        {
            Property = "VideoMessage";
        }

        public string Caption { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public uint Seconds { get; set; }
        public ulong FileLength { get; set; }
        public string Mimetype { get; set; }
        public bool GifPlayback { get; set; }

        public Stream Video
        {
            get => video;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            if (value is MemoryStream memoryStream)
            {
                video = memoryStream;
            }
            else
            {
                memoryStream = new MemoryStream();
                value.CopyTo(memoryStream);
                video = memoryStream;

            }
            video.Position = 0;
            FileLength = (ulong)video.Length;


        }

        public override async Task Process()
        {
            byte[] copy = video.ToArray();
            video.Position = 0;



            var temp = Path.GetTempFileName();
            File.WriteAllBytes(temp, copy);
            var mediaInfo = await FFProbe.AnalyseAsync(temp);
            File.Delete(temp);
            Seconds = (uint)mediaInfo.Duration.TotalSeconds;
        }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new VideoMessage()
            {
                ContextInfo = ContextInfo,
                Mimetype = Mimetype ?? "video/mp4",
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seconds = Seconds,
                JpegThumbnail = JpegThumbnail.ToByteString(),
                ThumbnailDirectPath = ThumbnailDirectPath,
                ThumbnailEncSha256 = ThumbnailEncSha256.ToByteString(),
                ThumbnailSha256 = ThumbnailSha256.ToByteString(),
                GifPlayback = GifPlayback
            };
            return image;
        }

        internal MediaUploadData ExtractThumbnail()
        {
            byte[] copy = video.ToArray();
            video.Position = 0;
            var temp = Path.GetTempFileName();
            File.WriteAllBytes(temp, copy);
            var path = Path.GetDirectoryName(temp);
            var output = Path.Combine(path, $"{Guid.NewGuid()}.png");
            var bitmap = FFMpeg.Snapshot(temp, output, new Size(32, 32), TimeSpan.FromSeconds(1));

            while (!File.Exists(output))
            {
                Thread.Sleep(100);
            }

            var buffer = File.ReadAllBytes(output);

            File.Delete(temp);
            File.Delete(output);


            JpegThumbnail = buffer;

            return new MediaUploadData()
            {
                Media = new MemoryStream(buffer),
            };
        }


        public string ThumbnailDirectPath { get; set; }
        public byte[] ThumbnailEncSha256 { get; set; }

        public byte[] ThumbnailSha256 { get; set; }

        internal void SetThumbnail(MediaUploadResult thumbnail, MediaEncryptResult thumbnailResult)
        {
            ThumbnailDirectPath = thumbnail.DirectPath;
            ThumbnailEncSha256 = thumbnailResult.FileEncSha256;
            ThumbnailSha256 = thumbnailResult.FileSha256;
        }
    }
}
