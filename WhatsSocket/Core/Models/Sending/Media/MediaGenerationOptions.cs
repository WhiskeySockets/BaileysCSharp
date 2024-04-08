using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models.Sending.Interfaces;
using WhatsSocket.Core.Utils;

namespace WhatsSocket.Core.Models.Sending.Media
{
    public class MediaGenerationOptions : IMediaGenerationOptions
    {
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }
        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }
}
