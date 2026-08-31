using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Extensions;
using BaileysCSharp.Core.Logging;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Types;
using BaileysCSharp.Core.Utils;
using Google.Protobuf;
using LiteDB;
using Proto;
using static BaileysCSharp.Core.Utils.JidUtils;

namespace BaileysCSharp.Core.NoSQL
{
    public class MemoryStore : IDisposable
    {
        

        private static object locker = new object();
        LiteDB.LiteDatabase database;

        Store<ChatModel> chats;
        Store<MessageModel> messages;
        Store<ContactModel> contacts;
        Store<GroupMetadataModel> groupMetaData;

        Dictionary<string, List<WebMessageInfo>> messageList;
        public ConnectionState State { get; set; }

        public MemoryStore(string root, EventEmitter ev, DefaultLogger logger)
        {
            State = new ConnectionState();
            EV = ev;
            Logger = logger;
            database = new LiteDatabase($"{root}\\store.db");

            chats = new Store<ChatModel>(database);
            contacts = new Store<ContactModel>(database);
            messages = new Store<MessageModel>(database);
            groupMetaData = new Store<GroupMetadataModel>(database);

            EV.MessageHistory.Set += MessageHistory_Set;

            EV.Connection.Update += Connection_Update;


            EV.Contacts.Update += Contacts_Update;
            EV.Contacts.Upsert += Contacts_Upsert;

            EV.Message.Upsert += Message_Upsert;
            EV.Message.Update += Message_Update;
            EV.Message.Delete += Message_Delete;
            EV.Receipt.Upsert += Receipt_Upsert;


            EV.Chats.Upsert += Chats_Upsert;
            EV.Chats.Update += Chats_Update;
            EV.Chats.Delete += Chats_Delete;


            EV.Group.Update += Group_Update;
            EV.Group.Upsert += Group_Upsert;

            EV.GroupParticipant.Update += GroupParticipant_Update;

            //EV.OnMessagesMediaUpdate += EV_OnMessagesMediaUpdate;
            //EV.OnBlockListUpdate += EV_OnBlockListUpdate;


            messageList = messages.Where(x => x.RemoteJid != null).GroupBy(x => x.RemoteJid).ToDictionary(x => x.Key, x => x.Select(y => y.ToMessageInfo()).ToList());
            StartTimer();
            //Timer checkPoint = new Timer(OnCheckpoint, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public void Destroy()
        {
            database.Dispose();
        }

        private void GroupParticipant_Update(object? sender, GroupParticipantUpdateModel e)
        {
            //TODO:
        }

        private void Group_Upsert(object? sender, GroupMetadataModel e)
        {
            lock (locker)
            {
                groupMetaData.Add(e);
            }
        }

        private void Group_Update(object? sender, GroupMetadataModel e)
        {
            //TODO:
        }

        private void Chats_Delete(object? sender, ChatModel[] e)
        {
            //TODO
        }

        private void Chats_Update(object? sender, ChatModel[] e)
        {
            lock (locker)
            {
                changes = true;
                foreach (var update in e)
                {
                    var existing = GetChat(update.ID);
                    if (existing != null)
                    {
                        existing.Update(update);
                        chats.Update(existing);
                    }
                }
            }
        }

        private void Chats_Upsert(object? sender, ChatModel[] e)
        {
            lock (locker)
            {
                changes = true;
                chats.Upsert(e);
            }
        }

        private void Receipt_Upsert(object? sender, MessageReceipt[] e)
        {
            List<MessageReceipt> updates = new List<MessageReceipt>();

            foreach (var item in e)
            {
                var msg = GetMessage(item.MessageID);
                if (msg != null)
                {
                    msg.Receipts = msg.Receipts ?? new List<MessageReceipt>();

                    var info = msg.ToMessageInfo();

                    var receipt = info.UserReceipt.FirstOrDefault(x => x.UserJid == item.RemoteJid);
                    if (receipt == null)
                    {
                        receipt = new UserReceipt()
                        {
                            UserJid = item.RemoteJid
                        };
                        info.UserReceipt.Add(receipt);
                    }

                    switch (item.Status)
                    {
                        case WebMessageInfo.Types.Status.ServerAck:
                        case WebMessageInfo.Types.Status.DeliveryAck:
                            if (!receipt.HasReceiptTimestamp)
                            {
                                receipt.ReceiptTimestamp = item.Time;
                            }
                            break;
                        case WebMessageInfo.Types.Status.Read:
                            receipt.ReadTimestamp = item.Time;
                            break;
                        case WebMessageInfo.Types.Status.Played:
                            receipt.PlayedTimestamp = item.Time;
                            break;
                        default:
                            break;
                    }

                    var jid = JidEncode(JidDecode(item.RemoteJid)?.User ?? "", "s.whatsapp.net");

                    if (!msg.Receipts.Any(x => x.RemoteJid == jid && x.Status == item.Status))
                    {
                        var recpt = new MessageReceipt()
                        {
                            MessageID = item.MessageID,
                            RemoteJid = jid,
                            Status = item.Status,
                            Time = item.Time,
                        };
                        updates.Add(recpt);
                        msg.Receipts.Add(recpt);
                    }

                    msg.Message = info.ToByteArray();

                    messages.Update(msg);
                }
            }

            if (updates.Count > 0)
                EV.Emit(EmitType.Update, updates.ToArray());
        }

        private void Message_Delete(object? sender, WebMessageInfo[] e)
        {
            //TODO
        }

        private void Message_Update(object? sender, WebMessageInfo[] e)
        {
            lock (locker)
            {
                foreach (var item in e)
                {
                    var msg = GetMessage(item.Key.Id);
                    if (msg != null)
                    {
                        msg.Message = item.ToByteArray();
                        messages.Update(msg);
                    }
                }
                //TODO
            }
        }

        private void Message_Upsert(object? sender, MessageEventModel e)
        {
            lock (locker)
            {
                var item = e;
                switch (item.Type)
                {
                    case MessageEventType.Append:
                    case MessageEventType.Notify:
                        foreach (var msg in item.Messages)
                        {
                            var jid = JidUtils.JidNormalizedUser(msg.Key.RemoteJid);
                            if (!messageList.TryGetValue(jid, out var list))
                            {
                                messageList[jid] = list = [];
                            }
                            list.Add(msg);
                            messages.Add(new MessageModel(msg));

                            var chat = GetChat(jid);
                            if (chat != null)
                            {
                                chat.UnreadCount++;
                                chat.ConversationTimestamp = msg.MessageTimestamp;
                                EV.Emit(EmitType.Update, chat);
                            }
                            else
                            {
                                chat = new ChatModel()
                                {
                                    ID = jid,
                                    ConversationTimestamp = msg.MessageTimestamp,
                                    UnreadCount = 1
                                };
                                EV.Emit(EmitType.Upsert, chat);
                            }

                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void Contacts_Upsert(object? sender, ContactModel[] e)
        {
            ContactsUpsert(e.ToList());
        }

        private void Contacts_Update(object? sender, ContactModel[] e)
        {
            if (e == null || e.Length == 0)
                return;
            lock (locker)
            {
                changes = true;
                var ContactsWithName = e.Where(x => !(string.IsNullOrEmpty(x.Name) || string.IsNullOrWhiteSpace(x.Name))).ToList();
                List<ContactModel> toAdd = new List<ContactModel>();
                foreach (var item in ContactsWithName)
                {
                    if (!toAdd.Any(x => x.ID == item.ID))
                    {
                        toAdd.Add(item);
                    }

                }
                contacts.InsertBulk(toAdd);
            }

        }

        private void Connection_Update(object? sender, ConnectionState e)
        {
            State.Connection = e.Connection;
            State.QR = e.QR;
            State.LastDisconnect = e.LastDisconnect;
            State.IsOnline = e.IsOnline;
            State.ReceivedPendingNotifications = e.ReceivedPendingNotifications;
            State.IsNewLogin = e.IsNewLogin;
        }

        private void MessageHistory_Set(object? sender, MessageHistoryModel[] e)
        {
            foreach (var item in e)
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
                    Logger.Debug(new { chatsAdded = chatsAdded.Select(x => x.ID) }, "synced chats");

                    var oldContacts = ContactsUpsert(item.Contacts);
                    if (item.IsLatest)
                    {
                        foreach (var contact in oldContacts)
                        {
                            var deleted = contacts.Delete(contact);
                        }
                    }

                    Logger.Debug(new { deletedContacts = item.IsLatest ? oldContacts.Length : 0, Contacts = item.Contacts.Select(x => new { x.ID, x.Name }) }, "synced contacts");

                    var newMessages = new List<MessageModel>();
                    foreach (var msg in item.Messages)
                    {
                        var storeMessage = new MessageModel(msg);
                        var jid = msg.Key.RemoteJid;
                        if (!messageList.TryGetValue(jid, out var list))
                        {
                            messageList[jid] = list = [];
                        }
                        list.Insert(0, msg);
                        newMessages.Add(storeMessage);

                    }
                    messages.InsertIfAbsent(newMessages);

                    Logger.Debug(new { messages = item.Messages.Count }, "synced messages");
                }
            }
        }

        private void MessageReceipt_Multi(MessageReceipt[] args)
        {
            List<MessageReceipt> updates = new List<MessageReceipt>();

            foreach (var item in args)
            {
                var msg = GetMessage(item.MessageID);
                if (msg != null)
                {
                    msg.Receipts = msg.Receipts ?? new List<MessageReceipt>();

                    var info = msg.ToMessageInfo();

                    var receipt = info.UserReceipt.FirstOrDefault(x => x.UserJid == item.RemoteJid);
                    if (receipt == null)
                    {
                        receipt = new UserReceipt()
                        {
                            UserJid = item.RemoteJid
                        };
                        info.UserReceipt.Add(receipt);
                    }

                    switch (item.Status)
                    {
                        case WebMessageInfo.Types.Status.ServerAck:
                        case WebMessageInfo.Types.Status.DeliveryAck:
                            if (!receipt.HasReceiptTimestamp)
                            {
                                receipt.ReceiptTimestamp = item.Time;
                            }
                            break;
                        case WebMessageInfo.Types.Status.Read:
                            receipt.ReadTimestamp = item.Time;
                            break;
                        case WebMessageInfo.Types.Status.Played:
                            receipt.PlayedTimestamp = item.Time;
                            break;
                        default:
                            break;
                    }

                    var jid = JidEncode(JidDecode(item.RemoteJid)?.User ?? "", "s.whatsapp.net");

                    if (!msg.Receipts.Any(x => x.RemoteJid == jid && x.Status == item.Status))
                    {
                        var recpt = new MessageReceipt()
                        {
                            MessageID = item.MessageID,
                            RemoteJid = jid,
                            Status = item.Status,
                            Time = item.Time,
                        };
                        updates.Add(recpt);
                        msg.Receipts.Add(recpt);
                    }

                    msg.Message = info.ToByteArray();

                    messages.Update(msg);
                }
            }

            if (updates.Count > 0)
                EV.Emit(EmitType.Update, updates.ToArray());
        }

        private void GroupUpsertEvent_Emit(GroupMetadataModel[] args)
        {
            lock (locker)
            {
                groupMetaData.InsertBulk(args);
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

        private void MessageDelete_Emit(MessageUpdate[] args)
        {
            ///TODO
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
        //error here if Collection was modified; enumeration operation may not execute
        public Message? GetMessage(MessageKey key)
        {
            var raw = GetMessage(key.Id);
            if (raw == null)
            {
                return null;
            }
            var web = WebMessageInfo.Parser.ParseFrom(raw.Message);
            return web.Message;
        }
        public MessageModel? GetMessage(string key)
        {
            lock (locker)
            {
                var raw = messages.FindByID(key);
                return raw;
            }
        }

       

        public List<ContactModel> GetAllGroups()
        {
            return contacts.Where(x => x.IsGroup).ToList();
        }

        public void AddGroup(ContactModel contactModel)
        {

        }

        internal GroupMetadataModel? GetGroup(string jid)
        {
            return groupMetaData.FindByID(jid);
        }

        public ContactModel? GetContact(string jid)
        {
            return contacts.FirstOrDefault(x => x.ID == jid);
        }

        internal ChatModel? GetChat(string? jid)
        {
            return chats.FirstOrDefault(x => x.ID == jid);
        }

        public void StartTimer()
        {
            checkPoint?.Dispose();
            checkPoint = new System.Threading.Timer(OnCheckpoint, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        private static System.Threading.Timer checkPoint;
        public EventEmitter EV { get; }
        public DefaultLogger Logger { get; }
        public void DisposeDb()
        {
            changes = true;
            OnCheckpoint(null);
            database?.Dispose();
            checkPoint?.Dispose();
        }
        public List<ContactModel> GetAllContact()
        {
            return contacts.Where(x => x.IsUser).ToList();
        }

        public void Dispose()
        {
            DisposeDb();
            //database.Dispose();
        }
    }
}
