using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.QrCode;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Imaging.Placeholders
{
    /// <summary>
    /// Объект, выполняющий форматирование значения поля из строки в текстовый QR-код,
    /// представленный в виде изображения в формате PNG.
    ///
    /// <para>Пример: {fv:Content:#qrcode}</para>
    /// <para>Пример: {fv:Content:#qrcode(w=100;h=100;px=10;t=url;ecc=q;utf8;bom)}</para>
    /// </summary>
    /// <remarks>
    /// Классы-наследники могут переопределять методы, связанные с формированием изображений и форматированием текста.
    /// </remarks>
    public class QRCodePlaceholderFormatter :
        BarcodePlaceholderFormatter
    {
        #region Constants

        /// <summary>
        /// Имя форматтера, по которому он регистрируется.
        /// </summary>
        public new const string FormatterName = "qrcode";

        /// <summary>
        /// Количество пикселей по умолчанию в модуле QR-кода, влияет на общий размер сгенерированной картинки
        /// наравне с длиной текста и типом QR-кода. Изменить настройку можно через свойство <c>#qrcode(px=4)</c>.
        /// </summary>
        private const int DefaultPixelsPerModule = 2;

        #endregion

        #region Base Overrides

        /// <doc path='info[@type="BarcodePlaceholderFormatter" and @item="TryGenerateBarcodeImageAsync"]'/>
        protected override ValueTask<byte[]?> TryGenerateBarcodeImageAsync(
            string text,
            ISerializableObject parameters,
            IPlaceholderImageParameters imageParameters,
            IPlaceholderReplacementContext context,
            IPlaceholder placeholder,
            IPlaceholderFormatSettings formatSettings,
            IPlaceholderFormatRequest request,
            CancellationToken cancellationToken = default)
        {
            var type = QRCodePlaceholderType.Text;
            var typeText = parameters.TryGet<string>("t");
            if (!string.IsNullOrEmpty(typeText))
            {
                if (Enum.TryParse(typeText, true, out QRCodePlaceholderType parsed))
                {
                    type = parsed;
                }
                else
                {
                    // Тип QR-кода указан, но он неизвестен
                    context.ValidationResult.AddError(this,
                        "Unknown QR-code type \"{0}\" for placeholder {1}.",
                        typeText,
                        placeholder.Text);

                    return new((byte[]?) null);
                }
            }

            var eccLevel = ECCLevel.Q;
            var eccLevelText = parameters.TryGet<string>("ecc");
            if (!string.IsNullOrEmpty(eccLevelText))
            {
                if (Enum.TryParse(eccLevelText, true, out ECCLevel parsed))
                {
                    eccLevel = parsed;
                }
                else
                {
                    // ECCLevel указан, но он неизвестен
                    context.ValidationResult.AddError(this,
                        "Unknown ECC level \"{0}\" for placeholder {1}.",
                        eccLevelText,
                        placeholder.Text);

                    return new((byte[]?) null);
                }
            }

            var utf8BOM = parameters.TryGet<bool>("bom");
            var pixelsPerModule = TryParseInt32(parameters, "px") ?? DefaultPixelsPerModule;

            byte[] result;
            try
            {
                var qrCodeData = QrCodeGeneratorHelper.CreateQrCode(text, type, eccLevel, utf8BOM);
                var size = qrCodeData.Size * pixelsPerModule;

                var info = new SKImageInfo(size, size);
                using var surface = SKSurface.Create(info);
                surface.Canvas.Render(qrCodeData, info.Width, info.Height);

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var memStream = new MemoryStream();
                data.SaveTo(memStream);

                result = memStream.ToArray();
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
                        $"Error while generating QR-code for placeholder {placeholder.Text} from value:{Environment.NewLine}{text}",
                        ex)
                    .End();

                return new((byte[]?) null);
            }

            imageParameters.ImageType = PlaceholderImageTypes.Png;
            return new(result);
        }

        #endregion
    }
}
