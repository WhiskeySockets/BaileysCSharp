using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WhatsSocket.Core.Utils
{
    public static class GenericUtils
    {

        public static string GenerateMessageID()
        {
            var bytes = AuthenticationUtils.RandomBytes(6);
            return "BAE5" + BitConverter.ToString(bytes).Replace("-","").ToUpper();
        }
    }
}
