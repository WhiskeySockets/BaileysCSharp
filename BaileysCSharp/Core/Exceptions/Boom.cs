using BaileysCSharp.Core.Events;

namespace BaileysCSharp.Exceptions
{
    public class BoomData
    {
        public BoomData(DisconnectReason statusCode)
        {
            StatusCode = (int)statusCode;
        }
        public BoomData(int statusCode)
        {
            StatusCode = statusCode;
        }
        public BoomData(Dictionary<string, string> data)
        {
            Data = data;
        }
        public BoomData(int statusCode, Dictionary<string, string> data)
        {
            StatusCode = statusCode;
            Data = data;
        }

        public int StatusCode { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    [Serializable]
    public class Boom : Exception
    {
        public Boom() { }
        public Boom(string message, DisconnectReason reason) : base(message)
        {
            Reason = reason;
        }
        public Boom(string message, BoomData data) : base(message)
        {
            Data = data;
        }
        public Boom(string message) : base(message)
        {
            Reason = DisconnectReason.None;
        }

        public DisconnectReason Reason { get; }
        public BoomData Data { get; }
    }

}
