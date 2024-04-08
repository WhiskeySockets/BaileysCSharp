using Proto;
using System.IO;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Utils;

using WhatsSocket.Core.Models.Sending.Interfaces;

namespace WhatsSocket.Core.Models.Sending
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
