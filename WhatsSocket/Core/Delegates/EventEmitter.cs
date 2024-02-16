using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.Stores;

namespace WhatsSocket.Core.Delegates
{
    public delegate void EventEmitterHandler<T>(BaseSocket sender, T args);
    public class EventEmitter
    {
        public event EventEmitterHandler<QRData> OnQR;
        public event EventEmitterHandler<SessionStore> OnSessionStoreChange;
        public event EventEmitterHandler<KeyStore> OnKeyStoreChange;
        public event EventEmitterHandler<AuthenticationCreds> OnCredsChange;
        public event EventEmitterHandler<DisconnectReason> OnDisconnect;
        public event EventEmitterHandler<Contact> OnContactChange;
        public event EventEmitterHandler<AppStateSyncKeyStore> OnAppStateSyncKeyStoreChange;
        public event EventEmitterHandler<AppStateSyncVersionStore> OnAppStateSyncVersionStoreChange;
        public event EventEmitterHandler<SenderKeyStore> OnSenderKeyStoreChange;

        public EventEmitter(BaseSocket sender)
        {
            Sender = sender;
        }

        public BaseSocket Sender { get; }


        internal void EmitQR(QRData qr)
        {
            OnQR?.Invoke(Sender, qr);
        }

        internal void Emit(SessionStore store)
        {
            OnSessionStoreChange?.Invoke(Sender, store);
        }

        internal void Emit(KeyStore store)
        {
            OnKeyStoreChange?.Invoke(Sender, store);
        }

        internal void Emit(AuthenticationCreds creds)
        {
            OnCredsChange.Invoke(Sender, creds);
        }

        internal void Emit(DisconnectReason reason)
        {
            OnDisconnect?.Invoke(Sender, reason);
        }

        internal void Emit(Contact contact)
        {
            OnContactChange?.Invoke(Sender, contact);
        }

        internal void Emit(AppStateSyncKeyStore appStateSyncKeyStore)
        {
            OnAppStateSyncKeyStoreChange?.Invoke(Sender, appStateSyncKeyStore);
        }

        internal void Emit(AppStateSyncVersionStore appStateSyncVersionStore)
        {
            OnAppStateSyncVersionStoreChange?.Invoke(Sender, appStateSyncVersionStore);
        }

        internal void Emit(SenderKeyStore senderKeyStore)
        {
            OnSenderKeyStoreChange?.Invoke(Sender, senderKeyStore);
        }
    }
}
