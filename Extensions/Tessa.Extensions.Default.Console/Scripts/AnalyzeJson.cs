using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class AnalyzeJson : BasicConsoleScriptBase
    {
        #region Nested Types

        private readonly record struct KeyName(object? Value, string[] Keys)
        {
            public string LastKey => this.Keys[^1];

            public string GetText(bool allKeys, int skipCount = 0)
            {
                var keyText = allKeys ? string.Join(" -> ", this.Keys.Skip(skipCount)) : this.LastKey;
                var suffix = this.Value is IList list and not byte[] ? $"[{list.Count}]" : null; // add "[count]" suffix for arrays
                return $"{keyText}{suffix}";
            }

            public override string ToString() => this.GetText(allKeys: true); // for debugging
        }

        private sealed record ResultItem(KeyName KeyName, int Level, double Percentage, string PercentageText, int Size)
        {
            public (string KeyText, string PercentageText, string SizeText) GetMessageComponents(bool padLevel, int keySkipCount = 0)
            {
                var keyText = padLevel
                    ? StringBuilderHelper.Acquire()
                        .Append(' ', this.Level * 2)
                        .Append(this.KeyName.GetText(allKeys: false, keySkipCount))
                        .ToStringAndRelease()
                    : this.KeyName.GetText(allKeys: true, keySkipCount);

                return (keyText, this.PercentageText, FormatInKb(this.Size));
            }

            public override string ToString()
            {
                // for debugging
                var (keyText, percentageText, sizeText) = this.GetMessageComponents(false);
                return $"{keyText}    {percentageText}    {sizeText}";
            }
        }

        private sealed class Analyzer(Dictionary<string, object?> storage, int totalSize)
        {
            public required int DiveLimit { get; init; }

            public required double DivePercentageThreshold { get; init; }

            public List<ResultItem> Analyze(string[] initialKeys)
            {
                var results = new List<ResultItem>();
                this.AnalyzeCore(results, initialKeys, 0);
                return results;
            }

            private void AnalyzeCore(List<ResultItem> results, string[] parentKeys, int level)
            {
                foreach (var (key, value) in DiveIntoStorage(storage, parentKeys).OrderBy(x => x.Key))
                {
                    if (value is not (Dictionary<string, object?> or IList and not byte[]))
                    {
                        continue;
                    }

                    string[] currentKeys = [..parentKeys, key];
                    var item = AnalyzeItem(value, totalSize, level, new(value, currentKeys));
                    results.Add(item);

                    if (value is Dictionary<string, object?> && currentKeys.Length < this.DiveLimit && item.Percentage >= this.DivePercentageThreshold)
                    {
                        this.AnalyzeCore(results, currentKeys, level + 1);
                    }
                }
            }

            private static Dictionary<string, object?> DiveIntoStorage(Dictionary<string, object?> storage, string[] keys)
            {
                try
                {
                    return keys.Aggregate(storage, static (current, key) => NotNullOrThrow(current.Get<Dictionary<string, object?>>(key)));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Can't dive into: {string.Join(" -> ", keys)}", ex);
                }
            }

            private static ResultItem AnalyzeItem(object value, int totalSize, int level, KeyName keyName)
            {
                var newText = value switch
                {
                    Dictionary<string, object?> dict => TessaSerializer.Instance.SerializeJson(dict),
                    IList list and not byte[] => TessaSerializer.Instance.SerializeJsonList(list),
                    _ => throw ArgumentOutOfRange(value.GetType().Name, paramName: nameof(value))
                };

                // diff in size includes: size of the value + length of the last key + quotes around the key (2) + colon (1)
                var size = newText.Length + keyName.LastKey.Length + 3;
                var percentage = size * 100.0 / totalSize;
                var percentageText = $"{percentage:F4}%";
                return new(keyName, level, percentage, percentageText, size);
            }
        }

        #endregion

        #region Private Methods

        private static string FormatInKb(int length) => $"{FormatSize(length, SizeUnit.Kilobytes)}\u00A0{FormatUnit(SizeUnit.Kilobytes)}";

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            var filePath = this.TryGetParameter("path").NormalizePathOnCurrentPlatform();
            if (string.IsNullOrEmpty(filePath))
            {
                await this.Logger.ErrorAsync("Pass the path to the json file in the \"path\" parameter, i.e.: -pp:path=C:\\Files\\storage.json");
                this.Result = -2;
                return;
            }

            var keyParam = this.TryGetParameter("key");
            var initialKeys = keyParam?.Split("-]", StringSplitOptions.RemoveEmptyEntries) ?? [];
            var sort = !string.IsNullOrEmpty(this.TryGetParameter("sort"));
            var topObj = this.TryGetParameter("top");
            var top = !string.IsNullOrEmpty(topObj) && int.TryParse(topObj, CultureInfo.InvariantCulture, out var topValue) ? topValue : 0;
            var diveParam = this.TryGetParameter("dive");
            var dive = !string.IsNullOrEmpty(diveParam) && double.TryParse(diveParam, CultureInfo.InvariantCulture, out var diveValue) ? diveValue : 5.0;
            var maxLevelObj = this.TryGetParameter("max");
            var maxLevel = !string.IsNullOrEmpty(maxLevelObj) && int.TryParse(maxLevelObj, CultureInfo.InvariantCulture, out var maxValue) ? Math.Max(1, maxValue) : 5;

            if (!File.Exists(filePath))
            {
                await this.Logger.ErrorAsync($"File does not exist: {filePath}");
                this.Result = -3;
                return;
            }

            try
            {
                var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
                var storage = TessaSerializer.Instance.DeserializeJsonDictionary(text);
                text = TessaSerializer.Instance.SerializeJson(storage); // fix if the file was indented or serialized via other means

                await this.Logger.InfoAsync($"Total size: {FormatInKb(text.Length)}");
                if (initialKeys.Length > 0)
                {
                    await this.Logger.InfoAsync($"Analyzing inside: {string.Join(" -> ", initialKeys)}");
                }
                else
                {
                    await this.Logger.InfoAsync("Analyzing all keys");
                }

                var analyzer = new Analyzer(storage, text.Length) { DiveLimit = maxLevel + initialKeys.Length, DivePercentageThreshold = dive };
                var results = analyzer.Analyze(initialKeys);

                if (results.Count > 0)
                {
                    await this.Logger.WriteLineAsync();

                    IEnumerable<ResultItem> orderedResults = sort ? results.OrderByDescending(x => x.Size) : results;
                    if (top > 0)
                    {
                        orderedResults = orderedResults.Take(top);
                    }

                    var messageComponents = orderedResults.Select(item => item.GetMessageComponents(padLevel: !sort, keySkipCount: initialKeys.Length)).ToArray();
                    var maxKeyLength = messageComponents.Max(x => x.KeyText.Length);
                    var maxPercentageLength = messageComponents.Max(x => x.PercentageText.Length);
                    foreach (var (keyText, percentageText, sizeText) in messageComponents)
                    {
                        await this.Logger.InfoAsync("{0}    {1}    {2}", keyText.PadRight(maxKeyLength), percentageText.PadRight(maxPercentageLength), sizeText);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error analyzing JSON.", ex);
                this.Result = -1;
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("The script analyzes json file for how much the size of each key is. Useful for metadata optimization.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync(
                "-pp:path=storage.json - the path to json file to analyze (relative to current folder). JSON should start with object {...} on the top-level.");
            await this.Logger.WriteLineAsync(
                "-pp:key=Info-]Themes (optional) - key path to start analysis with (case sensitive), nesting is separated with '-]'. All keys are analyzed when omitted.");
            await this.Logger.WriteLineAsync(
                "-pp:sort=1 (optional) - sort results by size by descending. If omitted - display hierarchy in alphabetic order.");
            await this.Logger.WriteLineAsync(
                "-pp:top=10 (optional) - maximum number of results to show; useful when sorting is applied. If omitted or 0 - show all the results.");
            await this.Logger.WriteLineAsync(
                "-pp:dive=5 (optional) - minimal percentage size for the object when analyzer should dive deeper to analyze its children (5% is default)." +
                " Use 0 to dive into everything without limitations. Diving is supported only for json objects (dictionaries).");
            await this.Logger.WriteLineAsync(
                "-pp:max=3 (optional) - maximum nesting level to dive into the object; use 1 to show only top-level keys. If omitted - dive max up to 5 levels.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(AnalyzeJson)}" +
                @" -pp:path=C:\Files\storage.json -pp:key=Info-]Cards-]GlobalReferences -pp:sort=1 -pp:top=5 -pp:dive=0.5 -pp:max=3");
        }

        #endregion
    }
}
