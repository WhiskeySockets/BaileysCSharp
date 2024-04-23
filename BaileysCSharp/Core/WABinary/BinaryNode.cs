
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace BaileysCSharp.Core.WABinary
{
    public class BinaryNode 
    {
        private object _content;

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
        public object content { get => _content; set => setContent(value); }

        private void setContent(object value)
        {
            if (value is JArray array)
            {
                _content = array.ToObject<BinaryNode[]>();
            }
            else
            {
                _content = value;
            }
        }

        internal byte[] ToByteArray()
        {
            return content as byte[];
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        internal string? getattr(string attribute)
        {
            if (attrs.ContainsKey(attribute))
            {
                return attrs[attribute];
            }
            return default;
        }

    }
}
