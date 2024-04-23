using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.NoSQL;

namespace BaileysCSharp.Core.Types
{
    public class AuthenticationState
    {
        public AuthenticationCreds Creds { get; set; }
        public BaseKeyStore Keys { get; set; }
    }
}
