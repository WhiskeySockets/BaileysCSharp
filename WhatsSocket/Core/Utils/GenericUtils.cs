using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Utils
{
    public class GenericUtils
    {

        public static BinaryNode? GetBinaryNodeChild(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.FirstOrDefault(x => x.tag == tag);
            }
            return null;
        }
        public static BinaryNode[] GetBinaryNodeChildren(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.Where(x => x.tag == tag).ToArray();
            }
            return new BinaryNode[0];
        }

    }
}
