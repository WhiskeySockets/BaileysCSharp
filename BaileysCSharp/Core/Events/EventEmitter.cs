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
using BaileysCSharp.Core.Logging;
using System.Collections.Concurrent;
using BaileysCSharp.Core.Models.Newsletters;
using static BaileysCSharp.Core.Models.Newsletters.NewsletterSettings;

namespace BaileysCSharp.Core.Events
{

    public delegate void EventEmitterHandler<T>(T args);
    public class EventEmitter
    {
        private object locker = new object();
        ConcurrentDictionary<string, IEventStore> Events = new ConcurrentDictionary<string, IEventStore>();
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
        public NewsletterMetaDataEventStore Newsletter { get; set; }
        public SyncDataEventStore SyncData { get; set; }

        public ILogger Logger { get; }

        public EventEmitter(ILogger logger)
        {
            Events[typeof(ContactModel).Name] = Contacts = new ContactsEventStore();
            Events[typeof(MessageHistoryModel).Name] = MessageHistory = new MessageHistoryEventStore();
            Events[typeof(ChatModel).Name] = Chats = new ChatEventStore();
            Events[typeof(ConnectionState).Name] = Connection = new ConnectionEventStore();
            Events[typeof(AuthenticationCreds).Name] = Auth = new AuthEventStore();
            Events[typeof(MessageEventModel).Name] = Message = new MessagingEventStore();
            Events[typeof(PresenceModel).Name] = Pressence = new PressenceEventStore();
            Events[typeof(MessageReceipt).Name] = Receipt = new MessageReceiptEventStore();
            Events[typeof(MessageReactionModel).Name] = Reaction = new ReactionEventStore();
            Events[typeof(GroupParticipantUpdateModel).Name] = GroupParticipant = new GroupParticipantEventStore();
            Events[typeof(GroupMetadataModel).Name] = Group = new GroupMetaDataEventStore();
            Events[typeof(NewsletterMetaData).Name] = Newsletter = new NewsletterMetaDataEventStore();
            Events[typeof(SyncState).Name] = SyncData = new SyncDataEventStore();

            Logger = logger;
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

        private bool InternalEmit<T>(EmitType type, params T[] args)
        {
            lock (locker)
            {
                var eventkey = $"{typeof(T).Name}";
                if (!Events.TryGetValue(eventkey, out var store))
                {
                    Logger.Warn($"{eventkey}.{type} has not been implemented yet");
                    return false;
                }
                ((DataEventStore<T>)store).Emit(type, args);
            }
            return true;
        }

        internal void Emit(EmitType action, params ContactModel[] value)
        {
            InternalEmit(action, value);
        }

        internal void Emit(EmitType action, params NewsletterMetaData[] value)
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
        internal void Emit(EmitType action, params MessageUpdate[] messageUpdates)
        {
            InternalEmit(action, messageUpdates);
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
        internal void Emit(EmitType action, SyncState syncState)
        {
            InternalEmit(action, syncState);
        }

        /** Receive an update on a call, including when the call was received, rejected, accepted */
        //'call': WACallEvent[]
        //'labels.edit': Label
        //'labels.association': { association: LabelAssociation, type: 'add' | 'remove' }
    }
}
