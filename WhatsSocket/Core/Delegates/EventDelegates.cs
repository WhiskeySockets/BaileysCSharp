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
    public delegate void KeyStoreChangeArgs(KeyStore store);
    public delegate void SessionStoreChangeArgs(SessionStore store);
    public delegate void CredentialsChangeArgs(BaseSocket sender, AuthenticationCreds authenticationCreds);
    public delegate void DisconnectedArgs(BaseSocket sender, DisconnectReason disconnectReason);
    public delegate void QRCodeArgs(BaseSocket sender, string qr_data);
    public delegate void SenderKeyStoreChangeArgs(SenderKeyStore store);
}
