using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Proto.Message.Types;

namespace WhatsSocket.Core.Utils
{
    public class MessageUtil
    {
        public static Message NormalizeMessageContent(Message? content)
        {
            if (content == null)
                return null;

            // set max iterations to prevent an infinite loop
            for (var i = 0; i < 5; i++)
            {
                var inner = GetFutureProofMessage(content);

                if (inner == null)
                {
                    break;

                }

                content = inner.Message;
            }
            return content;
        }



        public static FutureProofMessage? GetFutureProofMessage(Message content)
        {
            return content.EphemeralMessage ??
              content.ViewOnceMessage ??
              content.DocumentWithCaptionMessage ??
              content.ViewOnceMessageV2 ??
              content.ViewOnceMessageV2Extension ??
              content.EditedMessage;
        }

        internal static string GetContentType(Message content)
        {
            if (content != null)
            {
                return "what to do here ?";
            }
            return null;
        }
    }
}
