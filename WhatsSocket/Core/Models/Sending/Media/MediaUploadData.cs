namespace WhatsSocket.Core.Models.Sending.Media
{
    public class MediaUploadData
    {
        public Stream Media { get; set; }
        public string Caption { get; set; }
        public bool Ptt { get; set; }
        public long Seconds { get; set; }
        public bool GifPlayback { get; set; }
        public string FileName { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public string Mimeype { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public byte[] WaveForm { get; set; }
        public long? BackgroundArgb { get; set; }
    }
}
