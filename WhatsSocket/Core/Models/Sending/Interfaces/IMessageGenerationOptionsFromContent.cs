namespace WhatsSocket.Core.Models.Sending.Interfaces
{
    public interface IMessageGenerationOptionsFromContent : IMiscMessageGenerationOptions
    {
        public string UserJid { get; set; }
    }
}
