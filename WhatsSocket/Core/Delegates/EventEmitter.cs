using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Stores;
using WhatsSocket.Core.WABinary;

namespace WhatsSocket.Core.Delegates
{
    public delegate void EventEmitterHandler<T>(BaseSocket sender, T args);
    public class EventEmitter
    {
        public event EventEmitterHandler<QRData> OnQR;
        public event EventEmitterHandler<AuthenticationCreds> OnCredsChange;
        public event EventEmitterHandler<DisconnectReason> OnDisconnect;
        public event EventEmitterHandler<(List<ContactModel> contacts, List<ChatModel> chats, List<WebMessageInfo> messages, bool isLatest)> OnHistorySync;

        public EventEmitter(BaseSocket sender)
        {
            Sender = sender;
        }

        public BaseSocket Sender { get; }


        internal void EmitQR(QRData qr)
        {
            OnQR?.Invoke(Sender, qr);
        }

        //internal void Emit(SessionStore store)
        //{
        //    OnSessionStoreChange?.Invoke(Sender, store);
        //}

        //internal void Emit(KeyStore store)
        //{
        //    OnKeyStoreChange?.Invoke(Sender, store);
        //}

        internal void Emit(AuthenticationCreds creds)
        {
            OnCredsChange.Invoke(Sender, creds);
        }

        internal void Emit(DisconnectReason reason)
        {
            OnDisconnect?.Invoke(Sender, reason);
        }

        public event EventEmitterHandler<ContactModel[]> OnContactUpdated;
        internal void ContactUpdated(ContactModel[] contacts)
        {
            OnContactUpdated?.Invoke(Sender, contacts);
        }


        public event EventEmitterHandler<ContactModel[]> OnContactUpserted;
        internal void ContactUpsert(ContactModel[] contacts)
        {
            OnContactUpserted?.Invoke(Sender, contacts);
        }

        //internal void Emit(AppStateSyncKeyStore appStateSyncKeyStore)
        //{
        //    OnAppStateSyncKeyStoreChange?.Invoke(Sender, appStateSyncKeyStore);
        //}

        //internal void Emit(AppStateSyncVersionStore appStateSyncVersionStore)
        //{
        //    OnAppStateSyncVersionStoreChange?.Invoke(Sender, appStateSyncVersionStore);
        //}

        //internal void Emit(SenderKeyStore senderKeyStore)
        //{
        //    OnSenderKeyStoreChange?.Invoke(Sender, senderKeyStore);
        //}

        public void Flush()
        {
            //what to do here
        }

        public void MessageHistorySet((List<ContactModel> contacts, List<ChatModel> chats, List<WebMessageInfo> messages, bool isLatest) data)
        {
            OnHistorySync?.Invoke(Sender, data);
        }

        public event EventEmitterHandler<MessageUpdate> OnMessageUpdated;
        public void MessageUpdated(MessageUpdate model)
        {
            OnMessageUpdated?.Invoke(Sender, model);
        }

        public event EventEmitterHandler<(Message.Types.ReactionMessage reactionMessage, MessageKey key)> OnMessageReaction;
        internal void MessageReaction(Message.Types.ReactionMessage reactionMessage, MessageKey key)
        {
            OnMessageReaction?.Invoke(Sender, (reactionMessage, key));
        }

        public event EventEmitterHandler<(string jid, string participant, string action)> OnGroupParticipantUpdated;
        internal void GroupParticipantUpdate(string jid, string participant, string action)
        {
            OnGroupParticipantUpdated?.Invoke(Sender, (jid, participant, action));
        }

        public event EventEmitterHandler<(string jid, GroupMetadataModel update)> OnGroupUpdated;
        internal void GroupUpdate(string jid, GroupMetadataModel update)
        {
            OnGroupUpdated?.Invoke(Sender, (jid, update));
        }

        public event EventEmitterHandler<ChatModel[]> OnChatUpdated;
        public void ChatUpdate(ChatModel[] chat)
        {
            OnChatUpdated?.Invoke(Sender, chat);
        }

        public event EventEmitterHandler<ChatModel[]> OnChatUpserted;
        public void ChatUpsert(ChatModel[] chat)
        {
            OnChatUpserted?.Invoke(Sender, chat);
        }

        public event EventEmitterHandler<(WebMessageInfo[] newMessages, string type)> OnMessageUpserted;
        internal void MessageUpsert(WebMessageInfo[] newMessages, string type)
        {
            OnMessageUpserted?.Invoke(Sender, (newMessages, type));
        }

        public event EventEmitterHandler<GroupMetadataModel[]> OnGroupInserted;
        internal void GroupInsert(GroupMetadataModel[] value)
        {
            OnGroupInserted?.Invoke(Sender, value);
        }

        public event EventEmitterHandler<RetryNode[]> OnMessagesMediaUpdate;
        internal void MessagesMediaUpdate(RetryNode[] value)
        {
            OnMessagesMediaUpdate?.Invoke(Sender, value);
        }

        public event EventEmitterHandler<(string[] blocklist, string type)> OnBlockListUpdate;
        internal void BlockListUpdate(string[] blocklist, string type)
        {
            OnBlockListUpdate?.Invoke(Sender, (blocklist, type));
        }
    }
}
