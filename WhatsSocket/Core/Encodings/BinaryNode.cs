
namespace WhatsSocket.Core.Encodings
{
    public class BinaryNode
    {
        public BinaryNode()
        {
            attrs = new Dictionary<string, string>();
        }
        public string tag { get; set; }
        public Dictionary<string, string> attrs { get; set; }
        public object content { get; set; }

        internal byte[] ToByteArray()
        {
            return content as byte[];
        }
    }


    public class JidWidhDevice
    {
        public string User { get; set; }
        public int? Device { get; set; }
    }
    public class FullJid : JidWidhDevice
    {
        public string Server { get; set; }
        public int? DomainType { get; set; }
    }
}
