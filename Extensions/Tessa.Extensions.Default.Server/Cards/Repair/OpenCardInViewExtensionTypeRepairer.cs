using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Repair;
using Tessa.Cards.TypeSettings;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards.Repair
{
    /// <summary>
    /// Объект содержащий методы для исправления ошибок в OpenCardInViewExtensionType.
    /// </summary>
    public class OpenCardInViewExtensionTypeRepairer : ExtensionTypeRepairerDefault
    {
        #region Base Overrides

        /// <inheritdoc />
        public override ValueTask<RepairResult> RepairAsync(
            CardTypeExtension extension,
            CardType type,
            ICardSchemeInfoProvider cardSchemeInfoProvider,
            IValidationResultBuilder validationResult,
            TypeRepairLevel repairLevel = TypeRepairLevel.Default,
            CancellationToken cancellationToken = default)
        {
            // получаем блок настроек расширения
            ISerializableObject settings = extension.ExtensionSettings;
            if (!settings.ContainsKey(CardControlSettings.ReferenceOpenModeSetting))
            {
                var hasLegacy = RepairHelper.RepairReferenceMode(settings);

                // no reason to store the default value, it will be removed by type serializers
                settings.RemoveIfDefaultEnum(CardControlSettings.ReferenceOpenModeSetting, ReferenceOpenMode.Default);

                if (hasLegacy)
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .SetObjectName(this)
                        .Warning(CardValidationKeys.PropertyFixed, CardControlSettings.ReferenceOpenModeSetting)
                        .End();
                }
            }

            return new(RepairResult.Success);
        }

        #endregion
    }
}
