using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BarcodeStandard;
using SkiaSharp;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Type = BarcodeStandard.Type;

namespace Tessa.Extensions.Default.Imaging.Placeholders
{
    /// <summary>
    /// Объект, выполняющий форматирование значения поля из строки в штрих-код определённого формата,
    /// представленный в виде изображения в формате PNG.
    ///
    /// <para>Пример: {fv:Content:#barcode}</para>
    /// <para>Пример: {fv:Content:#barcode(w=250;h=80;t=ISBN)}</para>
    /// </summary>
    /// <remarks>
    /// Классы-наследники могут переопределять методы, связанные с формированием изображений и форматированием текста.
    /// </remarks>
    public class BarcodePlaceholderFormatter :
        ImagePlaceholderFormatter
    {
        #region Constants

        /// <summary>
        /// Имя форматтера, по которому он регистрируется.
        /// </summary>
        public new const string FormatterName = "barcode";

        /// <summary>
        /// Размер шрифта метки по умолчанию в пикселях. Соответствует размеру шрифта 10pt.
        /// </summary>
        public const float DefaultFontSize = 13.333F;

        #endregion

        #region Protected Methods

        /// <doc path='info[@type="BarcodePlaceholderFormatter" and @item="TryGenerateBarcodeImageAsync"]'/>
        protected virtual ValueTask<byte[]?> TryGenerateBarcodeImageAsync(
            string text,
            ISerializableObject parameters,
            IPlaceholderImageParameters imageParameters,
            IPlaceholderReplacementContext context,
            IPlaceholder placeholder,
            IPlaceholderFormatSettings formatSettings,
            IPlaceholderFormatRequest request,
            CancellationToken cancellationToken = default)
        {
            // по умолчанию кодируем штрих-код как Code128, потому что он более универсальный и часто используемый
            var type = Type.Code128;
            var barcodeTypeText = parameters.TryGet<string>("t");
            if (!string.IsNullOrEmpty(barcodeTypeText))
            {
                if (Enum.TryParse(barcodeTypeText, true, out Type parsed)
                    && parsed != Type.Unspecified)
                {
                    type = parsed;
                }
                else
                {
                    // тип указан, но он неизвестен
                    context.ValidationResult.AddError(this,
                        "Unknown barcode format \"{0}\" for placeholder {1}.",
                        barcodeTypeText,
                        placeholder.Text);

                    return new((byte[]?) null);
                }
            }

            // Поддерживаем старые записи в виде текста
            var includeLabel = parameters.TryGet<object>("l") is { } labelTypeObject
                ? labelTypeObject is not string labelTypeText || !labelTypeText.Equals("none", StringComparison.OrdinalIgnoreCase)
                : false;

            AlignmentPositions? alignment = null;
            var alignmentText = parameters.TryGet<string>("a");
            if (!string.IsNullOrEmpty(alignmentText))
            {
                if (Enum.TryParse(alignmentText, true, out AlignmentPositions parsed))
                {
                    alignment = parsed;
                }
                else
                {
                    // выравнивание указана, но он неизвестно
                    context.ValidationResult.AddError(this,
                        "Unknown alignment \"{0}\" for placeholder {1}.",
                        alignmentText,
                        placeholder.Text);

                    return new((byte[]?) null);
                }
            }

            float fontSize = DefaultFontSize;
            var fontSizeText = parameters.TryGet<string>("fs");
            if (!string.IsNullOrEmpty(fontSizeText))
            {
                if (!float.TryParse(fontSizeText, out fontSize))
                {
                    // размер шрифта указан неверно
                    context.ValidationResult.AddError(this,
                        "Failed to parse font size \"{0}\" for placeholder {1}.",
                        fontSizeText,
                        placeholder.Text);

                    return new((byte[]?) null);
                }
            }

            Barcode? barcode = null;
            SKImage? image = null;
            SKData? encodedData = null;
            MemoryStream? memoryStream = null;

            try
            {
                barcode = new Barcode
                {
                    IncludeLabel = includeLabel
                };
                barcode.LabelFont.Size = fontSize;

                var width = imageParameters.Width;
                if (width > 0.0)
                {
                    barcode.Width = (int) width;
                }

                var height = imageParameters.Height;
                if (height > 0.0)
                {
                    barcode.Height = (int) height;
                }

                if (alignment.HasValue)
                {
                    barcode.Alignment = alignment.Value;
                }

                memoryStream = new MemoryStream(capacity: 80000);

                image = barcode.Encode(type, text);

                encodedData = image.Encode(SKEncodedImageFormat.Png, 100);
                encodedData.SaveTo(memoryStream);

                ((IDisposable) barcode).Dispose();
                barcode = null;

                image.Dispose();
                image = null;

                encodedData.Dispose();
                encodedData = null;

                imageParameters.ImageType = PlaceholderImageTypes.Png;
                return new(memoryStream.ToArray());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(
                        $"Error while generating barcode {type} for placeholder {placeholder.Text} from value:{Environment.NewLine}{text}",
                        ex)
                    .End();

                return new((byte[]?) null);
            }
            finally
            {
                // Resharper можно ошибочно подсказывать, что переменные точно не null, но это не так - всегда возможно исключение
                // ReSharper disable ConstantConditionalAccessQualifier
                memoryStream?.Dispose();
                image?.Dispose();
                encodedData?.Dispose();
                ((IDisposable?) barcode)?.Dispose();
                // ReSharper restore ConstantConditionalAccessQualifier
            }
        }

        #endregion

        #region Base Overrides

        /// <doc path='info[@type="PlaceholderFormatterBase" and @item="FormatFieldCoreAsync"]'/>
        protected override async ValueTask<IPlaceholderFormatResult> FormatFieldCoreAsync(
            IPlaceholderReplacementContext context,
            IPlaceholder placeholder,
            IPlaceholderFormatSettings formatSettings,
            IPlaceholderFormatRequest request,
            CancellationToken cancellationToken = default)
        {
            var text = await this.FormatFieldTextAsync(context, placeholder, formatSettings, request, cancellationToken).ConfigureAwait(false);

            var parameters = formatSettings.GetCustomFormatParameters();
            var imageParameters = this.ParseImageParameters(parameters);

            // для пустого текста возвращаем пустую картинку, чтобы объект "Надпись" был корректно удалён из документа Word/Excel
            var data = string.IsNullOrWhiteSpace(text)
                ? Array.Empty<byte>()
                : await this.TryGenerateBarcodeImageAsync(
                    text, parameters, imageParameters, context, placeholder,
                    formatSettings, request, cancellationToken).ConfigureAwait(false);

            return data is not null
                ? PlaceholderFormatResult.CreateImage(data, imageParameters, formatSettings)
                : new PlaceholderFormatResult(text, formatSettings);
        }

        #endregion
    }
}
