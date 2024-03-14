using LiteDB;
using Proto;
using WhatsSocket.Core.NoSQL;

namespace WhatsSocket.Core.Models
{
    public class GroupParticipantModel
    {
        public string ID { get; set; }
        public string? ParticipantType { get; set; }
    }
    public class GroupMetadataModel : IMayHaveID
    {
        [BsonId]
        public string ID { get; set; }


        public string? Owner { get; set; }
        public string? Subject {  get; set; }
        /** group subject owner */
        public string? SubjectOwner { get; set; }
        /** group subject modification date */
        public ulong? SubjectTime { get; set; }
        public ulong Creation { get; set; }
        public string? Desc { get; set; }
        public string? DescID { get; set; }
        public string? DescOwner { get; set; }
        /** is set when the group only allows admins to change group settings */
        public bool Restrict { get; set; }
        /** is set when the group only allows admins to write messages */
        public bool Announce { get; set; }
        /** is set when the group also allows members to add participants */
        public bool MemberAddMode { get; set; }
        /** Request approval to join the group */
        public bool JoinApprovalMode { get; set; }
        /** is this a community */
        public bool IsCommunity { get; set; }
        /** is this the announce of a community */
        public bool IsCommunityAnnounce { get; set; }
        /** number of group participants */
        public ulong Size { get; set; }
        public GroupParticipantModel[] Participants { get; set; }
        public ulong? EphemeralDuration { get; set; }
        public string? InviteCode { get; set; }
        /** the person who added you */
        public string? Author { get; set; }

        public string GetID()
        {
            return ID;
        }
    }

}
