namespace WhatsSocket.Core.Models
{
    public class FullJid : JidWidhDevice
    {
        public string Server { get; set; }
        public int? DomainType { get; set; }
    }
}
