#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.TypeSettings;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Editors;
using Tessa.UI.Cards.Extensions;

namespace Tessa.Extensions.Default.Client.Extensions
{
    /// <summary>
    /// Тип расширения для типа карточки, используется для добавления представлению функционала Истории заданий
    /// </summary>
    public sealed class MakeViewTaskHistoryExtensionType :
        TypeExtensionTypeBase
    {
        #region Constants

        private const string CaptionText = "$UI_Cards_TypesEditor_MakeViewTaskHistory";

        private const string LeftRowColumns = "TypeCaption AuthorName RoleName UserName CompletedByName OptionCaption";

        private const string RightRowColumns = "FilesCount Created Planned InProgress Completed";

        private const string BottomColumns = "Result";

        private const string Token = "Token";

        #endregion

        #region ITypeExtensionType Members

        /// <inheritdoc />
        public override string Caption => CaptionText;

        /// <inheritdoc />
        public override async ValueTask<IEditorViewModel> CreateEditorCoreAsync(
            CardTypeExtension extension,
            CardType type,
            ICardUIResolver cardUIResolver,
            ICardSchemeInfoProvider cardSchemeInfoProvider,
            CancellationToken cancellationToken = default)
        {
            ISerializableObject settings = extension.ExtensionSettings;

            settings.TryAdd(CardTypeExtensionSettings.ViewControlAlias, null);
            settings.TryAdd(CardTypeExtensionSettings.CollapseGroups, BooleanBoxes.False);
            settings.TryAdd(CardTypeExtensionSettings.TokenParameterAlias, Token);
            settings.TryAdd(CardTypeExtensionSettings.LeftRowColumns, LeftRowColumns);
            settings.TryAdd(CardTypeExtensionSettings.RightRowColumns, RightRowColumns);
            settings.TryAdd(CardTypeExtensionSettings.BottomColumns, BottomColumns);

            static string GetCaption(ISerializableObject settings)
            {
                var viewControlAlias = string.Empty;
                if (settings.ContainsKey(CardTypeExtensionSettings.ViewControlAlias))
                {
                    viewControlAlias = $" \"{settings.TryGet<string>(CardTypeExtensionSettings.ViewControlAlias)}\"";
                }

                return Localize(CaptionText) + viewControlAlias;
            }

            IEditorViewModel editor = await PropertyGrid.CreateEditorAsync(
                () => GetCaption(settings),
                cancellationToken,
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_ViewControlAlias",
                    PropertyGridTypes.CreateString(settings, CardTypeExtensionSettings.ViewControlAlias)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_TokenParamAlias",
                    PropertyGridTypes.CreateString(settings, CardTypeExtensionSettings.TokenParameterAlias)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_LeftRowColumns",
                    PropertyGridTypes.CreateString(settings, CardTypeExtensionSettings.LeftRowColumns)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_RightRowColumns",
                    PropertyGridTypes.CreateString(settings, CardTypeExtensionSettings.RightRowColumns)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_BottomColumns",
                    PropertyGridTypes.CreateString(settings, CardTypeExtensionSettings.BottomColumns)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_CollapseGroups",
                    PropertyGridTypes.CreateBool(settings, CardTypeExtensionSettings.CollapseGroups))
            );

            return editor;
        }

        #endregion
    }
}
