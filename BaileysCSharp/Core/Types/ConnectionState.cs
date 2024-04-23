using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Models;

namespace BaileysCSharp.Core.Types
{
    public class LastDisconnect
    {
        public Exception Error { get; set; }
        public DateTime Date { get; set; }
    }

    public class ConnectionState
    {
        public WAConnectionState Connection { get; set; }
        public bool? IsNewLogin { get; set; }
        public string? QR { get; set; }
        public bool? IsOnline { get; set; }
        public bool ReceivedPendingNotifications { get; set; }

        public LastDisconnect LastDisconnect { get; set; }
    }
}
