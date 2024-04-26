using Google.Protobuf.WellKnownTypes;
using Proto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Events.Stores;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Core.WABinary;
using ConnectionState = BaileysCSharp.Core.Types.ConnectionState;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Events
{

    public delegate void EventEmitterHandler<T>(T args);
    public class EventEmitter
    {
        private object locker = new object();
        Dictionary<string, IEventStore> Events = new Dictionary<string, IEventStore>();
        public BaseSocket Sender { get; }



        public ContactsEventStore Contacts { get; set; }
        public MessageHistoryEventStore MessageHistory{ get; set; }
        public ChatEventStore Chats { get; set; }
        public ConnectionEventStore Connection { get; set; }
        public AuthEventStore Auth { get; set; }
        public MessagingEventStore Message { get; set; }
        public PressenceEventStore Pressence { get; set; }
        public MessageReceiptEventStore Receipt { get; set; }
        public ReactionEventStore Reaction { get; set; }
        public GroupParticipantEventStore GroupParticipant { get; set; }
        public GroupMetaDataEventStore Group { get; set; }

        public EventEmitter()
        {
            Events[typeof(ContactModel).Name] = Contacts = new ContactsEventStore();
            Events[typeof(MessageHistoryEventStore).Name] = MessageHistory = new MessageHistoryEventStore();
            Events[typeof(ChatModel).Name] = Chats = new ChatEventStore();
            Events[typeof(ConnectionState).Name] = Connection = new ConnectionEventStore();
            Events[typeof(AuthenticationCreds).Name] = Auth = new AuthEventStore();
            Events[typeof(MessageEventModel).Name] = Message = new MessagingEventStore();
            Events[typeof(PresenceModel).Name] = Pressence = new PressenceEventStore();
            Events[typeof(MessageReceipt).Name] = Receipt = new MessageReceiptEventStore();
            Events[typeof(MessageReactionModel).Name] = Reaction = new ReactionEventStore();
            Events[typeof(GroupParticipantEventStore).Name] = GroupParticipant = new GroupParticipantEventStore();
            Events[typeof(GroupMetadataModel).Name] = Group = new GroupMetaDataEventStore();



        }


        public bool Flush(bool force = false)
        {
            lock (locker)
            {
                if (buffersInProgress == 0)
                {
                    return false;
                }
                if (!force)
                {
                    buffersInProgress--;
                    if (buffersInProgress > 0)
                    {
                        return false;
                    }
                }
                foreach (var item in Events)
                {
                    item.Value.Flush();
                }
            }

            return true;
        }

        public long buffersInProgress = 0;

        public void Buffer()
        {
            lock (locker)
            {
                buffersInProgress++;
            }
        }


        public string[] BufferableEvent = [


            $"{typeof(MessagingHistory)}.{EmitType.Set}",

            $"{typeof(ChatModel)}.{EmitType.Upsert}",
            $"{typeof(ChatModel)}.{EmitType.Upsert}",
            $"{typeof(ChatModel)}.{EmitType.Delete}",

            $"{typeof(ContactModel)}.{EmitType.Upsert}",
            $"{typeof(ContactModel)}.{EmitType.Update}",

            $"{typeof(MessageEventModel)}.{EmitType.Upsert}",
            $"{typeof(MessageModel)}.{EmitType.Upsert}",
            $"{typeof(MessageModel)}.{EmitType.Update}",
            $"{typeof(MessageModel)}.{EmitType.Delete}",
            $"{typeof(MessageModel)}.{EmitType.Reaction}",

            //MessageUpsertModel

            //$"MessageReceipt.{EmitType.Update}",
            //$"Group.{EmitType.Reaction}",

            ];

        //public bool Emit<T>(EmitType type, params T[] args)
        //{
        //    lock (locker)
        //    {
        //        var eventkey = $"{typeof(T)}.{type}";
        //        if (!GroupedEvents.ContainsKey(eventkey))
        //        {
        //            GroupedEvents[eventkey] = new Dictionary<EmitType, IEventStore>();
        //        }
        //        var events = GroupedEvents[eventkey];
        //        if (!events.ContainsKey(type))
        //        {
        //            events[type] = new EventStore<T>(Sender, BufferableEvent.Contains($"{typeof(T)}.{type}"));
        //        }
        //        var store = (EventStore<T>)events[type];
        //        store.Append(args);
        //    }
        //    return true;
        //}

        private bool InternalEmit<T>(EmitType type, params T[] args)
        {
            lock (locker)
            {
                var eventkey = $"{typeof(T).Name}";
                var store = (DataEventStore<T>)Events[eventkey];
                store.Emit(type, args);
            }
            return true;
        }

        internal void Emit(EmitType action, params ContactModel[] value)
        {
            InternalEmit(action, value);
        }

        internal void Emit(EmitType action, MessageHistoryModel value)
        {
            InternalEmit(action, value);
        }


        internal void Emit(EmitType action, ChatModel value)
        {
            InternalEmit(action, value);
        }

        internal void Emit(EmitType action, ConnectionState connectionState)
        {
            InternalEmit(action, connectionState);
        }

        internal void Emit(EmitType action, AuthenticationCreds creds)
        {
            InternalEmit(action, creds);
        }

        internal void Emit(EmitType action, MessageEventModel messageEvent)
        {
            InternalEmit(action, messageEvent);
        }


        internal void Emit(EmitType action, PresenceModel presenceModel)
        {
            InternalEmit(action, presenceModel);
        }

        public void Emit(EmitType action, params MessageReceipt[] messageReceipts)
        {
            InternalEmit(action, messageReceipts);
        }

        internal void Emit(EmitType action, params MessageUpdate[] value)
        {
            //TODO: figure out the last one
        }


        internal void Emit(EmitType action, MessageReactionModel messageReactionModel)
        {
            InternalEmit(action, messageReactionModel);
        }

        internal void Emit(EmitType action, GroupParticipantUpdateModel groupParticipantUpdateModel)
        {
            InternalEmit(action, groupParticipantUpdateModel);
        }


        internal void Emit(EmitType action, GroupMetadataModel metadata)
        {
            InternalEmit(action, metadata);
        }

        /** Receive an update on a call, including when the call was received, rejected, accepted */
        //'call': WACallEvent[]
        //'labels.edit': Label
        //'labels.association': { association: LabelAssociation, type: 'add' | 'remove' }
    }
}
