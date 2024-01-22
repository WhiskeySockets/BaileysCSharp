
namespace WhatsSocket.Core.Encodings
{
    public class BinaryNode
    {
        public BinaryNode()
        {
            attrs = new Dictionary<string, string>();
        }
        public BinaryNode(string tag) : this()
        {
            this.tag = tag;
        }
        public BinaryNode(string tag, object content) : this(tag)
        {
            this.content = content;
        }
        public BinaryNode(string tag, params BinaryNode[] content) : this(tag)
        {
            this.content = content;
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
