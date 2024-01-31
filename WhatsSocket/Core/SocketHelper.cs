using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Proto.ClientPayload.Types;
using WhatsSocket.Core.Helper;
using Google.Protobuf;
using WhatsSocket.Core.Encodings;
using WhatsSocket.Core.Events;
using WhatsSocket.Core.Models;
using WhatsSocket.Exceptions;
using WhatsSocket.Core.Stores;

namespace WhatsSocket.Core
{

    public class SocketHelper
    {

        public static ClientPayload GenerateRegistrationNode(AuthenticationCreds creds)
        {
            var appVersion = EncryptionHelper.Md5("2.2329.9");
            var companion = new DeviceProps()
            {
                Os = "Baileys",
                PlatformType = DeviceProps.Types.PlatformType.Chrome,
                RequireFullSync = false,
            };
            var payload = new ClientPayload
            {
                Passive = false,
                ConnectReason = ConnectReason.UserActivated,
                ConnectType = ConnectType.WifiUnknown,
                UserAgent = new UserAgent()
                {
                    AppVersion = new UserAgent.Types.AppVersion()
                    {
                        Primary = 2,
                        Secondary = 2329,
                        Tertiary = 9,
                    },
                    Platform = UserAgent.Types.Platform.Macos,
                    ReleaseChannel = UserAgent.Types.ReleaseChannel.Release,
                    Mcc = "000",
                    Mnc = "000",
                    OsVersion = "0.1",
                    Manufacturer = "",
                    Device = "Dekstop",
                    OsBuildNumber = "0.1",
                    LocaleLanguageIso6391 = "en",
                    LocaleCountryIso31661Alpha2 = "us",
                    //PhoneId = creds.PhoneId
                },
                DevicePairingData = new DevicePairingRegistrationData()
                {
                    BuildHash = appVersion.ToByteString(),
                    DeviceProps = companion.ToByteString(),
                    ERegid = creds.RegistrationId.EncodeBigEndian().ToByteString(),
                    EKeytype = Constants.KEY_BUNDLE_TYPE.ToByteString(),
                    EIdent = creds.SignedIdentityKey.Public.ToByteString(),
                    ESkeyId = creds.SignedPreKey.KeyId.EncodeBigEndian(3).ToByteString(),
                    ESkeyVal = creds.SignedPreKey.KeyPair.Public.ToByteString(),
                    ESkeySig = creds.SignedPreKey.Signature.ToByteString(),
                }
            };

            return payload;
        }



        public static BinaryNode ConfigureSuccessfulPairing(AuthenticationCreds creds, BinaryNode node)
        {

            var signedIdentityKey = creds.SignedIdentityKey;

            var msgId = node.attrs["id"].ToString();
            var pairSuccessNode = GetBinaryNodeChild(node, "pair-success");


            var deviceIdentityNode = GetBinaryNodeChild(pairSuccessNode, "device-identity");
            var platformNode = GetBinaryNodeChild(pairSuccessNode, "platform");
            var deviceNode = GetBinaryNodeChild(pairSuccessNode, "device");
            var businessNode = GetBinaryNodeChild(pairSuccessNode, "biz");

            var bizName = businessNode?.attrs["name"];
            var jid = deviceNode.attrs["jid"];

            var detailsHmac = ADVSignedDeviceIdentityHMAC.Parser.ParseFrom(deviceIdentityNode.ToByteArray());


            var advSign = EncryptionHelper.HmacSign(detailsHmac.Details.ToByteArray(), Convert.FromBase64String(creds.AdvSecretKey));

            // check HMAC matches
            var hmac = detailsHmac.Hmac.ToBase64();
            if (hmac != advSign)
            {
                End("Invalid Account Signature", DisconnectReason.BadSession);
            }

            var account = ADVSignedDeviceIdentity.Parser.ParseFrom(detailsHmac.Details);

            var accountMsg = new byte[] { 6, 0 }
            .Concat(account.Details.ToByteArray())
            .Concat(signedIdentityKey.Public).ToArray();
            if (!EncryptionHelper.Verify(account.AccountSignatureKey.ToByteArray(), accountMsg, account.AccountSignature.ToByteArray()))
            {
                End("Failed to verify account Signature", DisconnectReason.BadSession);
            }

            // sign the details with our identity key
            var deviceMsg = new byte[] { 6, 1 }
            .Concat(account.Details.ToByteArray())
            .Concat(signedIdentityKey.Public)
            .Concat(account.AccountSignatureKey).ToArray();
            account.DeviceSignature = EncryptionHelper.Sign(signedIdentityKey.Private, deviceMsg).ToByteString();

            //TODO: Finish 
            var identity = CreateSignalIdentity(jid, account.AccountSignatureKey);
            var accountEnc = EncodeSignedDeviceIdentity(account, false);


            var deviceIdentity = ADVDeviceIdentity.Parser.ParseFrom(account.Details);

            var reply = new BinaryNode("iq")
            {
                attrs = new Dictionary<string, string>()
                    {
                        {"to",Constants.S_WHATSAPP_NET },
                        {"type","result" },
                        {"id", msgId }
                    },
                content = new BinaryNode[]
                {
                    new BinaryNode("pair-device-sign")
                    {
                        content = new BinaryNode[]
                        {
                            new BinaryNode()
                            {
                                tag = "device-identity",
                                attrs = new Dictionary<string, string>()
                                {
                                    {"key-index",deviceIdentity.KeyIndex.ToString() }

                                },
                                content = accountEnc
                            }
                        },
                    }
                }
            };

            creds.SignalIdentities = new SignalIdentity[] { identity };
            creds.Platform = platformNode.attrs["name"];
            creds.Me = new Contact()
            {
                ID = jid,
                Name = bizName
            };


            return reply;
        }

        public static BinaryNode? GetBinaryNodeChild(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.FirstOrDefault(x => x.tag == tag);
            }
            return null;
        }
        public static BinaryNode[] GetBinaryNodeChildren(BinaryNode? message, string tag)
        {
            if (message?.content is BinaryNode[] messages)
            {
                return messages.Where(x => x.tag == tag).ToArray();
            }
            return new BinaryNode[0];
        }

        private static void End(string message, DisconnectReason reason)
        {
            throw new Boom(message, reason);
        }





        public static byte[] EncodeSignedDeviceIdentity(ADVSignedDeviceIdentity account, bool includeSignatureKey)
        {
            var clone = ADVSignedDeviceIdentity.Parser.ParseFrom(account.ToByteArray());

            if (!includeSignatureKey)
            {
                clone.ClearAccountSignatureKey();
            }
            return clone.ToByteArray();
        }

        private static SignalIdentity CreateSignalIdentity(string jid, ByteString accountSignatureKey)
        {
            return new SignalIdentity()
            {
                Identifier = new ProtocolAddress { Name = jid },
                IdentifierKey = AuthenticationUtils.GenerateSignalPubKey(accountSignatureKey.ToByteArray())
            };
        }



        public static BinaryNode GetNextPreKeysNode(AuthenticationCreds creds, KeyStore keys, int count)
        {
            var preKeys = GetNextPreKeys(creds, keys, count);


            var registration = new BinaryNode("registration", EndodingHelper.EncodeBigEndian(creds.RegistrationId));
            var type = new BinaryNode("type", Constants.KEY_BUNDLE_TYPE);
            var identity = new BinaryNode("identity", creds.SignedIdentityKey.Public);

            var list = new BinaryNode("list", preKeys.Select(x => XmppPreKey(x.Value, x.Key)).ToArray());
            var signed = XmppSignedPreKey(creds.SignedPreKey);

            var iq = new BinaryNode("iq", registration, type, identity, list, signed)
            {
                attrs = new Dictionary<string, string>()
                {
                    {"xmlns" ,"encrypt" },
                    {"type","set" },
                    {"to",Constants.S_WHATSAPP_NET }
                },
            };

            return iq;
        }

        private static BinaryNode XmppSignedPreKey(SignedPreKey signedPreKey)
        {
            return new BinaryNode("skey")
            {
                content = new BinaryNode[]
                {
                    new BinaryNode("id", EndodingHelper.EncodeBigEndian(signedPreKey.KeyId,3)),
                    new BinaryNode("value", signedPreKey.KeyPair.Public),
                    new BinaryNode("signature", signedPreKey.Signature),
                }
            };
        }

        private static BinaryNode XmppPreKey(KeyPair value, int key)
        {
            return new BinaryNode("key")
            {
                content = new BinaryNode[]
                {
                    new BinaryNode("id", EndodingHelper.EncodeBigEndian(key,3)),
                    new BinaryNode("value", value.Public),
                }
            };
        }

        private static Dictionary<int, KeyPair> GetNextPreKeys(AuthenticationCreds creds, KeyStore keys, int count)
        {
            var keySet = GenerateOrGetPreKeys(creds, count);

            creds.NextPreKeyId = Math.Max(keySet.LastPreKeyId + 1, creds.NextPreKeyId);
            creds.FirstUnuploadedPreKeyId = Math.Max(creds.FirstUnuploadedPreKeyId, keySet.LastPreKeyId + 1);
            keys.Set(keySet.NewPreKeys);

            var preKeys = GetPreKeys(keys, keySet.PreKeyRange[0], keySet.PreKeyRange[0] + keySet.PreKeyRange[1]);


            return preKeys;
        }

        private static Dictionary<int, KeyPair> GetPreKeys(KeyStore keys, int min, int max)
        {
            List<int> ids = new List<int>();
            for (int i = min; i < max; i++)
            {
                ids.Add(i);
            }
            return keys.Range(ids);
        }

        private static PreKeySet GenerateOrGetPreKeys(AuthenticationCreds creds, int range)
        {
            var avaliable = creds.NextPreKeyId - creds.FirstUnuploadedPreKeyId;
            var remaining = range - avaliable;
            var lastPreKeyId = creds.NextPreKeyId + remaining - 1;
            Dictionary<int, KeyPair> newPreKeys = new Dictionary<int, KeyPair>();
            if (remaining > 0)
            {
                for (int i = creds.NextPreKeyId; i <= lastPreKeyId; i++)
                {
                    newPreKeys[i] = EncryptionHelper.GenerateKeyPair();
                }
            }

            return new PreKeySet()
            {
                NewPreKeys = newPreKeys,
                LastPreKeyId = lastPreKeyId,
                PreKeyRange = new int[] { creds.FirstUnuploadedPreKeyId, range }
            };
        }



        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        public static async Task<bool> ProcessNodeWithBuffer(BinaryNode node, string identifier, Func<BinaryNode, Task> action)
        {
            await semaphoreSlim.WaitAsync();
            await action(node);
            semaphoreSlim.Release();
            return true;
        }
    }
}
