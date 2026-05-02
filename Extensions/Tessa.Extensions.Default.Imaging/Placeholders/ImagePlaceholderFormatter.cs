using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Tessa.Imaging.Extensions;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Imaging.Placeholders
{
    /// <summary>
    /// Объект, выполняющий форматирование значения поля из массива байтов или из строки Base64 в изображение.
    /// Если тип данных значения несовместим или не конвертируется из строки Base64,
    /// то будет выполнено стандартное форматирование значения в строку.
    ///
    /// <para>Пример: {fv:Content:#image}</para>
    /// <para>Пример: {fv:Content:#image(w=640;h=480;png;reformat)}</para>
    /// </summary>
    /// <remarks>
    /// Классы-наследники могут переопределять методы, связанные с формированием изображений и форматированием текста.
    /// </remarks>
    public class ImagePlaceholderFormatter :
        PlaceholderFormatterBase
    {
        #region Constants

        /// <summary>
        /// Имя форматтера, по которому он регистрируется.
        /// </summary>
        public const string FormatterName = "image";

        #endregion

        #region Protected Methods

        /// <doc path='info[@type="ImagePlaceholderFormatter" and @item="ParseImageParameters"]'/>
        protected virtual IPlaceholderImageParameters ParseImageParameters(
            IDictionary<string, object?> customFormatParameters)
        {
            var imageType = customFormatParameters.TryGet<string>("img");
            if (!string.IsNullOrEmpty(imageType) && !imageType.Contains('/', StringComparison.Ordinal))
            {
                // если задан "jpeg", то меняем его на "image/jpeg"
                imageType = "image/" + imageType;
            }

            return new PlaceholderImageParameters
            {
                Width = TryParseDouble(customFormatParameters, "w") ?? 0.0,
                Height = TryParseDouble(customFormatParameters, "h") ?? 0.0,
                ImageType = imageType,
                AlternativeText = customFormatParameters.TryGet<string>("alt"),
                Reformat = customFormatParameters.TryGet<bool>("reformat"),
            };
        }

        /// <summary>
        /// Возвращает целое число, полученное из заданных параметров по строке <paramref name="key"/>.
        /// В параметрах значение также является строкой, и метод конвертирует его в <see cref="int"/>.
        /// Возвращает <c>null</c>, если параметр не найден или конвертация в целое число невозможна.
        /// </summary>
        /// <param name="parameters">Параметры, в которых будет получено значение в виде строки.</param>
        /// <param name="key">Строка, по которой искомое значение доступно в параметрах <paramref name="parameters"/>.</param>
        /// <returns>
        /// Вещественное число, полученное из заданных параметров
        /// или <c>null</c>, если параметр не найден или конвертация в целое число невозможна.
        /// </returns>
        protected static int? TryParseInt32(IDictionary<string, object?> parameters, string key)
        {
            var text = parameters.TryGet<string>(key);

            return string.IsNullOrEmpty(text)
                ? null
                : int.TryParse(text, out int result) ? result : null;
        }

        /// <summary>
        /// Возвращает вещественное число, полученное из заданных параметров по строке <paramref name="key"/>.
        /// В параметрах значение также является строкой, и метод конвертирует его в <see cref="double"/>.
        /// Возвращает <c>null</c>, если параметр не найден или конвертация в вещественное число невозможна.
        /// </summary>
        /// <param name="parameters">Параметры, в которых будет получено значение в виде строки.</param>
        /// <param name="key">Строка, по которой искомое значение доступно в параметрах <paramref name="parameters"/>.</param>
        /// <returns>
        /// Вещественное число, полученное из заданных параметров
        /// или <c>null</c>, если параметр не найден или конвертация в вещественное число невозможна.
        /// </returns>
        protected static double? TryParseDouble(IDictionary<string, object?> parameters, string key)
        {
            var text = parameters.TryGet<string>(key);

            return string.IsNullOrEmpty(text)
                ? null
                : double.TryParse(text, out double result) ? result : null;
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
            var value = request.Field.Value;
            if (value is null)
            {
                // например, нам из представления пришло null, и это должно соответствовать пустому изображению,
                // по которому документ, например, сможет удалить объект "надпись"

                var parameters = formatSettings.GetCustomFormatParameters();
                var imageParameters = this.ParseImageParameters(parameters);

                return PlaceholderFormatResult.CreateImage(Array.Empty<byte>(), imageParameters, formatSettings);
            }

            var data = value as byte[];

            if (data is null && value is string base64)
            {
                try
                {
                    data = Convert.FromBase64String(base64);
                }
                catch (Exception)
                {
                    data = null;
                }
            }

            if (data is { Length: > 0 })
            {
                int width = 0, height = 0;
                double horizontalResolution = 0.0, verticalResolution = 0.0;

                var parameters = formatSettings.GetCustomFormatParameters();
                var imageParameters = this.ParseImageParameters(parameters);

                // #image(png) конвертирует байты в png, если у них другой тип
                var convertToPng = parameters.TryGet<bool>("png");

                using (var image = Image.Load(data))
                {
                    if (image.TransformForOrientation() || convertToPng)
                    {
                        using var outputStream = new MemoryStream();

                        if (convertToPng)
                        {
                            await image.SaveAsPngAsync(outputStream, cancellationToken);
                            imageParameters.ImageType = PlaceholderImageTypes.Png;
                        }
                        else
                        {
                            await image.SaveAsync(outputStream, Image.DetectFormat(data), cancellationToken);
                        }

                        data = outputStream.ToArray();
                    }

                    width = image.Width;
                    horizontalResolution = image.Metadata.HorizontalResolution;
                    height = image.Height;
                    verticalResolution = image.Metadata.VerticalResolution;
                }

                var result = PlaceholderFormatResult.CreateImage(
                    data,
                    imageParameters,
                    formatSettings,
                    new Dictionary<string, object?>
                    {
                        ["ActualWidth"] = width,
                        ["ActualHorizontalResolution"] = horizontalResolution,
                        ["ActualHeight"] = height,
                        ["ActualVerticalResolution"] = verticalResolution
                    });

                return result;
            }

            return await base.FormatFieldCoreAsync(context, placeholder, formatSettings, request, cancellationToken);
        }

        #endregion
    }
}
