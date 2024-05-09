using Org.BouncyCastle.Asn1.X509;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BaileysCSharp.Core.Helper;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.WABinary;
using BaileysCSharp.Exceptions;
using static Proto.Message.Types.BCallMessage.Types;
using static Proto.Message.Types.InteractiveMessage.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;
using BaileysCSharp.Core.Types;
using System.Text.Json;

namespace BaileysCSharp.Core.Utils
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
            type = type.ToLower();
            var hkdfInfo = Constants.MEDIA_HKDF_KEY_MAPPING[type];
            return Encoding.UTF8.GetBytes($"WhatsApp {hkdfInfo} Keys");
        }


        public static int ToSmallestChunkSize(int number)
        {
            return (number / AES_CHUNK_SIZE) * AES_CHUNK_SIZE;
        }


        public static Dictionary<string, string> MEDIA_PATH_MAP = new Dictionary<string, string>()
        {
            { "Image","/mms/image" },
            { "Video","/mms/video" },
            { "Document","/mms/document" },
            { "Audio","/mms/audio" },
            { "Sticker","/mms/sticker" },
            { "thumbnail-link","/mms/sticker" },
            { "product-catalog-image","/product/image" },
            { "md-app-state","" },
            { "md-msg-hist","/mms/md-app-state" }
        };

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

        public static async Task<MediaUploadResult> GetWAUploadToServer(SocketConfig socketConfig, MemoryStream stream, MediaUploadOptions options, Func<bool, Task<MediaConnInfo>> refreshMediaConn)
        {

            List<MediaUploadResult> urls = new List<MediaUploadResult>();
            var uploadInfo = await refreshMediaConn(false);
            var body = stream.ToArray();

            options.FileEncSha256B64 = EncodeBase64EncodedStringForUpload(options.FileEncSha256B64);
            uploadInfo.Hosts.Reverse();
            foreach (var item in uploadInfo.Hosts)
            {
                socketConfig.Logger.Debug($"uploading to {item.HostName}");
                var auth = EncodeURIComponent(uploadInfo.Auth);
                var url = $"https://{item.HostName}{MEDIA_PATH_MAP[options.MediaType]}/{options.FileEncSha256B64}?auth={auth}&token={options.FileEncSha256B64}";

                try
                {
                    if (item.MaxContentLengthBytes > 0 && body.Length > item.MaxContentLengthBytes)
                    {
                        throw new Boom($"Body too large for {item.HostName}");
                    }
                    using (var httpClient = new HttpClient())
                    {
                        // Set headers
                        httpClient.DefaultRequestHeaders.Add("Origin", Constants.DEFAULT_ORIGIN);

                        // Create ByteArrayContent with the data and set the media type
                        ByteArrayContent content = new ByteArrayContent(body);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                        HttpResponseMessage response = await httpClient.PostAsync(url, content);


                        // Check the response status
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var result = JsonSerializer.Deserialize<MediaUploadResult>(json);
                            if (result != null)
                            {
                                urls.Add(result);
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Error: {response.StatusCode}");
                        }


                    }
                }
                catch (Exception ex)
                {

                }
            }

            return urls.FirstOrDefault();
        }


        public static string EncodeBase64EncodedStringForUpload(string b64)
        {
            string encoded = EncodeURIComponent(
                b64
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "")
            );
            return encoded;
        }

        public static string EncodeURIComponent(string str)
        {
            string encoded = Uri.EscapeDataString(str);
            return encoded;
        }
    }
}
