using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Repair;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Cards.Editors;
using Tessa.UI.Cards.Extensions;

namespace Tessa.Extensions.Default.Client.Extensions
{
    /// <summary>
    /// Настройки типа расширения для добавления возможности
    /// открытия карточки из представления.
    /// </summary>
    public sealed class OpenCardInViewExtensionType
        : TypeExtensionTypeBase
    {
        #region Constants

        private const string CaptionText = "$UI_Cards_TypesEditor_OpenCardInView";

        #endregion

        #region ITypeExtensionType Members

        /// <inheritdoc/>
        public override string Caption => CaptionText;

        /// <inheritdoc/>
        public override async ValueTask<IEditorViewModel> CreateEditorCoreAsync(
            CardTypeExtension extension,
            CardType type,
            ICardUIResolver cardUIResolver,
            ICardSchemeInfoProvider cardSchemeInfoProvider,
            CancellationToken cancellationToken = default)
        {
            // получаем блок настроек расширения
            ISerializableObject settings = extension.ExtensionSettings;

            // создаём значения по умолчанию, если их нет

            // алиас контрола представления (обязательный)
            settings.TryAdd(DefaultCardTypeExtensionSettings.ViewControlAlias, null);
            // префикс референса
            settings.TryAdd(DefaultCardTypeExtensionSettings.ViewReferencePrefix, null);
            // заголовок диалога, по умолчанию - отсутствует.
            settings.TryAdd(DefaultCardTypeExtensionSettings.CardDialogName, null);

            // режим открытия ссылок
            if (!settings.ContainsKey(CardControlSettings.ReferenceOpenModeSetting))
            {
                RepairHelper.RepairReferenceMode(settings, true);
            }

            // создаём редактор, и возвращаем его
            return await PropertyGrid.CreateEditorAsync(
                () => TypeExtensionTypeHelper.GetCaptionWithViewName(settings, this.Caption),
                cancellationToken,
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_ViewControlAlias",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.ViewControlAlias),
                    "$UI_Cards_TypesEditor_ViewControlAlias_ExtensionToolTip"),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_ReferencePrefix",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.ViewReferencePrefix),
                    "$UI_Cards_TypesEditor_ReferencePrefix_Tooltip"),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_DialogName",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.CardDialogName),
                    "$UI_Cards_TypesEditor_DialogName_ExtensionToolTip"),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_ReferenceOpenMode",
                    PropertyGridTypes.CreateEnum(settings, CardControlSettings.ReferenceOpenModeSetting, CardControlHelper.ReferenceOpenModeItems),
                    "$UI_Cards_TypesEditor_ReferenceOpenMode"));
        }

        #endregion
    }
}
