#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    public sealed class OnlyOfficeSettingsProvider(
        ICardCache cardCache,
        ITessaServerSettings tessaServerSettings,
        ITessaServerKeyProvider tessaServerKeyProvider)
        : IOnlyOfficeSettingsProvider
    {
        #region Fields

        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        private readonly ITessaServerSettings tessaServerSettings = NotNullOrThrow(tessaServerSettings);

        private readonly ITessaServerKeyProvider tessaServerKeyProvider = NotNullOrThrow(tessaServerKeyProvider);

        #endregion

        #region IOnlyOfficeSettingsProvider Members

        /// <inheritdoc />
        public async ValueTask<IOnlyOfficeSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.cardCache.Cards.GetAsync("OnlyOfficeSettings", cancellationToken);
            if (!result.IsSuccess)
            {
                throw new ValidationException(result.ValidationResult);
            }

            Dictionary<string, object?> fields = result.GetValue().Sections["OnlyOfficeSettings"].RawFields;

            var converterUrl = fields.Get<string>("ConverterUrl");
            var documentBuilderPath = fields.Get<string>("DocumentBuilderPath");
            var webApiBasePath = fields.Get<string>("WebApiBasePath");
            var tokenLifetimePeriod = fields.Get<int>("TokenLifetimePeriod");
            var loadTimeout = TimeSpan.FromMinutes(fields.Get<int>("LoadTimeoutPeriod"));
            var signatureKey = this.tessaServerKeyProvider.GetSignatureKey();
            var serverCode = this.tessaServerSettings.ServerCode;
            var jwtSecret = fields.Get<string>("JWTSecret");
            var onlyOfficeSignatureKey = string.IsNullOrEmpty(jwtSecret) ? null : Encoding.UTF8.GetBytes(jwtSecret);

            return new OnlyOfficeSettings(
                converterUrl,
                documentBuilderPath,
                webApiBasePath,
                tokenLifetimePeriod,
                loadTimeout,
                signatureKey,
                serverCode,
                onlyOfficeSignatureKey);
        }

        #endregion
    }
}
