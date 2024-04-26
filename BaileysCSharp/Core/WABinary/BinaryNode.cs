using BaileysCSharp.Core.Helper;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            if (value is JsonArray array)
            {
                _content = array.Deserialize<BinaryNode[]>();
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
            return JsonSerializer.Serialize(this, JsonHelper.Options);
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
