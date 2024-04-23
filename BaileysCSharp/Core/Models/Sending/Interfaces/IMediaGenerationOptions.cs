using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Utils;

namespace BaileysCSharp.Core.Models.Sending.Interfaces
{
    public interface IMediaGenerationOptions : IMediaUpload
    {
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }

        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
    }
}
