using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Events.Stores
{
    public class ContactsEventStore : DataEventStore<ContactModel>
    {

        public ContactsEventStore() : base(true)
        {
        }

        public event EventHandler<ContactModel[]> Upsert;
        public event EventHandler<ContactModel[]> Update;

        public override void Execute(EmitType value, ContactModel[] args)
        {
            switch (value)
            {
                case EmitType.Upsert:
                    Upsert?.Invoke(this, args);
                    break;
                case EmitType.Update:
                    Update?.Invoke(this, args);
                    break;
            }
        }
    }

}
