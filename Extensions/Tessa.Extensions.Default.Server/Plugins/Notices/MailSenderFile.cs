#nullable enable

using Tessa.Platform.IO;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Represents a file to be sent by email.
    /// </summary>
    public class MailSenderFile(ITempFile file, string? contentID = null)
    {
        /// <summary>
        /// Temp file.
        /// </summary>
        public ITempFile File { get; } = NotNullOrThrow(file);

        /// <summary>
        /// Content ID, if not null, the file will be sent as inline attachments.
        /// Only relevant for embedded images.
        /// </summary>
        public string? ContentID { get; } = contentID;
    }
}
