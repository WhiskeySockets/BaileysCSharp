using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Agreement;
using Google.Protobuf;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using BaileysCSharp.Core.Models;
using System.Security.Cryptography;
using System.Text;
using BaileysCSharp.LibSignal;
//using Sodium;

namespace BaileysCSharp.Core.Helper
{

    public static class CryptoUtils
    {
        public static byte[] GenerateSignalPubKey(byte[] publicKey)
        {
            if (publicKey.Length == 33)
                return publicKey;
            return new byte[] { 5 }.Concat(publicKey).ToArray();
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
            publicKey = GenerateSignalPubKey(publicKey);
            return Curve.VerifySignature(publicKey, message, signature);
        }

        public static byte[] Sign(byte[] privateKey, byte[] data)
        {
            var signature = Curve.Sign(privateKey, data);
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

        public static byte[] HmacSign(byte[] data, byte[] key, string type = "sha256")
        {
            return CalculateMAC(key, data, type);
        }

        public static byte[] CalculateMAC(byte[] key, byte[] data, string type = "sha256")
        {
            if (type == "sha256")
            {
                using (var hmacsha256 = new HMACSHA256(key))
                {
                    var hash = hmacsha256.ComputeHash(data);
                    return hash;
                }
            }
            else
            {
                using (var hmacsha256 = new HMACSHA512(key))
                {
                    var hash = hmacsha256.ComputeHash(data);
                    return hash;
                }
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
            try
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
            catch (Exception)
            {
                throw;
            }
        }

        public static byte[] DecryptAesCbc(byte[] buffer, byte[] key)
        {
            return DecryptAesCbcWithIV(buffer.Slice(16, buffer.Length), key, buffer.Slice(0, 16));
        }

        public static byte[] EncryptAesCbc(byte[] buffer, byte[] key, byte[] iv)
        {
            return EncryptAesCbcWithIV(buffer, key, iv);
        }

        public static byte[] EncryptAesCbcWithIV(byte[] buffer, byte[] key, byte[] iv)
        {
            byte[] result;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;

                // Create a decryptor to perform the stream transform
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(buffer, 0, buffer.Length);
                        csEncrypt.FlushFinalBlock();
                        result = msEncrypt.ToArray();
                    }
                }
            }
            return result;
        }


        public static byte[] DecryptAesCbcWithIV(byte[] buffer, byte[] key, byte[] iv)
        {
            byte[] result;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;

                // Create a decryptor to perform the stream transform
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        var outStream = new MemoryStream();
                        csDecrypt.CopyTo(outStream);
                        result = outStream.ToArray();
                    }
                }
            }
            return result;
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
            return Curve.GetSharedSecret(privateKey, publicKey);
        }

        public static byte[] GetDerivative(byte[] seed, byte[] key)
        {
            var hash = CalculateMAC(key, seed);
            return hash;
        }

    }


    public class HKDF
    {
        public static byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            using (var hmac = new HMACSHA256(salt))
            {
                return hmac.ComputeHash(inputKeyMaterial);
            }
        }

        public static byte[] Expand(byte[] prk, byte[] info, int outputLength)
        {
            int iterations = (int)Math.Ceiling(outputLength / (double)prk.Length);
            byte[] t = new byte[0];
            byte[] okm = new byte[0];

            using (var hmac = new HMACSHA256(prk))
            {
                for (int i = 0; i < iterations; i++)
                {
                    byte[] input = new byte[t.Length + info.Length + 1];
                    Buffer.BlockCopy(t, 0, input, 0, t.Length);
                    Buffer.BlockCopy(info, 0, input, t.Length, info.Length);
                    input[input.Length - 1] = (byte)(i + 1);

                    t = hmac.ComputeHash(input);
                    okm = Concatenate(okm, t);
                }
            }

            return okm;
        }

        private static byte[] Concatenate(byte[] arr1, byte[] arr2)
        {
            byte[] newArr = new byte[arr1.Length + arr2.Length];
            Buffer.BlockCopy(arr1, 0, newArr, 0, arr1.Length);
            Buffer.BlockCopy(arr2, 0, newArr, arr1.Length, arr2.Length);
            return newArr;
        }
    }

}
