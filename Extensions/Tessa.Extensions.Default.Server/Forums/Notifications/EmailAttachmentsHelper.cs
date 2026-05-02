#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using Tessa.Forums.Models;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Forums.Notifications
{
    public static class EmailAttachmentsHelper
    {
        #region Private Fields
        
        private const string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static readonly IdnMapping encoder = new();
        
        private static readonly Regex srcRegex = new(@"src=""http.+?""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex imgModeRegex = new(@"data-image-mode=""mode:.*;width:(.*);height:(.*);keepRatio:(.*);""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        private const string DefaultDomain = "forums";
        
        #endregion
        
        #region Public Constants

        public const string CidKey = "Cid";
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// Generate a Message-Id or Content-Id.
        /// </summary>
        /// <remarks>
        /// Generates a new Message-Id (or Content-Id) using the supplied domain.
        /// </remarks>
        /// <returns>The message identifier.</returns>
        /// <param name="domain">A domain to use.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="domain"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="domain"/> is invalid.
        /// </exception>
        public static string GenerateMessageId(string domain)
        {
            ThrowIfNullOrEmpty(domain);

            var value = (ulong) DateTime.UtcNow.Ticks;
            var id = StringBuilderHelper.Acquire(64);
            
            Span<byte> block = stackalloc byte[8];

            RandomNumberGenerator.Fill(block);

            do
            {
                id.Append(Base36[(int) (value % 36)]);
                value /= 36;
            } while (value != 0);

            id.Append('.');

            value = 0;
            for (var i = 0; i < 8; i++)
            {
                value = (value << 8) | block[i];
            }

            do
            {
                id.Append(Base36[(int) (value % 36)]);
                value /= 36;
            } while (value != 0);

            id.Append('@');
            id.Append(encoder.GetAscii(domain));

            return id.ToString();
        }
        
        /// <summary>
        /// Creates wrapper for html body.
        /// </summary>
        /// <param name="htmlBody">Html string.</param>
        /// <returns><inheritdoc cref="HtmlBodyInfo" path="/summary"/></returns>
        public static HtmlBodyInfo GetHtmlBodyInfo(string? htmlBody)
        {
            return new HtmlBodyInfo
            {
                HtmlBody = htmlBody
            };
        }

        /// <summary>
        /// Attaches in-line images to html body wrapper.
        /// </summary>
        /// <param name="htmlBodyInfo"><see cref="HtmlBodyInfo"/> containing images with url as it's source.</param>
        /// <param name="domain">Suffix for cid generation, default value - forums.</param>
        public static void AttachInlineImages(HtmlBodyInfo htmlBodyInfo, string domain = DefaultDomain)
        {
            ThrowIfNull(htmlBodyInfo);

            if (string.IsNullOrEmpty(htmlBodyInfo.HtmlBody))
            {
                return;
            }

            var originalHtmlBody = htmlBodyInfo.HtmlBody;

            var imgModeMatches = imgModeRegex.Matches(originalHtmlBody);
            foreach (Match imgModeMatch in imgModeMatches)
            {
                if (!int.TryParse(imgModeMatch.Groups[1].Value, out var width)
                    || !int.TryParse(imgModeMatch.Groups[2].Value, out var height)
                    || !bool.TryParse(imgModeMatch.Groups[3].Value, out var keepRatio))
                {
                    continue;
                }

                htmlBodyInfo.HtmlBody = htmlBodyInfo.HtmlBody.Replace(
                    imgModeMatch.Value,
                    keepRatio ? $"width=\"{width}\";" : $"width=\"{width}\";height=\"{height}\"",
                    StringComparison.OrdinalIgnoreCase);
            }

            MatchCollection srcMatches;
            if ((srcMatches = srcRegex.Matches(originalHtmlBody)).Count == 0)
            {
                return;
            }

            foreach (Match match in srcMatches)
            {
                var len = match.Value.Length - 1;
                var uri = new Uri(match.Value[5..len]);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                var fileId = queryParams.Get("amp;fileID")!;
                var cid = GenerateMessageId(domain);
                htmlBodyInfo.HtmlBody = htmlBodyInfo.HtmlBody
                    .Replace(match.Value,
                        $"src=\"cid:{cid}\"",
                        StringComparison.InvariantCulture);
                htmlBodyInfo.FileInfos.Add(new CidFileInfo
                {
                    FileID = Guid.Parse(fileId),
                    FileName = queryParams.Get("amp;FileName")!,
                    Cid = cid
                });
            }
        }
        
        /// <summary>
        /// Adds other attachments to html body wrapper.
        /// </summary>
        /// <param name="bodyInfo"><see cref="HtmlBodyInfo"/></param>
        /// <param name="attachments">List of items to add.</param>
        /// <param name="domain">Suffix for cid generation, default value - forums.</param>
        public static void AddOtherAttachments(HtmlBodyInfo bodyInfo, IReadOnlyCollection<ItemModel> attachments, string domain = DefaultDomain)
        {
            ThrowIfNull(bodyInfo);
            ThrowIfNull(attachments);

            var existedAttachments = bodyInfo.FileInfos.Select(x => x.FileID).ToHashSet();
            foreach (var attachment in attachments)
            {
                if (existedAttachments.Contains(attachment.ID))
                {
                    continue;
                }

                var cidFileInfo = new CidFileInfo
                {
                    FileID = attachment.ID,
                    Cid = GenerateMessageId(domain),
                    FileName = attachment.FileName
                };
                bodyInfo.FileInfos.Add(cidFileInfo);
            }
        }
        
        #endregion
        
        #region Nested Classes

        /// <summary>
        /// Wrapper object under html email body,
        /// which is aware of possible cid attachments.
        /// </summary>
        public class HtmlBodyInfo
        {
            /// <summary>
            /// Html body in which all url sources of images,
            /// were replaced by cid value.
            /// </summary>
            public string? HtmlBody { get; set; }

            /// <summary>
            /// List of the files, which contain content for each image.
            /// </summary>
            public List<CidFileInfo> FileInfos { get; set; } = [];
        }

        public class CidFileInfo
        {
            public Guid? FileID { get; init; }
            public string? FileName { get; init; }
            public string? Cid { get; init; }
        }

        #endregion
    }
}
