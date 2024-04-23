using FFMpegCore;
using Newtonsoft.Json;
using Proto;
using SkiaSharp;
using System.Diagnostics;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models.Sending.Interfaces;
using static Proto.Message.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BaileysCSharp.Core.Models.Sending.Media
{
    public class AudioMessageContent : AnyMediaMessageContent
    {
        private MemoryStream audio;

        public AudioMessageContent()
        {
            Property = "AudioMessage";
        }

        public string Caption { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public uint Seconds { get; set; }
        public ulong FileLength { get; set; }
        public string Mimetype { get; set; }
        public bool Ptt { get; private set; }

        public Stream Audio
        {
            get => audio;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            Ptt = false;
            if (value is MemoryStream memoryStream)
            {
                audio = memoryStream;
            }
            else
            {
                audio = new MemoryStream();
                value.CopyTo(audio);
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
            Seconds = (uint)mediaInfo.Duration.TotalSeconds;


            if (Ptt)
            {
                //TODO Ptt


            }
            File.Delete(temp);
        }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new AudioMessage()
            {
                ContextInfo = ContextInfo,
                Mimetype = Mimetype ?? "audio/mp4",
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seconds = Seconds,
                Ptt = Ptt
            };
            return image;
        }
    }
}
