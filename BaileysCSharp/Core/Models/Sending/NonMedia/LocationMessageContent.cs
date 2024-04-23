using static Proto.Message.Types;

namespace BaileysCSharp.Core.Models.Sending
{
    public class LocationMessageContent : AnyMessageContent
    {
        public LocationMessage Location { get; set; }
    }
}
