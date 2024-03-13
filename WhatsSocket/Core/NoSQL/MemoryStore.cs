using LiteDB;
using Org.BouncyCastle.Bcpg.Sig;
using Proto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Delegates;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.WABinary;

namespace WhatsSocket.Core.NoSQL
{
    public interface IMayHaveID
    {
        string GetID();
    }


    public class Store<T> : IEnumerable<T> where T : IMayHaveID
    {
        private ILiteCollection<T> collection;
        private List<T> list;
        public Store(LiteDatabase database)
        {
            collection = database.GetCollection<T>();
            list = collection.FindAll().ToList();
        }


        public void Add(T item)
        {
            Upsert([item]);
        }


        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public bool Delete(T item)
        {
            list.Remove(item);
            return collection.Delete(item.GetID());

        }

        public void DeleteAll()
        {
            collection.DeleteAll();
            list.Clear();
        }

        public void InsertBulk(List<T> toAdd)
        {
            InsertIfAbsent(toAdd);
        }

        public void Upsert(IEnumerable<T> toAdd)
        {
            InsertIfAbsent(toAdd);
        }

        public T[] InsertIfAbsent(IEnumerable<T> @new)
        {
            List<T> result = new List<T>();
            foreach (var item in @new)
            {
                if (list.Any(x => x.GetID() == item.GetID()))
                {
                    continue;
                }
                list.Add(item);
                collection.Insert(item);
                result.Add(item);
            }
            return result.ToArray();
        }

        internal T? FindByID(string iD)
        {
            return list.FirstOrDefault(x => x.GetID() == iD);
        }

        internal void Update(T existing)
        {
            collection.Update(existing);
        }
    }

    public class MemoryStore
    {
        private static object locker = new object();
        LiteDB.LiteDatabase database;

        Store<ChatModel> chats;
        Store<MessageModel> messages;
        Store<ContactModel> contacts;

        Dictionary<string, List<WebMessageInfo>> messageList;

        public MemoryStore(string root, EventEmitter ev, Logger logger)
        {
            EV = ev;
            Logger = logger;
            database = new LiteDB.LiteDatabase($"{root}\\store.db");
            EV.OnHistorySync += EV_OnHistorySync;
            EV.OnContactChange += EV_OnContactChange;
            EV.OnMessageUpserted += EV_OnMessageUpserted;
            EV.OnMessageUpdated += EV_OnMessageUpdated;
            EV.OnChatUpdated += EV_OnChatUpdated;
            EV.OnChatUpserted += EV_OnChatUpserted;

            chats = new Store<ChatModel>(database);
            contacts = new Store<ContactModel>(database);
            messages = new Store<MessageModel>(database);
            messageList = messages.GroupBy(x => x.RemoteJid).ToDictionary(x => x.Key, x => x.Select(y => y.ToMessageInfo()).ToList());

            Timer checkPoint = new Timer(OnCheckpoint, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
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
                        existing.UnreadCount += update.UnreadCount;
                        existing.Name = update.Name ?? existing.Name;
                        existing.TcToken = update.TcToken ?? existing.TcToken;
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

        private void EV_OnContactChange(BaseSocket sender, ContactModel args)
        {

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
