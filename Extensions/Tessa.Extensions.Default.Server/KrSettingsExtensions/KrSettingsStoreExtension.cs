#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.KrSettingsExtensions
{
    /// <summary>
    /// При сохранении карточки "Типовые настройки" прописывает флаг TypeIsDocType, если необходимо.
    /// </summary>
    public sealed class KrSettingsStoreExtension :
        CardStoreExtension
    {
        #region Private Methods

        private static void FillTypeIsDocType(IReadOnlyDictionary<string, CardSection> sections)
        {
            if (sections.TryGetValue("KrSettingsCycleGrouping", out var krPermissionTypesSection))
            {
                foreach (var row in krPermissionTypesSection.Rows)
                {
                    if (row.TryGetValue(KrConstants.KrStageTypes.TypeIsDocType, out var typeIsDocType) && typeIsDocType is null)
                    {
                        row[KrConstants.KrStageTypes.TypeIsDocType] = BooleanBoxes.False;
                    }
                }
            }
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.TryGetCard() is not { } card
                || card.TryGetSections() is not { } sections)
            {
                return;
            }

            // Заполнить TypeIsDocType.
            FillTypeIsDocType(sections);
        }
        
        #endregion
    }
}
