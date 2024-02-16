namespace WhatsSocket.Core.Models
{
    public class QRData
    {

        public QRData(string qr)
        {
            Data = qr;
        }

        public string Data { get; set; }
    }
}
