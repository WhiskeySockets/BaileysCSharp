namespace BaileysCSharp.Core.Models.Sending
{
    // types to generate WA messages
    public interface IAnyMessageContent
    {
        public bool? DisappearingMessagesInChat { get; set; }
    }


    public class AnyMessageContent : IAnyMessageContent
    {
        public bool? DisappearingMessagesInChat { get; set; }
    }



    public interface INewsletterMessageContent
    {
    }

    public class NewsletterTextMessage : INewsletterMessageContent 
    {    
        public string Text { get; set; }
    }

}
