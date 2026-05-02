namespace Tessa.Extensions.Default.Imaging.Placeholders
{
    /// <summary>
    /// Тип QR-кода для вывода в плейсхолдерах.
    /// </summary>
    public enum QRCodePlaceholderType
    {
        /// <summary>
        /// QR-код, вставляемый в виде текста.
        /// </summary>
        Text,

        /// <summary>
        /// QR-код, вставляемый как URL-ссылка. Если протокол не указан, то используется http://.
        /// </summary>
        Url,

        /// <summary>
        /// QR-код, вставляемый как mailto-ссылка для заданного email получателя.
        /// </summary>
        Email,

        /// <summary>
        /// SMS-сообщение, отправляемое на заданный телефонный номер.
        /// </summary>
        SMS,

        /// <summary>
        /// SMS-сообщение, отправляемое на заданный телефонный номер.
        /// </summary>
        MMS,

        /// <summary>
        /// QR-код, вставляемый как телефонный номер.
        /// </summary>
        Phone,

        /// <summary>
        /// QR-код, вставляемый как звонок заданному пользователю Skype.
        /// </summary>
        Skype,

        /// <summary>
        /// QR-код, вставляемый как WhatsApp-сообщение заданному пользователю.
        /// </summary>
        WhatsApp,
    }
}
