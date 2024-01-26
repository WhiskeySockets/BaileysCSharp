using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Agreement;
using Google.Protobuf;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using WhatsSocket.Core.Curve;
using WhatsSocket.Core.Models;
using System.Security.Cryptography;
using System.Text;
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
        public static string HmacSign(byte[] data, byte[] key)
        {
            return Convert.ToBase64String(CalculateMAC(key, data));
        }
        public static byte[] CalculateMAC(byte[] key, byte[] data)
        {
            using (var hmacsha256 = new HMACSHA256(key))
            {
                var hash = hmacsha256.ComputeHash(data);
                return hash;
            }
        }

        static string CalculateSHA256Hash(byte[] input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(input);

                // Convert the byte array to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        public static bool VerifyMac(byte[] data, byte[] key, byte[] mac, int length)
        {
            var calculatedMac = CalculateMAC(key, data).Take(length).ToArray();



            return mac.ToBase64() == calculatedMac.ToBase64();
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

        public static byte[] DecryptAesCbc(byte[] encryptedData, byte[] key, byte[] iv)
        {
            byte[] result;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;

                // Create a decryptor to perform the stream transform
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        var outStream = new MemoryStream();
                        csDecrypt.CopyTo(outStream);
                        return outStream.ToArray();
                    }
                }
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

        public static byte[][] DeriveSecrets(byte[] input, byte[] salt, byte[] info, int chunks = 3)
        {
            List<byte[]> signed = new List<byte[]>();
            var PRK = CalculateMAC(salt, input);
            var infoArray = new byte[info.Length + 1 + 32];
            infoArray.Set(info, 32);
            infoArray[infoArray.Length - 1] = 1;
            signed.Add(CalculateMAC(PRK, infoArray.Skip(32).ToArray()));
            if (chunks > 1)
            {
                var signedArray = signed[signed.Count - 1];
                Array.Copy(signedArray, infoArray, signedArray.Length);
                infoArray[infoArray.Length - 1] = 2;
                signed.Add(CalculateMAC(PRK, infoArray));

            }
            if (chunks > 2)
            {
                var signedArray = signed[signed.Count - 1];
                Array.Copy(signedArray, infoArray, signedArray.Length);
                infoArray[infoArray.Length - 1] = 3;
                signed.Add(CalculateMAC(PRK, infoArray));
            }

            return signed.ToArray();
        }


        // Decrypt a string into a string using a key and an IV 
        public static string DecryptCipherIV(byte[] key, byte[] data, byte[] iv)
        {
            try
            {
                using (var rijndaelManaged = new RijndaelManaged { Key = key, IV = iv, Mode = CipherMode.CBC })
                using (var memoryStream = new MemoryStream(data))
                using (var cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(key, iv),
                           CryptoStreamMode.Read))
                {
                    return new StreamReader(cryptoStream).ReadToEnd();
                }
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
            // You may want to catch more exceptions here...
        }
        public static byte[] CalculateAgreement(byte[] publicKey, byte[] privateKey)
        {
            publicKey = publicKey.Skip(1).ToArray();
            return Curve25519.GetSharedSecret(privateKey, publicKey);
        }

        //    public static ECCurve Curve25519 { get; } = new ECCurve()
        //    {
        //        CurveType = ECCurve.ECCurveType.PrimeMontgomery,
        //        B = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        //        A = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x07, 0x6d, 0x06 }, // 486662
        //        G = new ECPoint()
        //        {
        //            X = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9 },
        //            Y = new byte[] { 0x20, 0xae, 0x19, 0xa1, 0xb8, 0xa0, 0x86, 0xb4, 0xe0, 0x1e, 0xdd, 0x2c, 0x77, 0x48, 0xd1, 0x4c,
        //    0x92, 0x3d, 0x4d, 0x7e, 0x6d, 0x7c, 0x61, 0xb2, 0x29, 0xe9, 0xc5, 0xa2, 0x7e, 0xce, 0xd3, 0xd9 }
        //        },
        //        Prime = new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        //0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xed },
        //        //Prime = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        //        Order = new byte[] { 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //0x14, 0xde, 0xf9, 0xde, 0xa2, 0xf7, 0x9c, 0xd6, 0x58, 0x12, 0x63, 0x1a, 0x5c, 0xf5, 0xd3, 0xed },
        //        Cofactor = new byte[] { 8 }
        //    };
    }
}
