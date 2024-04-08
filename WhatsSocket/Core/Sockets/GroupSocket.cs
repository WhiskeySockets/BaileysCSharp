using System.Diagnostics.CodeAnalysis;
using WhatsSocket.Core.Models;
using static WhatsSocket.Core.Utils.ChatUtils;
using static WhatsSocket.Core.Models.ChatConstants;
using static WhatsSocket.Core.Utils.GenericUtils;
using WhatsSocket.Core.Utils;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.WABinary;
using System.Text;
using WhatsSocket.Core.Helper;

namespace WhatsSocket.Core.Sockets
{
    public enum ParticipantAction
    {
        Add = 1,
        Remove = 2,
        Promote = 3,
        Demote = 4
    }
    public enum GroupSetting
    {
        Announcement = 1,
        Not_Announcement = 2,
        Locked = 3,
        Unlocked = 4
    }
    public enum MemberAddMode
    {
        Admin_Add = 1,
        All_Member_Add = 2,
    }
    public enum MembershipApprovalMode
    {
        On = 1,
        Off = 2,
    }

    public abstract class GroupSocket : ChatSocket
    {
        public GroupSocket([NotNull] SocketConfig config) : base(config)
        {

        }


        protected override async Task<bool> HandleDirtyUpdate(BinaryNode node)
        {
            var dirtyNode = GetBinaryNodeChild(node, "dirty");
            if (dirtyNode?.getattr("type") == "groups")
            {
                await GroupFetchAllParticipating();
                await CleanDirtyBits("groups");
            }

            return true;
        }

        private async Task GroupFetchAllParticipating()
        {
            ///TODO:
        }


        public async Task<BinaryNode> GroupQuery(string jid, string type, BinaryNode[] content)
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs = {
                    { "type",type},
                    {"xmlns","w:g2" },
                    {"to",jid},
                },
                content = content
            };

            return await Query(node);
        }

        public async Task<GroupMetadataModel> GroupMetaData(string jid)
        {
            var result = await GroupQuery(jid, "get", [new BinaryNode()
            {
                tag = "query",
                attrs = {
                    {"request","interactive" }
                }
            }]);

            return ExtractGroupMetaData(result);
        }

        //groupCreate
        public async Task<GroupMetadataModel> GroupCreate(string subject, string[] participants)
        {
            var key = GenerateMessageID();
            var result = await GroupQuery("@g.us", "set", [new BinaryNode()
            {
                tag = "create",
                attrs = {
                    {"subject",subject },
                    {"key",key }
                },
                content = participants.Select(x =>
                    new BinaryNode()
                    {
                        tag = "participant",
                        attrs = { {"jid",x } }
                    }
                ).ToArray()
            }]);

            var metaData = ExtractGroupMetaData(result);
            Store.AddGroup(new ContactModel()
            {
                ID = metaData.ID,
                Name = subject
            });

            return metaData;
        }

        //groupLeave
        public async Task GroupLeave(string id)
        {
            var result = await GroupQuery("@g.us", "set", [new BinaryNode()
            {
                tag = "leave",
                attrs = {
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag ="group",
                        attrs = {
                            {"id", id }
                        }
                    }
                }
            }]);
        }
        //groupUpdateSubject
        public async Task GroupUpdateSubject(string jid, string subject)
        {
            var result = await GroupQuery(jid, "set", [new BinaryNode()
            {
                tag = "subject",
                content = Encoding.UTF8.GetBytes(subject)
            }]);
        }

        //groupRequestParticipantsList
        public async Task<GroupMetadataModel> GroupRequestParticipantsList(string jid)
        {
            var result = await GroupQuery(jid, "set", [new BinaryNode()
            {
                tag = "membership_approval_requests",
                attrs = { }
            }]);

            var node = GetBinaryNodeChild(result, "membership_approval_requests");
            var participant = GetBinaryNodeChild(node, "membership_approval_request");

            //This needs to be tested

            return ExtractGroupMetaData(result);
        }
        //groupRequestParticipantsUpdate
        public async Task GroupRequestParticipantsUpdate(string jid, string[] participants, string action)
        {
            var result = await GroupQuery(jid, "set", [new BinaryNode()
            {
                tag = "membership_requests_action",
                content = new BinaryNode[]{
                    new BinaryNode(){
                        tag = action,
                        content = participants.Select(x =>
                            new BinaryNode()
                            {
                                tag = "participant",
                                attrs = {
                                    {"jid",x }
                                }
                            }
                        ).ToArray()
                    }
                }
            }]);

            var node = GetBinaryNodeChild(result, "membership_requests_action");
            var nodeAction = GetBinaryNodeChild(node, action);
            var participantsAffected = GetBinaryNodeChildren(nodeAction, "participant");

            //return participantsAffected.map(p => {
            //    return { status: p.attrs.error || '200', jid: p.attrs.jid }
            //})
        }
        //groupParticipantsUpdate


        public async Task GroupParticipantsUpdate(string jid, string[] participants, ParticipantAction action)
        {
            var result = await GroupQuery(jid, "set", [new BinaryNode()
            {
                tag = action.ToString().ToLower(),
                attrs = {},
                content =  participants.Select(x =>
                            new BinaryNode()
                            {
                                tag = "participant",
                                attrs = {
                                    {"jid",x }
                                }
                            }
                        ).ToArray()
            }]);

            var node = GetBinaryNodeChild(result, action.ToString().ToLower());
            var participantsAffected = GetBinaryNodeChildren(node, "participant");

        }
        //groupUpdateDescription
        public async Task GroupUpdateDescription(string jid, string description)
        {
            var metadata = await GroupMetaData(jid);
            var prev = metadata?.Desc ?? "";

            Dictionary<string, string> attrs = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(description))
            {
                attrs["delete"] = "true";
            }
            else
            {
                attrs["id"] = GenerateMessageID();
            }
            if (!string.IsNullOrWhiteSpace(prev))
            {
                attrs["prev"] = prev;
            }


            var node = new BinaryNode()
            {
                tag = "description",
                attrs = attrs,
            };


            if (!string.IsNullOrWhiteSpace(description))
            {
                node.content = new BinaryNode[] {
                    new BinaryNode()
                    {
                        tag = "body",
                        content = Encoding.UTF8.GetBytes(description)
                    }
                };
            }

            var result = await GroupQuery(jid, "set", [node]);
        }

        //groupInviteCode
        public async Task<string> GroupInviteCode(string jid)
        {
            var result = await GroupQuery(jid, "get", [new BinaryNode() { tag = "invite", }]);
            var inviteNode = GetBinaryNodeChild(result, "invite");
            return inviteNode?.getattr("code") ?? "";
        }
        //groupRevokeInvite
        public async Task<string> GroupRevokeInvite(string jid)
        {
            var result = await GroupQuery(jid, "set", [new BinaryNode() { tag = "invite", }]);
            var inviteNode = GetBinaryNodeChild(result, "invite");
            return inviteNode?.getattr("code") ?? "";
        }
        //groupAcceptInvite
        public async Task<string> GroupAcceptInvite(string code)
        {
            var result = await GroupQuery("@g.us", "set", [new BinaryNode() { tag = "invite", attrs = { { "code", code } } }]);
            var inviteNode = GetBinaryNodeChild(result, "invite");
            return inviteNode?.getattr("code") ?? "";
        }
        //groupAcceptInviteV4
        //groupGetInviteInfo
        public async Task<GroupMetadataModel> GroupGetInviteInfo(string code)
        {
            var result = await GroupQuery("@g.us", "get", [new BinaryNode() { tag = "invite", attrs = { { "code", code } } }]);
            return ExtractGroupMetaData(result);
        }
        //groupToggleEphemeral
        public async Task GroupToggleEphemeral(string jid, ulong ephemeralExpiration = 0)
        {
            BinaryNode node;
            if (ephemeralExpiration > 0)
            {
                node = new BinaryNode()
                {
                    tag = "ephemeral",
                    attrs = { { "expiration", ephemeralExpiration.ToString() } }
                };
            }
            else
            {
                node = new BinaryNode()
                {
                    tag = "not_ephemeral",
                };
            }
            var result = await GroupQuery(jid, "set", [node]);
        }
        //groupSettingUpdate
        public async Task GroupSettingUpdate(string jid, GroupSetting setting)
        {
            await GroupQuery(jid, "set", [new BinaryNode() { tag = setting.ToString().ToLower() }]);
        }
        //groupMemberAddMode
        public async Task GroupMemberAddMode(string jid, MemberAddMode mode)
        {
            await GroupQuery(jid, "set", [new BinaryNode()
            {
                tag = "member_add_mode",
                content = Encoding.UTF8.GetBytes(mode.ToString())
            }]);
        }

        //groupJoinApprovalMode
        public async Task GroupJoinApprovalMode(string jid, MembershipApprovalMode mode)
        {
            await GroupQuery(jid, "set", [new BinaryNode()
            {
                tag = "membership_approval_mode",
                content = new BinaryNode[]{
                    new BinaryNode()
                    {
                        tag = "group_join",
                        attrs = {{"state",mode.ToString().ToLower()}}
                    }
                }
            }]);
        }



        public GroupMetadataModel ExtractGroupMetaData(BinaryNode result)
        {
            var group = GetBinaryNodeChild(result, "group");
            var descChild = GetBinaryNodeChild(result, "description");
            string desc = "";
            string descId = "";
            if (descChild != null)
            {
                desc = GetBinaryNodeChildString(descChild, "body");
                descId = descChild.attrs["id"];
            }


            var groupId = group.attrs["id"].Contains("@") ? group.attrs["id"] : JidUtils.JidEncode(group.attrs["id"], "g.us");
            var eph = GetBinaryNodeChild(group, "ephemeral")?.attrs["expiration"].ToUInt64();

            var participants = GetBinaryNodeChildren(group, "participant");
            var memberAddMode = GetBinaryNodeChildString(group, "member_add_mode") == "all_member_add";

            var metadata = new GroupMetadataModel
            {
                ID = groupId,
                Subject = group.getattr("subject"),
                SubjectOwner = group.getattr("s_o"),
                SubjectTime = group.getattr("s_t").ToUInt64(),
                Size = (ulong)participants.Length,
                Creation = group.attrs["creation"].ToUInt64(),
                Owner = group.getattr("creator") != null ? JidUtils.JidNormalizedUser(group.attrs["creator"]) : null,
                Desc = desc,
                DescID = descId,
                Restrict = GetBinaryNodeChild(group, "locked") != null,
                Announce = GetBinaryNodeChild(group, "announcement") != null,
                IsCommunity = GetBinaryNodeChild(group, "parent") != null,
                IsCommunityAnnounce = GetBinaryNodeChild(group, "default_sub_group") != null,
                JoinApprovalMode = GetBinaryNodeChild(group, "membership_approval_mode") != null,
                MemberAddMode = memberAddMode,
                Participants = participants.Select(x => new GroupParticipantModel()
                {
                    ID = x.attrs["jid"],
                    ParticipantType = x.getattr("type")

                }).ToArray(),
                EphemeralDuration = eph
            };


            return metadata;
        }
    }
}
