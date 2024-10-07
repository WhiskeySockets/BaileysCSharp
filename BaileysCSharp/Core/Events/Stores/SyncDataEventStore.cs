using BaileysCSharp.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Events.Stores
{
   
    public class SyncDataEventStore : DataEventStore<SyncState>
    {
        public SyncDataEventStore() : base(false)
        {
        }

        public event EventHandler<SyncState> Update;

        public override void Execute(EmitType value, SyncState[] args)
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
