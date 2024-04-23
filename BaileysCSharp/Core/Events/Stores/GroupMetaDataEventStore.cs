using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Events.Stores
{
    public class GroupMetaDataEventStore : DataEventStore<GroupMetadataModel>
    {
        public GroupMetaDataEventStore() : base(false)
        {
        }

        public event EventHandler<GroupMetadataModel> Update;
        public event EventHandler<GroupMetadataModel> Upsert;

        public override void Execute(EmitType value, GroupMetadataModel[] args)
        {
            foreach (var item in args)
            {
                switch (value)
                {
                    case EmitType.Upsert:
                        Upsert?.Invoke(this, item);
                        break;
                    case EmitType.Update:
                        Update?.Invoke(this, item);
                        break;
                }
            }
        }
    }

}
