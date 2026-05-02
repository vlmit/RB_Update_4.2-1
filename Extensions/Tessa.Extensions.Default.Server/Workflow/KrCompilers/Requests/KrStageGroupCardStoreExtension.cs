#nullable enable

using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Platform;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.Requests
{
    /// <summary>
    /// Расширение на сохранение группы этапов.
    /// Заполняет поле <see cref="KrStageTypes.TypeIsDocType"/> для секции <see cref="KrStageTypes.Name"/> при сохранении карточки, когда оно не задано.
    /// </summary>
    public sealed class KrStageGroupCardStoreExtension : CardStoreExtension
    {
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

            if (sections.TryGetValue(KrStageTypes.Name, out var stageTypesSection)
                && stageTypesSection.TryGetRows() is { Count: > 0 } rows)
            {
                foreach (var row in rows)
                {
                    if (row.TryGetValue(KrStageTypes.TypeIsDocType, out var typeIsDocType)
                        && typeIsDocType is null)
                    {
                        row[KrStageTypes.TypeIsDocType] = BooleanBoxes.False;
                    }
                }
            }
        }

        #endregion
    }
}
