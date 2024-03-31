namespace WhatsSocket.Core.Events
{
    public enum EmitType
    {
        Set = 1,
        Upsert = 2,
        Update = 4,
        Delete = 8,
        Reaction = 16,
    }
}
