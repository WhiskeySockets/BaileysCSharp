using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    public class AuthenticationState
    {
        public AuthenticationCreds Creds { get; set; }
        public BaseKeyStore Keys { get; set; }
    }
}
