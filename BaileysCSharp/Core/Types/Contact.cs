using LiteDB;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Utils;

namespace BaileysCSharp.Core.Models
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
        public bool Saved { get; set; }
        public bool? SaveContact
        {
            get
            {
                return !(string.IsNullOrEmpty(Name) || string.IsNullOrWhiteSpace(Name));
            }
        }

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
        public bool IsUser
        {
            get
            {
                return JidUtils.IsJidUser(ID);
            }
        }

        public string PhoneNumber
        {
            get
            {
                return JidUtils.IsJidUser(ID) ? JidUtils.JidDecode(ID)?.User : string.Empty;
            }
        }
    }
}
