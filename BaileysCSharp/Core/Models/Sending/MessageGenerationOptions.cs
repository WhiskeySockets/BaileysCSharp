using Proto;
using System.IO;
using BaileysCSharp.Core.Extensions;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Utils;

using BaileysCSharp.Core.Models.Sending.Interfaces;

namespace BaileysCSharp.Core.Models.Sending
{
    public class MessageGenerationOptions : IMessageGenerationOptions
    {
        public MessageGenerationOptions(IMiscMessageGenerationOptions? options)
        {
            this.CopyMatchingValues(options);
        }

        public string UserJid { get; set; }
        public ulong Timestamp { get; set; }
        public WebMessageInfo Quoted { get; set; }
        public ulong? EphemeralExpiration { get; set; }
        public string MessageID { get; set; }
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }
        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
        public List<string>? StatusJidList { get; set; }
    }
}
