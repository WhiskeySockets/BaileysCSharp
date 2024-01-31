namespace WhatsSocket.Core.Models.SenderKeys
{
    public class SenderMessageKeyStructure
    {
        public SenderMessageKeyStructure(uint iteration, byte[] seed)
        {
            Iteration = iteration;
            Seed = seed;
        }
        public uint Iteration { get; set; }
        public byte[] Seed { get; set; }
    }


}
