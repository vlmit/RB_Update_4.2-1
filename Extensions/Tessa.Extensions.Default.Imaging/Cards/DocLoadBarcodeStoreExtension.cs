using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Imaging.DocLoad;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Licensing;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Sequences;
using Unity;

namespace Tessa.Extensions.Default.Imaging.Cards
{
    public sealed class DocLoadBarcodeStoreExtension :
        CardStoreExtension
    {
        #region Consts

        public const string CreateBarcodeKey = "KrCreateBarcode";

        #endregion

        #region Private Fields

        private readonly ISequenceProvider sequenceProvider;

        private readonly ICardCache cardCache;

        private readonly ISession session;

        private readonly IUnityContainer container;

        private readonly IPlaceholderManager placeholderManager;

        private readonly ILicenseManager licenseManager;

        #endregion

        #region Constructor

        public DocLoadBarcodeStoreExtension(
            ISequenceProvider sequenceProvider,
            ICardCache cardCache,
            ISession session,
            IUnityContainer container,
            IPlaceholderManager placeholderManager,
            ILicenseManager licenseManager)
        {
            this.sequenceProvider = NotNullOrThrow(sequenceProvider);
            this.cardCache = NotNullOrThrow(cardCache);
            this.session = NotNullOrThrow(session);
            this.container = NotNullOrThrow(container);
            this.placeholderManager = NotNullOrThrow(placeholderManager);
            this.licenseManager = NotNullOrThrow(licenseManager);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task BeforeRequest(ICardStoreExtensionContext context)
        {
            if (context.Request.TryGetInfo()?.TryGet<bool>(CreateBarcodeKey) is true)
            {
                context.Request.ForceTransaction = true;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.TryGetInfo()?.TryGet<bool>(CreateBarcodeKey) is not true
                || context.Request.TryGetCard() is not { } card
                || !(await this.licenseManager.GetLicenseAsync(context.CancellationToken)).Modules.Contains(LicenseModules.DocLoadID))
            {
                return;
            }

            var docLoad = await this.cardCache.Cards.GetAsync(CardHelper.DocLoadTypeName, context.CancellationToken);
            if (!docLoad.IsSuccess)
            {
                return;
            }

            var settingsCard = docLoad.GetValue();

            var fields = settingsCard.Sections[DocLoadStrings.DocLoadSettingsSectionName].Fields;
            var isEnabled = fields.TryGet<bool>(DocLoadStrings.IsEnabledFieldName);
            if (!isEnabled)
            {
                return;
            }

            var tableName = fields.TryGet<string>(DocLoadStrings.DefaultBarcodeTableNameFieldName)!;
            var fieldName = fields.TryGet<string>(DocLoadStrings.DefaultBarcodeFieldNameFieldName)!;

            var db = context.DbScope!.Db;
            var barcode = await db.SetCommand(
                    context.DbScope.BuilderFactory
                        .Select()
                        .C(fieldName)
                        .From(tableName).NoLock()
                        .Where()
                        .C("ID").Equals().P("ID")
                        .Build(),
                    db.Parameter("ID", card.ID))
                .LogCommand()
                .ExecuteAsync<string>(context.CancellationToken);

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                // Выходим если номер выделен.
                return;
            }

            var sequenceName = await this.GetPlaceholderInfoAsync(context, fields.TryGet<string>(DocLoadStrings.BarcodeSequenceFieldName));
            if (!context.ValidationResult.IsSuccessful() || string.IsNullOrEmpty(sequenceName))
            {
                return;
            }

            var number = await this.sequenceProvider.AcquireNumberAsync(sequenceName, context.ValidationResult);
            if (number is null)
            {
                return;
            }

            var fullNumber = await this.GetPlaceholderInfoAsync(context, fields.TryGet<string>(DocLoadStrings.BarcodeFormatFieldName), number);
            await db.SetCommand(
                    context.DbScope.BuilderFactory
                        .Update(tableName)
                        .C(fieldName).Assign().P("Value")
                        .Where()
                        .C("ID").Equals().P("ID")
                        .Build(),
                    db.Parameter("Value", fullNumber),
                    db.Parameter("ID", context.Request.Card.ID))
                .LogCommand()
                .ExecuteNonQueryAsync(context.CancellationToken);

            context.Info[CreateBarcodeKey] = fullNumber;
        }

        /// <inheritdoc/>
        public override Task AfterRequest(ICardStoreExtensionContext context)
        {
            if (context.Response is null
                || !context.ValidationResult.IsSuccessful()
                || context.Info.TryGet<string>(CreateBarcodeKey) is null)
            {
                return Task.CompletedTask;
            }

            context.Response.Info[CreateBarcodeKey] = context.Info[CreateBarcodeKey];

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task<string?> GetPlaceholderInfoAsync(
            ICardStoreExtensionContext context,
            string? value,
            long? number = null)
        {
            if (value is null || !value.Contains('{', StringComparison.Ordinal))
            {
                return value;
            }

            var info = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { PlaceholderHelper.ContextKey, context },
                { PlaceholderHelper.SessionKey, this.session },
                { PlaceholderHelper.UnityContainerKey, this.container },
                { PlaceholderHelper.DbScopeKey, context.DbScope! },
                { PlaceholderHelper.CardKey, context.Request.Card },
                { PlaceholderHelper.CardIDKey, context.Request.Card.ID },
                { PlaceholderHelper.CardTypeIDKey, context.Request.Card.TypeID },
                { PlaceholderHelper.NoCardInDbKey, BooleanBoxes.False },
            };

            if (number.HasValue)
            {
                info[PlaceholderHelper.NumberKey] = number.Value;
            }

            var document = new StringPlaceholderDocument(value);

            var result = await this.placeholderManager.FindAndReplaceAsync(document, info, FindingOptions.SkipUnknown, cancellationToken: context.CancellationToken);
            context.ValidationResult.Add(result);

            return result.IsSuccessful ? document.Text : null;
        }

        #endregion
    }
}
