using LiteDB;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg.Sig;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public MemoryStore(string root, EventEmitter ev, Logger logger)
        {
            EV = ev;
            Logger = logger;
            database = new LiteDB.LiteDatabase($"{root}\\store.db");
            EV.OnHistorySync += EV_OnHistorySync;

            EV.OnContactUpdated += EV_OnContactUpdated;
            EV.OnContactUpserted += EV_OnContactUpserted;

            EV.OnMessageUpserted += EV_OnMessageUpserted;
            EV.OnMessageUpdated += EV_OnMessageUpdated;
            EV.OnMessagesDeleted += EV_OnMessagesDeleted;

            EV.OnChatUpserted += EV_OnChatUpserted;
            EV.OnChatUpdated += EV_OnChatUpdated;

            EV.OnGroupInserted += EV_OnGroupInserted;
            EV.OnGroupUpdated += EV_OnGroupUpdated;

            EV.OnMessagesMediaUpdate += EV_OnMessagesMediaUpdate;
            EV.OnBlockListUpdate += EV_OnBlockListUpdate;

            chats = new Store<ChatModel>(database);
            contacts = new Store<ContactModel>(database);
            messages = new Store<MessageModel>(database);
            groupMetaData = new Store<GroupMetadataModel>(database);

            var json = JsonConvert.SerializeObject(contacts.ToArray());

            messageList = messages.GroupBy(x => x.RemoteJid).ToDictionary(x => x.Key, x => x.Select(y => y.ToMessageInfo()).ToList());

            Timer checkPoint = new Timer(OnCheckpoint, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private void EV_OnMessagesDeleted(BaseSocket sender, MessageModel[] args)
        {
            //TODO:
        }

        private void EV_OnBlockListUpdate(BaseSocket sender, (string[] blocklist, string type) args)
        {
            //TODO:
        }

        private void EV_OnMessagesMediaUpdate(BaseSocket sender, RetryNode[] args)
        {
            //TODO:
        }

        private void EV_OnGroupUpdated(BaseSocket sender, (string jid, GroupMetadataModel update) args)
        {
            //TODO:
        }

        private void EV_OnGroupInserted(BaseSocket sender, GroupMetadataModel[] args)
        {
            groupMetaData.InsertBulk(args);
        }

        private void EV_OnChatUpserted(BaseSocket sender, ChatModel[] args)
        {
            chats.Upsert(args);
        }

        private void EV_OnChatUpdated(BaseSocket sender, ChatModel[] args)
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

        private void EV_OnMessageUpserted(BaseSocket sender, (WebMessageInfo[] newMessages, string type) args)
        {
            lock (locker)
            {
                changes = true;
                switch (args.type)
                {
                    case "append":
                    case "notify":
                        foreach (var msg in args.newMessages)
                        {
                            var jid = JidUtils.JidNormalizedUser(msg.Key.RemoteJid);
                            if (messageList.ContainsKey(jid) == false)
                            {
                                messageList[jid] = new List<WebMessageInfo>();
                            }
                            messageList[jid].Add(msg);
                            messages.Add(new MessageModel(msg));
                            EV.ChatUpsert([new ChatModel()
                            {
                                ID= jid,
                                ConversationTimestamp = msg.MessageTimestamp,
                                UnreadCount=1
                            }]);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void EV_OnMessageUpdated(BaseSocket sender, MessageUpdate args)
        {

        }

        private void EV_OnContactUpdated(BaseSocket sender, ContactModel[] args)
        {
            lock (locker)
            {
                changes = true;
                foreach (var update in args)
                {
                    var existing = contacts.FindByID(update.ID);
                    if (existing != null)
                    {
                        existing.Update(update);
                        contacts.Update(existing);
                    }
                }
            }
        }

        private void EV_OnContactUpserted(BaseSocket sender, ContactModel[] args)
        {
            contacts.InsertBulk(args);
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

        private void EV_OnHistorySync(BaseSocket sender, (List<Models.ContactModel> newContacts, List<Models.ChatModel> chats, List<Proto.WebMessageInfo> newMessages, bool isLatest) args)
        {
            lock (locker)
            {
                changes = true;
                if (args.isLatest)
                {
                    chats.DeleteAll();
                    messages.DeleteAll();
                }
                var chatsAdded = chats.InsertIfAbsent(args.chats);
                Logger.Debug(new { chatsAdded }, "synced chats");

                var oldContacts = ContactsUpsert(args.newContacts);
                if (args.isLatest)
                {
                    foreach (var item in oldContacts)
                    {
                        var deleted = contacts.Delete(item);
                    }
                }

                Logger.Debug(new { deletedContacts = args.isLatest ? oldContacts.Length : 0, args.newContacts }, "synced contacts");

                var newMessages = new List<MessageModel>();
                foreach (var msg in args.newMessages)
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

                Logger.Debug(new { messages = args.newMessages.Count }, "synced messages");
            }
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

        public EventEmitter EV { get; }
        public Logger Logger { get; }
    }
}
