using BaileysCSharp.Core.Models.Newsletters;

namespace BaileysCSharp.Core.Events.Stores
{
    public class NewsletterMetaDataEventStore : DataEventStore<NewsletterMetaData>
    {

        public NewsletterMetaDataEventStore() : base(false)
        {
        }

        public event EventHandler<NewsletterMetaData[]> Upsert;
        public event EventHandler<NewsletterMetaData[]> Update;
        public event EventHandler<NewsletterMetaData[]> Delete;

        public override void Execute(EmitType value, NewsletterMetaData[] args)
        {
            switch (value)
            {
                case EmitType.Upsert:
                    Upsert?.Invoke(this, args);
                    break;
                case EmitType.Update:
                    Update?.Invoke(this, args);
                    break;
                case EmitType.Delete:
                    Delete?.Invoke(this, args);
                    break;
            }
        }
    }

}
