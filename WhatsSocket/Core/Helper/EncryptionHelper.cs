
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using WhatsSocket.Core.Models;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Agreement;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using WhatsSocket.Core.Credentials;
using System.Text;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using WhatsSocket.Core.Curve;
using System.Linq;
//using Sodium;

namespace WhatsSocket.Core.Helper
{

    public static class EncryptionHelper
    {
        static public KeyPair GenerateKeyPair()
        {
            var x25519KeyPairGenerator = GeneratorUtilities.GetKeyPairGenerator("X25519");
            x25519KeyPairGenerator.Init(new X25519KeyGenerationParameters(new SecureRandom()));

            AsymmetricCipherKeyPair keyPair = x25519KeyPairGenerator.GenerateKeyPair();

            var publicKeyBytes = ((X25519PublicKeyParameters)keyPair.Public).GetEncoded();
            var privateKeyBytes = ((X25519PrivateKeyParameters)keyPair.Private).GetEncoded();

            return new KeyPair()
            {
                Public = publicKeyBytes,
                Private = privateKeyBytes,
            };
        }
        public static byte[] SharedKey(ByteString privateKey, ByteString publicKey)
        {
            return SharedKey(privateKey.ToByteArray(), publicKey.ToByteArray());
        }

        public static byte[] SharedKey(byte[] privateKey, ByteString publicKey)
        {
            return SharedKey(privateKey, publicKey.ToByteArray());
        }
        public static byte[] SharedKey(ByteString privateKey, byte[] publicKey)
        {
            return SharedKey(privateKey.ToByteArray(), publicKey);
        }

        public static byte[] SharedKey(byte[] privateKey, byte[] publicKey)
        {
            X25519PrivateKeyParameters privateParamenter = new X25519PrivateKeyParameters(privateKey, 0);
            X25519PublicKeyParameters publicParamenter = new X25519PublicKeyParameters(publicKey);
            X25519Agreement agreement = new X25519Agreement();
            agreement.Init(privateParamenter);
            byte[] buffer = new byte[agreement.AgreementSize];
            agreement.CalculateAgreement(publicParamenter, buffer, 0);


            return buffer;
        }

        public static bool Verify(byte[] publicKey, byte[] message, byte[] signature)
        {
            publicKey = AuthenticationUtils.GenerateSignalPubKey(publicKey);
            return Curve25519.VerifySignature(publicKey, message, signature);
        }



        public static byte[] Sign(byte[] privateKey, byte[] data)
        {
            var signature = Curve25519.Sign(privateKey, data);
            return signature;

            //Ed25519PrivateKeyParameters privateParamenter = new Ed25519PrivateKeyParameters(privateKey, 0);
            //var signer = SignerUtilities.GetSigner("Ed25519");
            //signer.Init(true, privateParamenter);
            //signer.BlockUpdate(data, 0, data.Length);
            //return signer.GenerateSignature();
        }


        public static byte[] Md5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return hashBytes;
            }
        }
        public static string HmacSign(byte[] text, byte[] key)
        {
            using (var hmacsha256 = new HMACSHA256(key))
            {
                var hash = hmacsha256.ComputeHash(text);
                return Convert.ToBase64String(hash);
            }
        }



        public static byte[] Sha256(IEnumerable<byte> data)
        {
            // Create a SHA-256 digest
            Sha256Digest sha256 = new Sha256Digest();

            // Update the digest with the data
            sha256.BlockUpdate(data.ToArray(), 0, data.Count());

            // Finalize the digest to get the hash
            byte[] hash = new byte[sha256.GetDigestSize()];
            sha256.DoFinal(hash, 0);
            return hash;
        }


        public static byte[] EncryptAESGCM(byte[] plaintext, byte[] encKey, byte[] iv, byte[] hash)
        {
            using (AesGcm aesGcm = new AesGcm(encKey))
            {
                byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
                byte[] ciphertext = new byte[plaintext.Length];
                aesGcm.Encrypt(iv, plaintext, ciphertext, tag, hash);
                var final = ciphertext.Concat(tag).ToArray();
                return final;
            }
        }

        public static byte[] DecryptAESGCM(Span<byte> ciphertext, byte[] encKey, byte[] iv, byte[] hash)
        {
            var GCM_TAG_LENGTH = 128 >> 3;
            using (AesGcm aesGcm = new AesGcm(encKey))
            {
                var enc = ciphertext.Slice(0, ciphertext.Length - GCM_TAG_LENGTH);
                var tag = ciphertext.Slice(ciphertext.Length - GCM_TAG_LENGTH);
                byte[] decryptedData = new byte[ciphertext.Length - 16];
                aesGcm.Decrypt(iv, enc, tag, decryptedData, hash);
                return decryptedData;
            }
        }


        public static byte[] HKDF(byte[] ikm, int length, byte[] salt, byte[] info)
        {
            HkdfBytesGenerator hkdfGenerator = new HkdfBytesGenerator(new Sha256Digest());
            hkdfGenerator.Init(new HkdfParameters(ikm, salt, info));
            byte[] output = new byte[length];
            hkdfGenerator.GenerateBytes(output, 0, length);
            return output;
        }


    }
}
