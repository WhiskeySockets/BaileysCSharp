using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Credentials;
using WhatsSocket.Core.Models;

namespace WhatsSocket.Core.Helper
{

    public static class AuthenticationUtils
    {
        public static byte[] GenerateSignalPubKey(byte[] publicKey)
        {
            if (publicKey.Length == 33)
                return publicKey;
            return new byte[] { 5 }.Concat(publicKey).ToArray();
        }

        public static AuthenticationCreds InitAuthCreds()
        {
            /*
        noiseKey: Curve.generateKeyPair(),
		pairingEphemeralKeyPair: Curve.generateKeyPair(),
		signedIdentityKey: identityKey,
		signedPreKey: signedKeyPair(identityKey, 1),
		registrationId: generateRegistrationId(),
		advSecretKey: randomBytes(32).toString('base64'),
		processedHistoryMessages: [],
		nextPreKeyId: 1,
		firstUnuploadedPreKeyId: 1,
		accountSyncCounter: 0,
		accountSettings: {
			unarchiveChats: false
		},
		// mobile creds
		deviceId: Buffer.from(uuidv4().replace(/-/g, ''), 'hex').toString('base64url'),
		phoneId: uuidv4(),
		identityId: randomBytes(20),
		registered: false,
		backupToken: randomBytes(20),
		registration: {} as never,
		pairingCode: undefined,
            */

            var creds = new AuthenticationCreds();
            creds.NoiseKey = EncryptionHelper.GenerateKeyPair();
            creds.PairingEphemeralKeyPair = EncryptionHelper.GenerateKeyPair();
            creds.SignedIdentityKey = EncryptionHelper.GenerateKeyPair();
            creds.SignedPreKey = SignedKeyPair(creds.SignedIdentityKey, 1);
            creds.RegistrationId = GenerateRegistrationId();
            creds.AdvSecretKey = RandomBytes(32).ToBase64();
            creds.NextPreKeyId = 1;
            creds.FirstUnuploadedPreKeyId = 1;
            creds.AccountSyncCounter = 0;
            creds.AccountSettings = new AccountSettings()
            {
                UnarchiveChats = false
            };
            // mobile creds
            creds.DeviceId = ConvertToBase64Url(Guid.NewGuid().ToString("N"));
            creds.PhoneId = $"{Guid.NewGuid()}";
            creds.IdentityId = RandomBytes(20);
            creds.Registered = false;
            creds.BackupToken = RandomBytes(20);
            creds.Registration = new Registration() { };



            return creds;
        }


        internal static void Randomize(AuthenticationCreds? creds)
        {
            if (creds != null)
            {
                creds.NoiseKey = EncryptionHelper.GenerateKeyPair();
                creds.AdvSecretKey = RandomBytes(32).ToBase64();
            }
        }
        static string ConvertToBase64Url(string input)
        {
            // Convert hex string to byte array
            byte[] bytes = Enumerable.Range(0, input.Length)
                                     .Where(x => x % 2 == 0)
                                     .Select(x => Convert.ToByte(input.Substring(x, 2), 16))
                                     .ToArray();

            // Convert to base64url
            string base64Url = Convert.ToBase64String(bytes)
                                      .Replace('+', '-')
                                      .Replace('/', '_')
                                      .TrimEnd('=');

            return base64Url;
        }


        public static byte[] RandomBytes(int size)
        {
            Random rnd = new Random((short)DateTime.Now.Ticks);
            byte[] buffer = new byte[size];
            rnd.NextBytes(buffer);
            return buffer;
        }

        public static int GenerateRegistrationId()
        {
            var buffer = RandomBytes(2);
            return buffer[0] & 16383;
        }

        public static SignedPreKey SignedKeyPair(KeyPair identityKeyPair, int keyId)
        {
            var preKey = EncryptionHelper.GenerateKeyPair();
            var pubKey = GenerateSignalPubKey(preKey.Public);





            var signature = EncryptionHelper.Sign(identityKeyPair.Private, pubKey);

            return new SignedPreKey()
            {
                KeyPair = preKey,
                Signature = signature,
                KeyId = keyId
            };
        }

    }
}
