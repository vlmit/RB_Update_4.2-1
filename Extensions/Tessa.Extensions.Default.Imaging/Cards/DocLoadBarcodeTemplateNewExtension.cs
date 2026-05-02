using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Imaging.DocLoad;
using Tessa.Platform.Licensing;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Imaging.Cards
{
    public sealed class DocLoadBarcodeTemplateNewExtension :
        CardNewExtension
    {
        #region Private Fields

        private readonly ICardCache cardCache;

        private readonly ILicenseManager licenseManager;

        #endregion

        #region Constructor

        public DocLoadBarcodeTemplateNewExtension(ICardCache cardCache, ILicenseManager licenseManager)
        {
            this.cardCache = NotNullOrThrow(cardCache);
            this.licenseManager = NotNullOrThrow(licenseManager);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardNewExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
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

            if (context.Response is null)
            {
                return;
            }

            var sections = context.Response.Card.Sections;
            if (sections.TryGetValue(tableName, out var section)
                && section.Fields.ContainsKey(fieldName))
            {
                section.Fields[fieldName] = null;
            }
        }

        #endregion
    }
}
