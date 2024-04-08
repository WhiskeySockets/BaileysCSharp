namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IMediaUpload   
    {
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }
}
