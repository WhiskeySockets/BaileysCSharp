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

        internal static bool GetContentType(Message content)
        {
            if (content == null)
            {
                return false;
            }
            if (content.SenderKeyDistributionMessage != null)
                return false;

            var keys = content.GetType().GetProperties().Where(x => x.Name == "HasConversation" || (x.Name.Contains("Message"))).ToArray();
            foreach (var key in keys)
            {
                var value = key.GetValue(content, null);
                if (value is bool hasValue)
                {
                    if (hasValue)
                        return true;
                }
                if (value != null)
                    return true;
            }


            return false;
        }
    }
}
