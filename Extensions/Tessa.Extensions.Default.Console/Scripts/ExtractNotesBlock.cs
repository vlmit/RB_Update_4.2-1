using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class ExtractNotesBlock : BasicConsoleScriptBase
    {
        #region Private Methods

        // \S guarantees that there is some text before and after the header (i.e., it's not start or end of the file)
        private static readonly Regex headerRegex = new(@"\S\n[\n\s]+\S", RegexOptions.CultureInvariant);

        private static readonly Regex blockRegex = new(@"^\w.*$", RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static string ExtractNotesCore(string text, bool skipHeaderTrim)
        {
            text = text.NormalizeLineEndingsUnixStyle();

            // skip header, i.e., everything before a first empty line
            if (!skipHeaderTrim)
            {
                if (headerRegex.Match(text) is { Success: true } headerMatch)
                {
                    // the last symbol in the match is \S, we should include it
                    text = text[(headerMatch.Index + headerMatch.Length - 1)..];
                }
            }

            // find a first block, i.e., line starting with a word character (not a descriptor symbol, such as "# Something changed")
            if (blockRegex.Match(text) is not { Success: true } firstBlock)
            {
                // no blocks in an entire file, return everything
                return text;
            }

            // skip the first block
            text = text[(firstBlock.Index + firstBlock.Length)..];

            // find a second block
            if (blockRegex.Match(text) is not { Success: true } secondBlock)
            {
                // only one block, return it
                return text;
            }

            // return text between first and second blocks
            return text[..(secondBlock.Index - 1)];
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            var filePath = this.TryGetParameter("path").NormalizePathOnCurrentPlatform();
            if (string.IsNullOrEmpty(filePath))
            {
                await this.Logger.ErrorAsync("Pass the path to the notes file in the \"path\" parameter, i.e.: -pp:path=C:\\Repository\\ReleaseNotes.txt");
                this.Result = -2;
                return;
            }

            if (!File.Exists(filePath))
            {
                await this.Logger.ErrorAsync($"Notes file does not exist: {filePath}");
                this.Result = -3;
                return;
            }

            var outPath = this.TryGetParameter("out").NormalizePathOnCurrentPlatform();
            var skipHeaderTrim = !string.IsNullOrEmpty(this.TryGetParameter("skipHeaderTrim"));

            await this.Logger.InfoAsync("Extracting notes from file ({0}): {1}", skipHeaderTrim ? "do not trim a header" : "trim a header", filePath);

            try
            {
                var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
                var notes = ExtractNotesCore(text, skipHeaderTrim).Trim();
                // write results even if they are empty

                if (!string.IsNullOrEmpty(outPath))
                {
                    await this.Logger.InfoAsync("Writing output to file: {0}", outPath);

                    var outFolder = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outFolder))
                    {
                        FileHelper.CreateDirectoryIfNotExists(outFolder);
                    }

                    await File.WriteAllTextAsync(outPath, notes, Encoding.UTF8, cancellationToken);
                }
                else
                {
                    await this.Logger.WriteLineAsync(notes);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error extracting notes.", ex);
                this.Result = -1;
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync(
                "The script extracts first notes block from the text file in ReleaseNotes format." +
                " Writes result to the specified file or to standard output.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync(
                "-pp:path=ReleaseNotes.txt - the path to text file to extract the notes from (relative to the current folder).");
            await this.Logger.WriteLineAsync(
                "-pp:out=ExtractedNotesBlock.txt (optional) - the path to the file to write results to (relative to the current folder)." +
                " If omitted then the results are written to standard console output.");
            await this.Logger.WriteLineAsync(
                "-pp:skipHeaderTrim=1 (optional) - do not attempt to skip the file's header, i.e. text before the first empty line. Any non-empty value is treated as true.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(ExtractNotesBlock)}" +
                " -pp:path=ReleaseNotes.txt -pp:out=ReleaseNotes.block.txt");
        }

        #endregion
    }
}
