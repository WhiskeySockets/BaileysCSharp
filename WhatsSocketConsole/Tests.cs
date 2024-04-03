using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core;
using System.Xml.Linq;
using Newtonsoft.Json;
using WhatsSocket.Core.Models.Sessions;
using WhatsSocket.Core.WABinary;
using WhatsSocket.Core.Utils;
using Org.BouncyCastle.Bcpg;
using Proto;
using WhatsSocket.LibSignal;
using System.Security.Cryptography;
using WhatsSocket.Core.NoSQL;
using WhatsSocket.Core.Signal;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Textsecure;
using WhatsSocket.Exceptions;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using WhatsSocket.Core.Extensions;

namespace WhatsSocketConsole
{

    public static class Tests
    {

        public static void RunTests()
        {
            TestInitOutgoing();
            TestMessageEncrypt();
        }

        private static void TestMessageEncrypt()
        {
            var config = new SocketConfig()
            {
                ID = "TEST",
            };
            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();
            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);
            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };
            var storage = new SignalStorage(config.Auth);
            var cipher = new SessionCipher(storage, new ProtocolAddress("27797798179@whatsapp.net"));
            var data = Convert.FromBase64String("+gEwChoyNzc5Nzc5ODE3OUBzLndoYXRzYXBwLm5ldBISMhAKDm9oIGhlbGxvIHRoZXJlAwMD");
            var enc = cipher.Encrypt(data);

            Debug.Assert(enc.Data.ToBase64() == "MwohBZTF9+2FCJ5gK4GVpWGbfsHorSV+Ak5kjXBwol/k3Z1HEAAYACJAXTAtgSv9hL3PuFlqCQX2t4dV9S59gKGnVE3CBzod/6sLf3zM9dd3Z2cJh83rqKUGGkoNa5Q6jSqTxyIzanp63IU0ZCFnNqSf");
            enc = cipher.Encrypt(data);
            var session = cipher.GetRecord();
            var currentSession = session.Sessions["BfVT0Dram/Xpa5pBIzH+LbTB3kfro7Y4x3+uhQEIS2sv"];
            Debug.Assert(currentSession.Chains[currentSession.CurrentRatchet.EphemeralKeyPair.Public.ToBase64()].ChainKey.Key.ToBase64() == "1d4d9xbnPgaXS4tckvJNyQ2ijGytNWEUxjoDDkHNHNo=");

        }

        private static void TestInitOutgoing()
        {
            var config = new SocketConfig()
            {
                ID = "TEST",
            };
            var credsFile = Path.Join(config.CacheRoot, $"creds.json");
            AuthenticationCreds? authentication = null;
            if (File.Exists(credsFile))
            {
                authentication = AuthenticationCreds.Deserialize(File.ReadAllText(credsFile));
            }
            authentication = authentication ?? AuthenticationUtils.InitAuthCreds();
            BaseKeyStore keys = new FileKeyStore(config.CacheRoot);
            config.Auth = new AuthenticationState()
            {
                Creds = authentication,
                Keys = keys
            };
            var storage = new SignalStorage(config.Auth);

            var lines = @"Outgoing: 27797798179.17
Be2bRNKMspaCBZbebXcfsFthN07kj3FlvOns/dyHNcFz
BSxNNMB0nIVraSvQ3+00o8Aw6qNJfvp3McNE3rZmb40Q
16591268
1
BXMo39+yxfO9uF6IHoWczHHgqKZ3fkNtnEvLIOmrcrEU
hIN7l2sU/djZWGCp1yb64mfm+kj8HlW4f9qn0af9SAOgoyV51yuQVtmfRigQIZklNVGe0Mp6gwl6oE8K+i5WAg==
BWIb5rB+tzjVPfwoUvJyWTcqyUAoU067jAdgpQRMuXxv
kFQ3siF82p1LwzGBxmAbVINNv4fXlItxU1mmH82061s=
BdWUmSafT6S+K2DBIuc/COjncHSoXm2vyKgJg/UTeThc
COLzV1PCiQLEfrcdUyGq6LBhU8AH8vFElHMyFLm5In4=
Outgoing: 27797798179.17 done";

            var split = lines.Split('\n');

            var inputsess = new E2ESession()
            {
                IdentityKey = Convert.FromBase64String(split[1]),
                PreKey = new PreKeyPair()
                {
                    Public = Convert.FromBase64String(split[2]),
                    KeyId = split[3].ToUInt32(),
                },
                RegistrationId = 569050546,
                SignedPreKey = new SignedPreKey()
                {
                    KeyId = split[4].ToUInt32(),
                    Public = Convert.FromBase64String(split[5]),
                    Signature = Convert.FromBase64String(split[6])
                },
            };

            var baseKey = new KeyPair()
            {
                Public = Convert.FromBase64String(split[7]),
                Private = Convert.FromBase64String(split[8])
            };

            var outKey = new KeyPair()
            {
                Public = Convert.FromBase64String(split[9]),
                Private = Convert.FromBase64String(split[10])
            };


            var sessionBuilder = new SessionBuilder(storage, new ProtocolAddress("27797798179.17@whatsapp.net"));
            sessionBuilder.OutKeyPair = baseKey;
            sessionBuilder.GenKeyPair = outKey;
            var session = sessionBuilder.InitOutGoing(inputsess);
            //Should Match

            var currentSession = session.Sessions["BWIb5rB+tzjVPfwoUvJyWTcqyUAoU067jAdgpQRMuXxv"];
            Debug.Assert(currentSession.CurrentRatchet.RootKey.ToBase64() == "Ia0gQPsFo/btmVVcAVFr1EIw2r0ftYOW8aHs83c/CwY=");
            Debug.Assert(currentSession.IndexInfo.RemoteIdentityKey.ToBase64() == "Be2bRNKMspaCBZbebXcfsFthN07kj3FlvOns/dyHNcFz");
            Debug.Assert(currentSession.Chains[currentSession.CurrentRatchet.EphemeralKeyPair.Public.ToBase64()].ChainKey.Key.ToBase64() == "oP2I/lIzXNMSKPekZOZ3MuRiYPIpkOT27umBjeQwyPQ=");
            Debug.Assert(currentSession.PendingPreKey?.BaseKey?.ToBase64() == "BWIb5rB+tzjVPfwoUvJyWTcqyUAoU067jAdgpQRMuXxv");

        }


    }
}
