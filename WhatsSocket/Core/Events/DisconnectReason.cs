namespace WhatsSocket.Core.Events
{
    public enum DisconnectReason
    {
        None = 0,
        ConnectionClosed = 428,
        ConnectionLost = 408,
        ConnectionReplaced = 440,
        TimedOut = 408,
        LoggedOut = 401,
        BadSession = 500,
        RestartRequired = 515,
        MultideviceMismatch = 411,
        MissMatch = 901,
        NoKeyForMutation = 404,
    }
}
