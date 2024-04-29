using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Models;
using static BaileysCSharp.Core.Utils.GenericUtils;
using static BaileysCSharp.Core.Utils.JidUtils;
using static BaileysCSharp.Core.Helper.CryptoUtils;
using BaileysCSharp.Core.Signal;
using BaileysCSharp.Core.Stores;
using BaileysCSharp.Core.Models.Sessions;
using BaileysCSharp.Core.WABinary;

namespace BaileysCSharp.Core.Utils
{
    public static class SignalUtils
    {
        private static SignedPreKey? ExtractKey(BinaryNode node)
        {
            if (node == null)
                return null;
            var key = new SignedPreKey();
            key.KeyId = GetBinaryNodeChildUInt(node, "id", 3);
            key.Public = GenerateSignalPubKey(GetBinaryNodeChildBuffer(node, "value"));
            key.Signature = GetBinaryNodeChildBuffer(node, "signature");
            return key;
        }

        public static void ParseAndInjectE2ESessions(BinaryNode node, SignalRepository repository)
        {
            var nodes = GetBinaryNodeChildren(GetBinaryNodeChild(node, "list"), "user");

            foreach (var item in nodes)
            {
                var signedKey = GetBinaryNodeChild(item, "skey");
                var key = GetBinaryNodeChild(item, "key");
                var identity = GetBinaryNodeChildBuffer(item, "identity");
                var jid = item.attrs["jid"];
                var registrationId = GetBinaryNodeChildUInt(item, "registration", 4);

                repository.InjectE2ESession(jid, new E2ESession()
                {
                    RegistrationId = registrationId,
                    IdentityKey = GenerateSignalPubKey(identity),
                    SignedPreKey = ExtractKey(signedKey),
                    PreKey = ExtractKey(key)
                }); 
            }

        }

        public static JidWidhDevice[] ExtractDeviceJids(BinaryNode result, string myJid, bool excludeZeroDevices)
        {
            var me = JidDecode(myJid);
            var myUser = me.User;
            var myDevice = me.Device;

            var extracted = new List<JidWidhDevice>();

            if (result.content is BinaryNode[] resultNodes)
            {
                foreach (var node in resultNodes)
                {
                    var list = GetBinaryNodeChild(node, "list")?.content as BinaryNode[];
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            var jidUser = JidDecode(item.attrs["jid"]);
                            var user = jidUser.User;
                            var devicesNode = GetBinaryNodeChild(item, "devices");
                            var deviceListNode = GetBinaryNodeChild(devicesNode, "device-list")?.content as BinaryNode[];
                            if (deviceListNode != null)
                            {
                                foreach (var deviceNode in deviceListNode)
                                {
                                    var tag = deviceNode.tag;
                                    var device = Convert.ToInt32(deviceNode.attrs["id"]);


                                    if (tag == "device" && // ensure the "device" tag
                                        (!excludeZeroDevices || device != 0) && // if zero devices are not-excluded, or device is non zero
                                        (myUser != user || myDevice != device) && // either different user or if me user, not this device
                                        (device == 0 || !string.IsNullOrEmpty(deviceNode.getattr("key-index")))) // ensure that "key-index" is specified for "non-zero" devices, produces a bad req otherwise
                                    {
                                        extracted.Add(new JidWidhDevice() { User = user, Device = device });
                                    }
                                }
                            }
                        }
                    }
                }
            }


            return extracted.ToArray();
        }
    }
}
