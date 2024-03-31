using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Models;
using WhatsSocket.Core.WABinary;

namespace WhatsSocket.Core.Utils
{
    public class MediaMessageUtil
    {
        const string DEF_HOST = "mmg.whatsapp.net";
        const int AES_CHUNK_SIZE = 16;
        public static async Task<byte[]> DownloadContentFromMessage(ExternalBlobReference blob, string type, MediaDownloadOptions options)
        {
            var downloadUrl = $"https://{DEF_HOST}{blob.DirectPath}";
            var keys = GetMediaKeys(blob.MediaKey.ToByteArray(), type);
            return await DownloadEncryptedContent(downloadUrl, keys, options);
        }
        public static async Task<byte[]> DownloadContentFromMessage(Message.Types.HistorySyncNotification blob, string type, MediaDownloadOptions options)
        {
            var downloadUrl = $"https://{DEF_HOST}{blob.DirectPath}";
            var keys = GetMediaKeys(blob.MediaKey.ToByteArray(), type);
            return await DownloadEncryptedContent(downloadUrl, keys, options);
        }

        private static async Task<byte[]> DownloadEncryptedContent(string downloadUrl, MediaDecryptionKeyInfo keys, MediaDownloadOptions options)
        {
            var bytesFetched = 0;
            var startChunk = 0;
            var firstBlockIsIV = false;

            if (options.StartByte > 0)
            {
                var chunk = ToSmallestChunkSize(options.StartByte);
                if (chunk > 0)
                {
                    startChunk = chunk - AES_CHUNK_SIZE;
                    bytesFetched = chunk;
                    firstBlockIsIV = true;
                }
            }

            var endChunk = options.EndByte > 0 ? ToSmallestChunkSize(options.EndByte) + AES_CHUNK_SIZE : 0;

            using (var httpClient = new HttpClient())
            {
                httpClient.MaxResponseContentBufferSize = 2147483647;
                httpClient.DefaultRequestHeaders.Add("Origin", Constants.DEFAULT_ORIGIN);

                var range = "";
                if (startChunk > 0)
                {
                    range = $"bytes={startChunk}-";
                    if (endChunk > 0)
                    {
                        range = $"{range}{endChunk}";
                    }
                }
                if (!string.IsNullOrWhiteSpace(range))
                {
                    httpClient.DefaultRequestHeaders.Add("Range", range);
                }

                var data = await httpClient.GetByteArrayAsync(downloadUrl);
                var decryptLength = ToSmallestChunkSize(data.Length);
                data = data.Slice(0, decryptLength);


                var iv = keys.IV;
                if (firstBlockIsIV)
                {
                    iv = data.Slice(0, AES_CHUNK_SIZE);
                    data = data.Slice(AES_CHUNK_SIZE);
                }

                var decrypted = Helper.CryptoUtils.DecryptAesCbcWithIV(data, keys.CipherKey, iv);
                return decrypted;
            }
        }



        public static MediaDecryptionKeyInfo GetMediaKeys(byte[] buffer, string mediaType)
        {
            var expandedMediaKey = Helper.CryptoUtils.HKDF(buffer, 112, [], HkdifInfoKey(mediaType));

            return new MediaDecryptionKeyInfo()
            {
                IV = expandedMediaKey.Slice(0, 16),
                CipherKey = expandedMediaKey.Slice(16, 48),
                MacKey = expandedMediaKey.Slice(48, 80)
            };
        }

        public static byte[] HkdifInfoKey(string type)
        {
            var hkdfInfo = Constants.MEDIA_HKDF_KEY_MAPPING[type];
            return Encoding.UTF8.GetBytes($"WhatsApp {hkdfInfo} Keys");
        }


        public static int ToSmallestChunkSize(int number)
        {
            return (number / AES_CHUNK_SIZE) * AES_CHUNK_SIZE;
        }


        public static int GetStatusCodeForMediaRetry(string errorCode)
        {
            switch (errorCode)
            {
                case "1":
                    return 200;
                case "2":
                    return 404;
                case "3":
                    return 412;
                case "0":
                    return 418;
                default:
                    return 418;
            }
        }

    }
}
