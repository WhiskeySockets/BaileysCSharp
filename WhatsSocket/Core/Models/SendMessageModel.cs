using Proto;
using SkiaSharp;
using System.IO;
using WhatsSocket.Core.Extensions;
using WhatsSocket.Core.Helper;
using WhatsSocket.Core.Utils;
using static Proto.Message.Types;

namespace WhatsSocket.Core.Models
{
    // types to generate WA messages
    public abstract class AnyMessageContent
    {

    }

    public interface IContextable
    {
        ContextInfo? ContextInfo { get; set; }
    }

    public interface IMentionable
    {
        string[] Mentions { get; set; }
    }

    public interface IViewOnce
    {
        bool ViewOnce { get; set; }
    }

    public interface IWithDimentions
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
    }

    public interface IEditable
    {
        public MessageKey? Edit { get; set; }
    }
    public interface IDeleteable
    {
        public MessageKey Delete { get; set; }
    }

    public class MediaUploadData
    {
        public Stream Media { get; set; }
        public string Caption { get; set; }
        public bool Ptt { get; set; }
        public long Seconds { get; set; }
        public bool GifPlayback { get; set; }
        public string FileName { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public string Mimeype { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public byte[] WaveForm { get; set; }
        public long? BackgroundArgb { get; set; }
    }


    public class TextMessageContent : AnyMessageContent, IMentionable, IContextable, IEditable
    {
        public string Text { get; set; }
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }
    }


    public abstract class AnyMediaMessageContent : AnyMessageContent, IMentionable, IContextable, IEditable
    {
        public ContextInfo? ContextInfo { get; set; }
        public string[] Mentions { get; set; }
        public MessageKey? Edit { get; set; }


        public abstract IMediaMessage ToMediaMessage();
    }

    public class ImageMessageContent : AnyMediaMessageContent, IWithDimentions
    {
        private Stream image;

        public Stream Image
        {
            get => image;

            set => OnLoadImage(value);
        }

        private void OnLoadImage(Stream value)
        {
            byte[] copy;
            if (value is MemoryStream memoryStream)
            {
                image = memoryStream;
                copy = memoryStream.ToArray();
            }
            else
            {
                memoryStream = new MemoryStream();
                value.CopyTo(memoryStream);
                image = memoryStream;
                copy = memoryStream.ToArray();

            }
            image.Position = 0;
            FileLength = (ulong)copy.Length;
            using (var bitmap = SKBitmap.Decode(copy))
            {
                Height = (uint)bitmap.Height;
                Width = (uint)bitmap.Width;
                using (var resized = bitmap.Resize(new SKSizeI(32, 32), SKFilterQuality.None))
                {
                    using (var stream = new MemoryStream())
                    {
                        resized.Encode(stream, SKEncodedImageFormat.Jpeg, 50);
                        JpegThumbnail = stream.ToArray();
                    }
                }
            }
        }

        public string Caption { get; set; }
        public byte[] JpegThumbnail { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public ulong FileLength { get; set; }

        public override IMediaMessage ToMediaMessage()
        {
            var image = new ImageMessage()
            {
                ContextInfo = ContextInfo,
                Width = Width,
                Height = Height,
                Mimetype = "image/jpeg",
                JpegThumbnail = JpegThumbnail.ToByteString(),
                MediaKeyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            if (!string.IsNullOrWhiteSpace(Caption))
            {
                image.Caption = Caption;
            }
            return image;
        }
    }

    public class LocationMessageContent : AnyMessageContent
    {
        public LocationMessage Location { get; set; }
    }

    public class ContactShareModel
    {
        public string FullName { get; set; }
        public string Organization { get; set; }
        public string ContactNumber { get; set; }
    }

    public class ContactMessageContent : AnyMessageContent
    {
        public ContactShareModel Contact { get; set; }
    }

    public class DeleteMessageContent : AnyMessageContent, IDeleteable
    {
        public MessageKey? Delete { get; set; }
    }

    public class MessageParticipant
    {
        public string Jid { get; set; }
        public ulong Count { get; set; }
    }

    public class ReactMessageContent : AnyMessageContent
    {
        public string ReactText { get; set; }
        public MessageKey Key { get; set; }

    }

    public interface IMessageRelayOptions : IMinimalRelayOptions
    {
        public MessageParticipant Participant { get; set; }
        public Dictionary<string, string> AdditionalAttributes { get; set; }
        public bool? UseUserDevicesCache { get; set; }

        public List<string>? StatusJidList { get; set; }
    }

    public class MessageRelayOptions : IMessageRelayOptions
    {
        public MessageParticipant Participant { get; set; }
        public Dictionary<string, string> AdditionalAttributes { get; set; }
        public bool? UseUserDevicesCache { get; set; }
        public List<string>? StatusJidList { get; set; }
        public string MessageID { get; set; }
    }

    public interface IMinimalRelayOptions
    {
        public string MessageID { get; set; }
    }

    public interface IMediaUpload
    {
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }

    public interface IMiscMessageGenerationOptions : IMinimalRelayOptions, IMediaUpload
    {
        public ulong Timestamp { get; set; }
        public WebMessageInfo Quoted { get; set; }
        public ulong? EphemeralExpiration { get; set; }
        public List<string>? StatusJidList { get; set; }

    }

    public interface IMessageGenerationOptionsFromContent : IMiscMessageGenerationOptions
    {
        public string UserJid { get; set; }
    }

    public interface IMediaGenerationOptions : IMediaUpload
    {
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }

        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
    }

    public class MediaGenerationOptions : IMediaGenerationOptions
    {
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }
        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
    }


    public interface IMessageContentGenerationOptions : IMediaGenerationOptions
    {

    }


    public class MessageContentGenerationOptions : MediaGenerationOptions
    {



    }

    public interface IMessageGenerationOptions : IMessageGenerationOptionsFromContent, IMessageContentGenerationOptions
    {

    }




    public class MessageGenerationOptions : IMessageGenerationOptions
    {
        public MessageGenerationOptions(IMiscMessageGenerationOptions? options)
        {
            this.CopyMatchingValues(options);
        }

        public string UserJid { get; set; }
        public ulong Timestamp { get; set; }
        public WebMessageInfo Quoted { get; set; }
        public ulong? EphemeralExpiration { get; set; }
        public string MessageID { get; set; }
        public Logger Logger { get; set; }
        public string? MediaTypeOveride { get; set; }
        public Func<MemoryStream, MediaUploadOptions, Task<MediaUploadResult>> Upload { get; set; }
        public ulong? MediaUploadTimeoutMs { get; set; }
        public string? BackgroundColor { get; set; }
        public ulong? Font { get; set; }
        public List<string>? StatusJidList { get; set; }
    }
}
