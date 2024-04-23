using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Proto.ClientPayload.Types;
using BaileysCSharp.Core.Helper;
using Google.Protobuf;
using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Exceptions;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Core.WABinary;
using BaileysCSharp.Core.NoSQL;
using static BaileysCSharp.Core.Utils.GenericUtils;
using static BaileysCSharp.Core.Helper.CryptoUtils;
using BaileysCSharp.Core.Extensions;
using Newtonsoft.Json;
using BaileysCSharp.LibSignal;

namespace BaileysCSharp.Core.Utils
{

    public class ValidateConnectionUtil
    {

        public static ClientPayload GenerateRegistrationNode(AuthenticationCreds creds)
        {
            var appVersion = Helper.CryptoUtils.Md5("2.2329.9");
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
                    ESkeyVal = creds.SignedPreKey.Public.ToByteString(),
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


            var advSign = Helper.CryptoUtils.HmacSign(detailsHmac.Details.ToByteArray(), Convert.FromBase64String(creds.AdvSecretKey));

            // check HMAC matches
            var hmac = detailsHmac.Hmac.ToBase64();
            if (hmac != advSign.ToBase64())
            {
                End("Invalid Account Signature", DisconnectReason.BadSession);
            }

            var account = ADVSignedDeviceIdentity.Parser.ParseFrom(detailsHmac.Details);

            var accountMsg = new byte[] { 6, 0 }
            .Concat(account.Details.ToByteArray())
            .Concat(signedIdentityKey.Public).ToArray();
            if (!Helper.CryptoUtils.Verify(account.AccountSignatureKey.ToByteArray(), accountMsg, account.AccountSignature.ToByteArray()))
            {
                End("Failed to verify account Signature", DisconnectReason.BadSession);
            }

            // sign the details with our identity key
            var deviceMsg = new byte[] { 6, 1 }
            .Concat(account.Details.ToByteArray())
            .Concat(signedIdentityKey.Public)
            .Concat(account.AccountSignatureKey).ToArray();
            account.DeviceSignature = CryptoUtils.Sign(signedIdentityKey.Private, deviceMsg).ToByteString();

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

            if (creds.SignalIdentities == null)
            {
                creds.SignalIdentities = [];
            }
            creds.SignalIdentities = creds.SignalIdentities.Concat([identity]).ToArray();
            //creds.Account = account;
            creds.Platform = platformNode.attrs["name"];
            creds.Me = new ContactModel()
            {
                ID = jid,
                Name = bizName
            };
            creds.Account = new Account()
            {
                AccountSignature = account.AccountSignature.ToByteArray(),
                AccountSignatureKey = account.AccountSignatureKey.ToByteArray(),
                Details = account.Details.ToByteArray(),
                DeviceSignature = account.DeviceSignature.ToByteArray(),
            };


            return reply;
        }
        private static void End(string message, DisconnectReason reason)
        {
            throw new Boom(message, reason);
        }





        public static byte[] EncodeSignedDeviceIdentity(Account account, bool includeSignatureKey)
        {
            return EncodeSignedDeviceIdentity(new ADVSignedDeviceIdentity()
            {
                DeviceSignature = account.DeviceSignature.ToByteString(),
                AccountSignature = account.AccountSignature.ToByteString(),
                AccountSignatureKey = account.AccountSignatureKey.ToByteString(),
                Details = account.Details.ToByteString()
            }, includeSignatureKey);
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

        private static SignalIdentity CreateSignalIdentity(string wid, ByteString accountSignatureKey)
        {
            return new SignalIdentity()
            {
                Identifier = new ProtocolAddress { Name = wid },
                IdentifierKey = GenerateSignalPubKey(accountSignatureKey.ToByteArray())
            };
        }



        public static BinaryNode GetNextPreKeysNode(AuthenticationCreds creds, BaseKeyStore keys, uint count)
        {
            var preKeys = GetNextPreKeys(creds, keys, count);


            var registration = new BinaryNode("registration", creds.RegistrationId.EncodeBigEndian());
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

        public static BinaryNode XmppSignedPreKey(SignedPreKey signedPreKey)
        {
            return new BinaryNode("skey")
            {
                content = new BinaryNode[]
                {
                    new BinaryNode("id", signedPreKey.KeyId.EncodeBigEndian(3)),
                    new BinaryNode("value", signedPreKey.Public),
                    new BinaryNode("signature", signedPreKey.Signature),
                }
            };
        }

        public static BinaryNode XmppPreKey(KeyPair value, string key)
        {
            return new BinaryNode("key")
            {
                content = new BinaryNode[]
                {
                    new BinaryNode("id", Convert.ToInt32(key).EncodeBigEndian(3)),
                    new BinaryNode("value", value.Public),
                }
            };
        }

        public static Dictionary<string, PreKeyPair> GetNextPreKeys(AuthenticationCreds creds, BaseKeyStore keys, uint count)
        {
            var keySet = GenerateOrGetPreKeys(creds, count);

            creds.NextPreKeyId = Math.Max(keySet.LastPreKeyId + 1, creds.NextPreKeyId);
            creds.FirstUnuploadedPreKeyId = Math.Max(creds.FirstUnuploadedPreKeyId, keySet.LastPreKeyId + 1);

            foreach (var item in keySet.NewPreKeys)
            {
                keys.Set(item.Key.ToString(), new PreKeyPair(item.Key, item.Value));
            }
            //keys.Set(keySet.NewPreKeys.Select(x => new PreKeyPair(x.Key.ToString(), x.Value)).ToArray());

            var preKeys = GetPreKeys(keys, keySet.PreKeyRange[0], keySet.PreKeyRange[0] + keySet.PreKeyRange[1]);


            return preKeys;
        }

        private static Dictionary<string, PreKeyPair> GetPreKeys(BaseKeyStore keys, uint min, uint max)
        {
            List<string> ids = new List<string>();
            for (uint i = min; i < max; i++)
            {
                ids.Add(i.ToString());
            }
            return keys.Get<PreKeyPair>(ids);
        }

        private static PreKeySet GenerateOrGetPreKeys(AuthenticationCreds creds, uint range)
        {

            var avaliable = creds.NextPreKeyId - creds.FirstUnuploadedPreKeyId;
            var remaining = range - avaliable;
            var lastPreKeyId = creds.NextPreKeyId + remaining - 1;
            Dictionary<uint, KeyPair> newPreKeys = new Dictionary<uint, KeyPair>();
            if (remaining > 0)
            {
                for (uint i = creds.NextPreKeyId; i <= lastPreKeyId; i++)
                {
                    newPreKeys[i] = Curve.GenerateKeyPair();
                }
            }

            return new PreKeySet()
            {
                NewPreKeys = newPreKeys,
                LastPreKeyId = lastPreKeyId,
                PreKeyRange = new uint[] { creds.FirstUnuploadedPreKeyId, range }
            };
        }


    }
}
