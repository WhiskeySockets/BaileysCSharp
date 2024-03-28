using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proto
{
    public interface IContextable
    {
        ContextInfo ContextInfo { get; set; }
    }

    public sealed partial class Message
    {
        internal void SetContextInfo(ContextInfo contextInfo)
        {
            var children = this.GetType().GetProperties();
            foreach (var item in children)
            {
                var value = item.GetValue(this, null);
                if (value != null && value is IContextable contextable)
                {
                    contextable.ContextInfo = contextInfo;
                }
            }
        }

        public partial class Types
        {
            public sealed partial class ExtendedTextMessage : IContextable
            {

            }

            public sealed partial class AudioMessage : IContextable
            {

            }

            public sealed partial class ImageMessage : IContextable
            {

            }

            public sealed partial class DocumentMessage : IContextable
            {

            }
            public sealed partial class DocumentMessage : IContextable
            {

            }
        }
    }

}
