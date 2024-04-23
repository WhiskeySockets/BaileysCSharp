using Proto;
using BaileysCSharp.Core.Models.Sending.Interfaces;
using BaileysCSharp.Core.Models.Sending.Media;

namespace BaileysCSharp.Core.Models.Sending.NonMedia
{
    public class MessageContentGenerationOptions : MediaGenerationOptions
    {



    }

    public class MessageGenerationOptionsFromContent : IMiscMessageGenerationOptions
    {
        public WebMessageInfo Quoted { get; set; }
        public ulong Timestamp { get; set; }
        public ulong? EphemeralExpiration { get; set; }
        public List<string>? StatusJidList { get; set; }
        public string MessageID { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }
}
