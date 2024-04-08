using LiteDB;
using Newtonsoft.Json;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.WABinary;

namespace WhatsSocket.Core.Models
{
    public class ContactModel : IMayHaveID
    {
        [BsonId]
        public string ID { get; set; }
        public string LID { get; set; }
        public string Name { get; set; }

        public string Notify { get; set; }

        public string VerifiedName { get; set; }
        public string? ImgUrl { get; set; }
        public string Status { get; set; }

        public string GetID()
        {
            return ID;
        }

        public override string ToString()
        {
            return $"{ID} - {Name}";
        }


        public bool IsGroup
        {
            get
            {
                return JidUtils.IsJidGroup(ID);
            }
        }
    }
}
