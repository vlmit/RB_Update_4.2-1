#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    /// <summary>
    /// Представляет собой класс, содержащий настройки редактора OnlyOffice.
    /// </summary>
    public sealed class OnlyOfficeSettings :
        IOnlyOfficeSettings
    {
        #region Constructors

        public OnlyOfficeSettings(
            string? converterUrl,
            string? documentBuilderPath,
            string? webApiBasepath,
            int tokenLifetimePeriod,
            TimeSpan loadTimeout,
            byte[] signatureKey,
            string serverCode,
            byte[]? onlyOfficeSignatureKey = null)
        {
            this.ConverterUrl = converterUrl;
            this.DocumentBuilderPath = documentBuilderPath;
            this.WebApiBasePath = webApiBasepath;
            this.TokenLifetimePeriod = tokenLifetimePeriod;
            this.LoadTimeout = loadTimeout;
            this.SignatureKey = signatureKey;
            this.ServerCode = serverCode;
            this.OnlyOfficeSignatureKey = onlyOfficeSignatureKey;
        }

        #endregion

        #region IOnlyOfficeSettings Members

        /// <inheritdoc />
        public string? ConverterUrl { get; }

        /// <inheritdoc />
        public string? DocumentBuilderPath { get; }

        /// <inheritdoc />
        public string? WebApiBasePath { get; }

        /// <inheritdoc />
        public int TokenLifetimePeriod { get; }

        /// <inheritdoc />
        public TimeSpan LoadTimeout { get; }

        /// <inheritdoc />
        public byte[] SignatureKey { get; }

        /// <inheritdoc />
        public string ServerCode { get; }

        /// <inheritdoc />
        public byte[]? OnlyOfficeSignatureKey { get; }

        #endregion
    }
}
