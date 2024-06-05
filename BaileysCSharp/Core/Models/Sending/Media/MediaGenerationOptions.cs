using BaileysCSharp.Core.Logging;
using BaileysCSharp.Core.Models.Sending.Interfaces;
using BaileysCSharp.Core.Utils;

namespace BaileysCSharp.Core.Models.Sending.Media
{
    public class MediaGenerationOptions : IMediaGenerationOptions
    {
        public DefaultLogger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }
        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }
}
