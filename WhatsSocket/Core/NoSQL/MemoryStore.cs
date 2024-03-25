using LiteDB;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg.Sig;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.WABinary;

namespace WhatsSocket.Core.NoSQL
{

    public class MemoryStore
    {
        private static object locker = new object();
        LiteDB.LiteDatabase database;

        Store<ChatModel> chats;
        Store<MessageModel> messages;
        Store<ContactModel> contacts;
        Store<GroupMetadataModel> groupMetaData;

        Dictionary<string, List<WebMessageInfo>> messageList;
        public ConnectionState State { get; set; }

        public MemoryStore(string root, EventEmitter ev, Logger logger)
        {
            State = new ConnectionState();
            EV = ev;
            Logger = logger;
            database = new LiteDB.LiteDatabase($"{root}\\store.db");
            //EV.OnHistorySync += EV_OnHistorySync;

            var historyEvent = EV.On<MessageHistoryModel>(EmitType.Set);
            historyEvent.Emit += HistoryEvent_Emit;

            var connectionEvent = EV.On<ConnectionState>(EmitType.Update);
            connectionEvent.Emit += ConnectionEvent_Emit;

            var contactUpdateEvent = EV.On<ContactModel>(EmitType.Update);
            contactUpdateEvent.Emit += ContactUpdateEvent_Emit;
            var contactUpsert = EV.On<ContactModel>(EmitType.Upsert);
            contactUpsert.Emit += ContactUpsert_Emit;
            //EV.OnContactUpdated += EV_OnContactUpdated;
            //EV.ContactsUpsert.OnEmit += ContactsUpsert_OnEmit;

            var messageUpdateEvent = EV.On<WebMessageInfo>(EmitType.Update);
            messageUpdateEvent.Emit += MessageUpdateEvent_Emit;
            var messageUpsert = EV.On<MessageUpsertModel>(EmitType.Upsert);
            messageUpsert.Emit += MessageUpsert_Emit;
            var messageDelete = EV.On<MessageUpdate>(EmitType.Delete);
            messageDelete.Emit += MessageDelete_Emit;
            //EV.OnMessageUpserted += EV_OnMessageUpserted;
            //EV.OnMessageUpdated += EV_OnMessageUpdated;
            //EV.OnMessagesDeleted += EV_OnMessagesDeleted;

            var chatUpsertEvent = EV.On<ChatModel>(EmitType.Upsert);
            chatUpsertEvent.Emit += ChatUpsertEvent_Emit;
            var chatUpdateEvent = EV.On<ChatModel>(EmitType.Update);
            chatUpdateEvent.Emit += ChatUpdateEvent_Emit;
            var chatDeleteEvent = EV.On<ChatModel>(EmitType.Delete);
            chatDeleteEvent.Emit += ChatDeleteEvent_Emit;
            //EV.OnChatUpserted += EV_OnChatUpserted;
            //EV.OnChatUpdated += EV_OnChatUpdated;
            //EV.OnChatDeleted += EV_OnChatDeleted;


            var groupUpdateEvent = EV.On<GroupMetadataModel>(EmitType.Update);
            groupUpdateEvent.Emit += GroupUpdateEvent_Emit;
            var groupUpsertEvent = EV.On<GroupMetadataModel>(EmitType.Upsert);
            groupUpsertEvent.Emit += GroupUpsertEvent_Emit;
            //EV.OnGroupsUpsert += EV_OnGroupInserted;
            //EV.OnGroupUpdated += EV_OnGroupUpdated;

            //EV.OnMessagesMediaUpdate += EV_OnMessagesMediaUpdate;
            //EV.OnBlockListUpdate += EV_OnBlockListUpdate;

            chats = new Store<ChatModel>(database);
            contacts = new Store<ContactModel>(database);
            messages = new Store<MessageModel>(database);
            groupMetaData = new Store<GroupMetadataModel>(database);

            var json = JsonConvert.SerializeObject(contacts.ToArray());

            messageList = messages.GroupBy(x => x.RemoteJid).ToDictionary(x => x.Key, x => x.Select(y => y.ToMessageInfo()).ToList());

            Timer checkPoint = new Timer(OnCheckpoint, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private void ConnectionEvent_Emit(BaseSocket sender, ConnectionState[] args)
        {
            State.Connection = args[0].Connection;
            State.QR = args[0].QR;
            State.LastDisconnect = args[0].LastDisconnect;
            State.IsOnline = args[0].IsOnline;
            State.ReceivedPendingNotifications = args[0].ReceivedPendingNotifications;
            State.IsNewLogin = args[0].IsNewLogin;
        }

        private void GroupUpsertEvent_Emit(BaseSocket sender, GroupMetadataModel[] args)
        {
            lock (locker)
            {
                groupMetaData.InsertBulk(args);
            }
        }


        private void GroupUpdateEvent_Emit(BaseSocket sender, GroupMetadataModel[] args)
        {
            ///
        }

        private void ChatDeleteEvent_Emit(BaseSocket sender, ChatModel[] args)
        {
            ///TODO
        }

        private void ChatUpdateEvent_Emit(BaseSocket sender, ChatModel[] args)
        {
            lock (locker)
            {
                changes = true;
                foreach (var update in args)
                {
                    var existing = chats.FindByID(update.ID);
                    if (existing != null)
                    {
                        existing.Update(update);
                        chats.Update(existing);
                    }
                }
            }
        }

        private void ChatUpsertEvent_Emit(BaseSocket sender, ChatModel[] args)
        {
            lock (locker)
            {
                changes = true;
                chats.Upsert(args);
            }
        }

        private void ContactUpsert_Emit(BaseSocket sender, ContactModel[] args)
        {
            ContactsUpsert(args.ToList());
        }

        private void ContactUpdateEvent_Emit(BaseSocket sender, ContactModel[] args)
        {
            ///TODO
        }


        private ContactModel[] ContactsUpsert(List<ContactModel> newContacts)
        {
            var oldContacts = newContacts.ToList();
            List<ContactModel> toAdd = new List<ContactModel>();
            foreach (var item in newContacts)
            {
                if (!toAdd.Any(x => x.ID == item.ID))
                {
                    toAdd.Add(item);
                }
                oldContacts.Remove(item);
            }
            contacts.InsertBulk(toAdd);
            return oldContacts.ToArray();
        }

        private void MessageDelete_Emit(BaseSocket sender, MessageUpdate[] args)
        {
            ///TODO
        }

        private void MessageUpsert_Emit(BaseSocket sender, MessageUpsertModel[] args)
        {
            lock (locker)
            {
                foreach (var item in args)
                {

                    changes = true;
                    switch (item.Type)
                    {
                        case MessageUpsertType.Append:
                        case MessageUpsertType.Notify:
                            foreach (var msg in item.Messages)
                            {
                                var jid = JidUtils.JidNormalizedUser(msg.Key.RemoteJid);
                                if (messageList.ContainsKey(jid) == false)
                                {
                                    messageList[jid] = new List<WebMessageInfo>();
                                }
                                messageList[jid].Add(msg);
                                messages.Add(new MessageModel(msg));
                                EV.Emit(EmitType.Upsert, [new ChatModel()
                            {
                                ID = jid,
                                ConversationTimestamp = msg.MessageTimestamp,
                                UnreadCount = 1
                            }]);
                                //EV.ChatsUpsert();
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void MessageUpdateEvent_Emit(BaseSocket sender, WebMessageInfo[] args)
        {
            ///TODO
        }

        private void HistoryEvent_Emit(BaseSocket sender, MessageHistoryModel[] args)
        {
            foreach (var item in args)
            {

                lock (locker)
                {
                    changes = true;
                    if (item.IsLatest)
                    {
                        chats.DeleteAll();
                        messages.DeleteAll();
                    }
                    var chatsAdded = chats.InsertIfAbsent(item.Chats);
                    Logger.Debug(new { chatsAdded }, "synced chats");

                    var oldContacts = ContactsUpsert(item.Contacts);
                    if (item.IsLatest)
                    {
                        foreach (var contact in oldContacts)
                        {
                            var deleted = contacts.Delete(contact);
                        }
                    }

                    Logger.Debug(new { deletedContacts = item.IsLatest ? oldContacts.Length : 0, item.Contacts }, "synced contacts");

                    var newMessages = new List<MessageModel>();
                    foreach (var msg in item.Messages)
                    {
                        var storeMessage = new MessageModel(msg);
                        var jid = msg.Key.RemoteJid;
                        if (messageList.ContainsKey(jid) == false)
                        {
                            messageList[jid] = new List<WebMessageInfo>();
                        }
                        messageList[jid].Insert(0, msg);
                        newMessages.Add(storeMessage);

                    }
                    messages.InsertIfAbsent(newMessages);

                    Logger.Debug(new { messages = item.Messages.Count }, "synced messages");
                }
            }
        }


        bool changes = true;
        private void OnCheckpoint(object? state)
        {
            lock (locker)
            {
                if (changes)
                {
                    try
                    {
                        database.Checkpoint();
                    }
                    catch (Exception)
                    {

                    }
                }
                changes = false;
            }
        }


        public EventEmitter EV { get; }
        public Logger Logger { get; }
    }
}
