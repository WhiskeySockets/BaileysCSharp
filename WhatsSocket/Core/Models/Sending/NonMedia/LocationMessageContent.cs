using static Proto.Message.Types;

namespace WhatsSocket.Core.Models.Sending
{
    public class LocationMessageContent : AnyMessageContent
    {
        public LocationMessage Location { get; set; }
    }
}
