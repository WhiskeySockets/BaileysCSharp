using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Events.Stores
{
    public class GroupParticipantEventStore : DataEventStore<GroupParticipantUpdateModel>
    {
        public GroupParticipantEventStore() : base(false)
        {
        }

        public event EventHandler<GroupParticipantUpdateModel> Update;

        public override void Execute(EmitType value, GroupParticipantUpdateModel[] args)
        {
            foreach (var item in args)
            {
                switch (value)
                {
                    case EmitType.Update:
                        Update?.Invoke(this, item);
                        break;
                }
            }
        }
    }

}
