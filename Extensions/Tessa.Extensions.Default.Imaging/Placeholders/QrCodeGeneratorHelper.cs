using SkiaSharp.QrCode;
using Tessa.Imaging.QRCodeGenerator;

namespace Tessa.Extensions.Default.Imaging.Placeholders
{
    /// <summary>
    /// Вспомогательные методы для класса <see cref="SkiaSharp.QrCode.QRCodeGenerator"/>.
    /// </summary>
    public static class QrCodeGeneratorHelper
    {
        #region Public Methods

        /// <summary>
        /// Создает QR-код.
        /// </summary>
        /// <param name="plainText">Кодируемый текст.</param>
        /// <param name="placeholderType"><inheritdoc cref="QRCodePlaceholderType" path="/summary"/></param>
        /// <param name="eccLevel">Уровень коррекции ошибок.</param>
        /// <param name="utf8BOM">Определяет необходимость вставки специальных символов BOM (byte order mark) в начале строки для кодирования UTF-8.</param>
        /// <param name="eciMode">Режим работы со специфическими символами.</param>
        /// <param name="requestedVersion">Версия QR-кода.</param>
        /// <param name="quietZoneSize">Размер чистой зоны вокруг QR-кода.</param>
        /// <returns>Созданный QR-код.</returns>
        public static QRCodeData CreateQrCode(
            string plainText,
            QRCodePlaceholderType placeholderType,
            ECCLevel eccLevel,
            bool utf8BOM = false,
            EciMode eciMode = EciMode.Default,
            int requestedVersion = -1,
            int quietZoneSize = 4)
        {
            var qrCodeText = GetQRCodeText(plainText, placeholderType);
            return QRCodeGenerator.CreateQrCode(qrCodeText, eccLevel, utf8BOM, eciMode, requestedVersion, quietZoneSize);
        }

        #endregion

        #region Private Methods

        private static string GetQRCodeText(string text, QRCodePlaceholderType type) =>
            type switch
            {
                QRCodePlaceholderType.Text => text,
                QRCodePlaceholderType.Url => PayloadGeneratorHelper.Url(text),
                QRCodePlaceholderType.Email => PayloadGeneratorHelper.Mail(text),
                QRCodePlaceholderType.SMS => PayloadGeneratorHelper.SMS(text),
                QRCodePlaceholderType.MMS => PayloadGeneratorHelper.MMS(text),
                QRCodePlaceholderType.Phone => PayloadGeneratorHelper.PhoneNumber(text),
                QRCodePlaceholderType.Skype => PayloadGeneratorHelper.SkypeCall(text),
                QRCodePlaceholderType.WhatsApp => PayloadGeneratorHelper.WhatsAppMessage(text),
                _ => throw ArgumentOutOfRange(type)
            };

        #endregion
    }
}
