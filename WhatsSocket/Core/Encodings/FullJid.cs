﻿namespace WhatsSocket.Core.Encodings
{
    public class FullJid : JidWidhDevice
    {
        public string Server { get; set; }
        public int? DomainType { get; set; }
    }
}