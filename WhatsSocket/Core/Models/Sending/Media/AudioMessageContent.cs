using FFMpegCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities;
using Proto;
using SkiaSharp;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models.Sending.Interfaces;
using static Proto.Message.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WhatsSocket.Core.Models.Sending.Media
{
    public class AudioMessageContent : AnyMediaMessageContent
    {
        private MemoryStream audio;

        public string Caption { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public uint Seconds { get; set; }
        public ulong FileLength { get; set; }
        public string Mimetype { get; set; }

        public Stream Audio
        {
            get => audio;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            if (value is MemoryStream memoryStream)
            {
                audio = memoryStream;
            }
            else
            {
                memoryStream = new MemoryStream();
                value.CopyTo(memoryStream);
                audio = memoryStream;

            }
            audio.Position = 0;
            FileLength = (ulong)audio.Length;


        }

        public override async Task Process()
        {
            byte[] copy = audio.ToArray();
            audio.Position = 0;



            var temp = Path.GetTempFileName();
            File.WriteAllBytes(temp, copy);
            var mediaInfo = await FFProbe.AnalyseAsync(temp);
            File.Delete(temp);
            Seconds = (uint)mediaInfo.Duration.TotalSeconds;
        }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new AudioMessage()
            {
                ContextInfo = ContextInfo,
                Mimetype = Mimetype ?? "audio/mp4",
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seconds = Seconds,
            };
            return image;
        }
    }
}
