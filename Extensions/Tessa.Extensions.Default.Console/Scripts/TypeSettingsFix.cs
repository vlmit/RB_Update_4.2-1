using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.TypeSettings;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public class TypeSettingsFix : ServerConsoleScriptBase
    {
        #region Constants

        private const string LegacyFont1 = "Segoe UI";

        private const string LegacyFont2 = "Segoe UI, ./Fonts/#Open Sans";

        #endregion

        #region FixerVisitor Private Class

        public sealed class FixerVisitor : CardTypeVisitor
        {
            public bool HasChanges { get; set; }

            /// <inheritdoc />
            public override ValueTask VisitControlAsync(
                CardTypeControl control,
                CardTypeBlock? block,
                CardTypeForm? form,
                CardType type,
                CancellationToken cancellationToken = default)
            {
                if (control.ControlSettings.TryGet<object>("TextStyle") is Dictionary<string, object?> textStyle
                    && textStyle.TryGet<object>(CardTextStyleSettings.SelectedFontFamilySetting) is string and (LegacyFont1 or LegacyFont2))
                {
                    textStyle[CardTextStyleSettings.SelectedFontFamilySetting] = null;
                    this.HasChanges = true;
                }

                if (control.ControlSettings.TryGet<object>("TextColor") is string textColor
                    && string.Equals(textColor, "#FF000000", StringComparison.OrdinalIgnoreCase))
                {
                    control.ControlSettings["TextColor"] = null;
                    this.HasChanges = true;
                }

                if (control.ControlSettings.TryGet<object>("Bakground") is { } backgroundLegacy)
                {
                    // first change legacy key to actual, so that default legacy color can be set to null below
                    control.ControlSettings.Remove("Bakground");
                    if (control.ControlSettings.TryGet<object>("Background") is null)
                    {
                        control.ControlSettings["Background"] = backgroundLegacy;
                    }

                    this.HasChanges = true;
                }

                if (control.ControlSettings.TryGet<object>("Background") is string background
                    && background.StartsWith("#00", StringComparison.OrdinalIgnoreCase))
                {
                    // transparency is zero no matter the actual color; usually its #00FFFFFF, but in some instances we see #00000000
                    control.ControlSettings["Background"] = null;
                    this.HasChanges = true;
                }

                if (control.ControlSettings.TryGet<object>("BorderColor") is string borderColor
                    && borderColor.StartsWith("#00", StringComparison.OrdinalIgnoreCase))
                {
                    // transparency is zero no matter the actual color; usually its #00FFFFFF, but in some instances we see #00000000
                    control.ControlSettings["BorderColor"] = null;
                    this.HasChanges = true;
                }

                if (control.ControlSettings.TryGet<object>("BorderThiknes") is { } borderThicknessLegacy)
                {
                    control.ControlSettings.Remove("BorderThiknes");
                    if (control.ControlSettings.TryGet<object>("BorderThickness") is null)
                    {
                        control.ControlSettings["BorderThickness"] = borderThicknessLegacy;
                    }

                    this.HasChanges = true;
                }

                if (control.BlockSettings.TryGet<object>("CaptionStyle") is Dictionary<string, object?> captionStyle)
                {
                    if (captionStyle.TryGet<object>("TextColor") is string captionTextColor
                        && string.Equals(captionTextColor, "#FF505050", StringComparison.OrdinalIgnoreCase))
                    {
                        captionStyle["TextColor"] = null;
                        this.HasChanges = true;
                    }

                    if (captionStyle.TryGet<object>("TextStyle") is Dictionary<string, object?> textCaptionStyle
                        && textCaptionStyle.TryGet<object>(CardTextStyleSettings.SelectedFontFamilySetting) is string and (LegacyFont1 or LegacyFont2))
                    {
                        textCaptionStyle[CardTextStyleSettings.SelectedFontFamilySetting] = null;
                        this.HasChanges = true;
                    }
                }

                return base.VisitControlAsync(control, block, form, type, cancellationToken);
            }
        }

        #endregion

        #region Base Overrides

        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            string? source = this.TryGetParameter("source");
            if (string.IsNullOrEmpty(source))
            {
                await this.Logger.ErrorAsync(
                    "Pass the path to .jtype file or folders with such files in the" +
                    " \"dir\" parameter, i.e.: -pp:source=C:\\Repository\\Configuration\\Types");
                this.Result = -1;
                return;
            }

            var visitor = new FixerVisitor();
            foreach (string filePath in DefaultConsoleHelper.GetSourceFiles(source, "*.jtype"))
            {
                try
                {
                    await this.Logger.InfoAsync("Checking font fixes in file \"{0}\"", filePath);
                    string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);

                    var type = NotNullOrThrow(await CardSerializableObject.DeserializeFromJsonAsync<CardType>(json, null, cancellationToken));

                    visitor.HasChanges = false;
                    await type.VisitAsync(visitor, cancellationToken);

                    if (visitor.HasChanges)
                    {
                        await this.Logger.InfoAsync("Saving font fixes in file \"{0}\"", filePath);
                        json = await type.SerializeToJsonAsync(indented: true, cancellationToken);
                        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await this.Logger.LogExceptionAsync($"An error occurred during the operation with file \"{filePath}\": ", ex);
                }
            }

            this.Result = 0;
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Fixes settings for controls and blocks in types files .jtype.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("-pp:source=PathToTypes - Specifies path to a type or to a folder containing types (recursively in subfolders).");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(TypeSettingsFix)}" +
                " -pp:source=C:\\Repository\\Configuration\\Types");
        }

        #endregion
    }
}
