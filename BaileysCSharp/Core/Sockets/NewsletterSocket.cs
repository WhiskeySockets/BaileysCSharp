using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Newsletters;
using BaileysCSharp.Core.WABinary;
using static BaileysCSharp.Core.Utils.ProcessMessageUtil;
using static BaileysCSharp.Core.Utils.GenericUtils;
using static BaileysCSharp.Core.WABinary.Constants;
using System.Text.Json.Serialization;
using System;
using BaileysCSharp.Core.Utils;
using static BaileysCSharp.Core.Models.Newsletters.NewsletterSettings;
using static BaileysCSharp.Core.Models.Newsletters.NewsletterMetaData;
using System.Reflection.Metadata;
using BaileysCSharp.Core.Models.Sending.Interfaces;
using BaileysCSharp.Core.Models.Sending;
using Proto;
using BaileysCSharp.Core.Models.Sending.NonMedia;
using static Proto.Message.Types;
using BaileysCSharp.Core.Converters;
using BaileysCSharp.Core.Types;

namespace BaileysCSharp.Core.Sockets
{

    public class NewsletterSocket : BusinessSocket
    {
        public const string queryMexFetchNewsletterEnforcementsJobQuery = "8232776406750176";
        public const string WAWebMexAdminCountNewsletterJobMutation = "7130823597031706";
        public const string queryRecommendedNewsletters = "7263823273662354";
        public const string queryNewslettersDirectory = "";
        public const string querySubscribedNewsletters = "";
        public const string queryNewsletterSubscribers = "";


        public const string WAWebMexMetaDataNewsletterJobMutation = "6620195908089573";

        public const string WAWebMexMuteNewsletterJobMutation = "25151904754424642";
        public const string WAWebMexUnmuteNewsletterJobMutation = "7337137176362961";
        public const string WAWebMexUpdateNewsletterJobMutation = "7150902998257522";
        public const string WAWebMexCreateNewsletterJobMutation = "6996806640408138";
        public const string WAWebMexDeleteNewsletterJobMutation = "8316537688363079";
        public const string WAWebMexUnfollowNewsletter = "7238632346214362";
        public const string WAWebMexFollowNewsletter = "7871414976211147";





        public NewsletterSocket([NotNull] SocketConfig config) : base(config)
        {
        }

        public async Task AcceptTOSNotice()
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    { "id", GenerateMessageTag() },
                    {"to", S_WHATSAPP_NET },
                    {"type", "set" },
                    {"xmlns","tos" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "notice",
                        attrs =
                        {
                            {"id","20601218" },
                            {"stage","5" }
                        }
                    }
                }
            };
            var result = await Query(node);
            var notice = GetBinaryNodeChild(result, "notice");

        }

        private async Task<BinaryNode> NewsletterWMexQuery(string queryId, INewsletterVariable? input = null)
        {
            var query = new WMexQuery { Variables = input };
            var result = await NewsletterQuery(queryId, query);
            return result;
        }


        private async Task<BinaryNode> NewsletterQuery(string queryId, object? input = null)
        {
            var newsletterRequest = JsonSerializer.Serialize(input, new JsonSerializerOptions()
            {
                Converters =
                {
                    new InterfaceConverter<INewsletterInputParams>(),
                    new InterfaceConverter<INewsletterVariable>(),
                },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            var buffer = Encoding.UTF8.GetBytes(newsletterRequest);
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    { "id", GenerateMessageTag() },
                    { "to", S_WHATSAPP_NET },
                    { "type", "get" },
                    { "xmlns", "w:mex" }
                },
                content = new BinaryNode[]
                {
                    new BinaryNode()
                    {
                        tag = "query",
                        attrs =
                        {
                            {"query_id", queryId }
                        },
                        content = buffer
                    }
                }
            };
            var result = await Query(node);
            return result;
        }



        private async Task<BinaryNode> NewsletterQuery(string jid, string type, BinaryNode[] content)
        {
            var node = new BinaryNode()
            {
                tag = "iq",
                attrs =
                {
                    { "id", GenerateMessageTag() },
                    { "to", jid },
                    { "type", type },
                    { "xmlns", "newsletter" }
                },
                content = content
            };
            var result = await Query(node);
            return result;
        }




        public async Task<Xwa2NewslettersRecommended> QueryRecommendedNewsletters()
        {
            var @params = new NewsletterVariable()
            {
                Input = new NewsletterRecommendedInput()
                {
                    CountryCodes = new List<string> { "ZA" },
                    Limit = 20
                }
            };
            var result = await NewsletterWMexQuery(queryRecommendedNewsletters, @params);
            var content = GetBinaryNodeChild(result, "result");
            var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());

            var newsletterResult = JsonSerializer.Deserialize<QueryNewsletterRecommendedResult>(jsonResult);
            return newsletterResult?.Data?.Xwa2NewslettersRecommended;
        }


        public async Task<long> SubscribeNewsletterUpdates(string jid)
        {
            var result = await NewsletterQuery(jid, "set", [new BinaryNode() {
                tag = "live_updates"
            }]);
            var live_updates = GetBinaryNodeChild(result, "live_updates")?.getattr("duration");
            if (live_updates != null)
            {
                return Convert.ToInt64(live_updates);
            }

            return -1;
        }

        public async Task<long> NewsletterReactionMode(string jid, NewsletterReactionMode mode)
        {
            var @params = new NewsletterVariable()
            {
                NewsletterID = jid,
                Updates = new NewsletterUpdateParamType()
                {
                    Setttings = new NewsletterSettings()
                    {
                        ReactionMode = mode
                    }
                }
            };
            var result = await NewsletterWMexQuery(WAWebMexUpdateNewsletterJobMutation, @params);
            return -1;
        }

        public async Task<NewsletterMetaData> NewsletterCreate(string channelName)
        {
            var result = await NewsletterWMexQuery(WAWebMexCreateNewsletterJobMutation, new NewsletterVariable()
            {
                Input = new CreateNewsletterInput()
                {
                    Name = channelName,
                }
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                var createResult = JsonSerializer.Deserialize<NewsletterActionResult>(jsonResult);
                if (createResult != null)
                {
                    EV.Emit(Events.EmitType.Upsert, createResult.Data.Xwa2NewsletterCreate);
                    return createResult.Data.Xwa2NewsletterCreate;
                }
            }
            return default;
        }



        public async Task<bool> NewsletterFollow(string jid)
        {
            var result = await NewsletterWMexQuery(WAWebMexFollowNewsletter, new NewsletterVariable()
            {
                NewsletterID = jid,
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                return true;
            }
            return false;
        }

        public async Task<bool> NewsletterUnFollow(string jid)
        {
            var result = await NewsletterWMexQuery(WAWebMexUnfollowNewsletter, new NewsletterVariable()
            {
                NewsletterID = jid,
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                return true;
            }
            return false;
        }

        public async Task<bool> NewsletterMute(string jid)
        {
            var result = await NewsletterWMexQuery(WAWebMexMuteNewsletterJobMutation, new NewsletterVariable()
            {
                NewsletterID = jid,
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                return true;
            }
            return false;
        }

        public async Task<bool> NewsletterUnMute(string jid)
        {
            var result = await NewsletterWMexQuery(WAWebMexUnmuteNewsletterJobMutation, new NewsletterVariable()
            {
                NewsletterID = jid,
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                return true;
            }
            return false;
        }
        //newsletterUpdatePicture,
        //newsletterRemovePicture,

        public async Task<NewsletterMetaData> NewsletterUpdateName(string jid, string name)
        {
            var @params = new NewsletterVariable()
            {
                NewsletterID = jid,
                Updates = new NewsletterUpdateParamType()
                {
                    Name = name
                }
            };
            var result = await NewsletterWMexQuery(WAWebMexUpdateNewsletterJobMutation, @params);
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                var createResult = JsonSerializer.Deserialize<NewsletterActionResult>(jsonResult);
                if (createResult != null)
                {
                    EV.Emit(Events.EmitType.Update, createResult.Data.Xwa2NewsletterCreate);
                    return createResult.Data.Xwa2NewsletterCreate;
                }
            }
            return default;
        }


        public async Task<NewsletterMetaData> NewsletterUpdateDescription(string jid, string description)
        {
            var @params = new NewsletterVariable()
            {
                NewsletterID = jid,
                Updates = new NewsletterUpdateParamType()
                {
                    Description = description
                }
            };
            var result = await NewsletterWMexQuery(WAWebMexUpdateNewsletterJobMutation, @params);
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                var createResult = JsonSerializer.Deserialize<NewsletterActionResult>(jsonResult);
                if (createResult != null)
                {
                    EV.Emit(Events.EmitType.Update, createResult.Data.Xwa2NewsletterCreate);
                    return createResult.Data.Xwa2NewsletterCreate;
                }
            }
            return default;
        }

        public async Task<NewsletterMetaData> NewsletterMetadata(string key, NewsletterMetaDataType type)
        {
            var @params = new NewsletterMetadataVariable()
            {
                Input = new NewsletterQueryInput()
                {
                    Key = key,
                    Type = type.ToString(),
                    ViewRole = "GUEST"
                },
                FetchCreationTime = false,
                FetchFullImage = false,
                FetchViewerMetadata = false,
            };
            var result = await NewsletterWMexQuery(WAWebMexMetaDataNewsletterJobMutation, @params);
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                var queryResult = JsonSerializer.Deserialize<QueryNewsletterResult>(jsonResult);
                return queryResult?.Data?.Xwa2Newsletter;
            }
            return default;
        }

        public async Task<long> NewsletterAdminCount(string jid)
        {
            var result = await NewsletterWMexQuery(WAWebMexAdminCountNewsletterJobMutation, new NewsletterVariable()
            {
                NewsletterID = jid,
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                var adminCount = JsonSerializer.Deserialize<NewsletterAdminCountResult>(jsonResult);
                return adminCount?.Data.Xwa2NewsletterAdmin.AdminCount ?? -1;
            }

            return -1;
        }

        //newsletterChangeOwner,
        //newsletterDemote,

        public async Task<bool> NewsletterDelete(string jid)
        {
            var result = await NewsletterWMexQuery(WAWebMexDeleteNewsletterJobMutation, new NewsletterVariable()
            {
                NewsletterID = jid,
            });
            var content = GetBinaryNodeChild(result, "result");
            if (content != null)
            {
                var jsonResult = Encoding.UTF8.GetString(content.ToByteArray());
                EV.Emit(Events.EmitType.Delete, new NewsletterMetaData()
                {
                    Id = jid,
                });
                return true;
            }
            return false;
        }

        //newsletterReactMessage,
        //newsletterfetchMessages,
        //newsletterfetchUpdates


        public async Task<WebMessageInfo?> SendNewsletterMessage(string jid, INewsletterMessageContent content)
        {
            var decoded = JidUtils.JidDecode(jid);
            if (decoded?.Server == "newsletter")
            {
                var fullMsg = new WebMessageInfo()
                {
                    Message = new Message() { },
                    Key = new MessageKey()
                    {
                        FromMe = true,
                        RemoteJid = jid,
                        Participant = Creds.Me.ID,
                        Id = GenerateMessageID()
                    }
                };

                if (content is NewsletterTextMessage text)
                {
                    fullMsg.Message.ExtendedTextMessage = new ExtendedTextMessage()
                    {
                        Text = text.Text,
                    };
                }

                await RelayMessage(jid, fullMsg.Message, new MessageRelayOptions()
                {
                    MessageID = fullMsg.Key.Id,
                });

                await UpsertMessage(fullMsg, MessageEventType.Append);
            }
            throw new Exception($"{decoded?.Server} not supported");
        }
    }
}
