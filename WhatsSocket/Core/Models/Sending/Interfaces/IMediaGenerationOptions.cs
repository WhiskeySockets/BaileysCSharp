using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Utils;

namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IMediaGenerationOptions : IMediaUpload
    {
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }

        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
    }
}
