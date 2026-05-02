using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Cards.Editors;
using Tessa.UI.Cards.Extensions;
using Tessa.UI.Files;
using Tessa.Views;

namespace Tessa.Extensions.Default.Client.Extensions
{
    public sealed class InitializeFilesViewExtensionType(
        ICardMetadata cardMetadata,
        ICardDialogManager cardDialogManager,
        ICardSchemeInfoProvider cardSchemeInfoProvider)
        : TypeExtensionTypeBase
    {
        #region Constants

        private const string CaptionText = "$UI_Cards_TypesEditor_InitializeFilesView";

        #endregion

        #region Fields

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly ICardDialogManager cardDialogManager = NotNullOrThrow(cardDialogManager);
        private readonly ICardSchemeInfoProvider cardSchemeInfoProvider = NotNullOrThrow(cardSchemeInfoProvider);

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
            settings.TryAdd(DefaultCardTypeExtensionSettings.FilesViewAlias, null);
            settings.TryAdd(DefaultCardTypeExtensionSettings.CategoriesViewAlias, CardControlSettings.FileCategoriesFilteredViewAlias);
            settings.TryAdd(DefaultCardTypeExtensionSettings.PreviewControlName, null);
            settings.TryAdd(DefaultCardTypeExtensionSettings.DefaultGroup, null);
            settings.TryAdd(DefaultCardTypeExtensionSettings.IsCategoriesEnabled, BooleanBoxes.False);
            settings.TryAdd(DefaultCardTypeExtensionSettings.IsManualCategoriesCreationDisabled, BooleanBoxes.False);
            settings.TryAdd(DefaultCardTypeExtensionSettings.IsNullCategoryCreationDisabled, BooleanBoxes.False);
            settings.TryAdd(DefaultCardTypeExtensionSettings.IsIgnoreExistingCategories, BooleanBoxes.False);
            settings.TryAdd(DefaultCardTypeExtensionSettings.CategoriesViewMapping, null);
            settings.TryAdd(DefaultCardTypeExtensionSettings.PagingMode, Int32Boxes.Box((int) Paging.No));
            settings.TryAdd(DefaultCardTypeExtensionSettings.PageLimit, DefaultTypeExtensionTypeHelper.DefaultPageLimit);

            static string GetCaptionAsync(ISerializableObject settings)
            {
                string viewControlAlias = string.Empty;
                if (settings.ContainsKey(DefaultCardTypeExtensionSettings.FilesViewAlias))
                {
                    viewControlAlias = $" \"{settings.TryGet<string>(DefaultCardTypeExtensionSettings.FilesViewAlias)}\"";
                }

                return Localize(CaptionText) + viewControlAlias;
            }

            return await PropertyGrid.CreateEditorAsync(
                () => GetCaptionAsync(settings),
                cancellationToken,
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_ViewControlAlias",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.FilesViewAlias)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_CategoriesView",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.CategoriesViewAlias)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_FilePreviewControlAlias",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.PreviewControlName)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_Grouping",
                    PropertyGridTypes.CreateString(settings, DefaultCardTypeExtensionSettings.DefaultGroup),
                    string.Format(
                        "{1} - " + await LocalizeNameAsync("UI_Cards_TypesEditor_CategoryGrouping_Tooltip") + "{0}" +
                        "{2} - " + await LocalizeNameAsync("UI_Cards_TypesEditor_CopyGrouping_Tooltip"),
                        Environment.NewLine, FileGroupingNames.Category, FileGroupingNames.Copy)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_CategoriesViewMapping",
                    PropertyGridTypes.CreateViewMap(
                        settings,
                        DefaultCardTypeExtensionSettings.CategoriesViewMapping,
                        type,
                        null,
                        this.cardMetadata,
                        this.cardDialogManager,
                        this.cardSchemeInfoProvider)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_UseCategories",
                    PropertyGridTypes.CreateBool(settings, DefaultCardTypeExtensionSettings.IsCategoriesEnabled)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_CreateCategoriesDisabled",
                    PropertyGridTypes.CreateBool(settings, DefaultCardTypeExtensionSettings.IsManualCategoriesCreationDisabled)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_CreateNullCategoryDisabled",
                    PropertyGridTypes.CreateBool(settings, DefaultCardTypeExtensionSettings.IsNullCategoryCreationDisabled)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_IsIgnoreExistingCategories",
                    PropertyGridTypes.CreateBool(settings, DefaultCardTypeExtensionSettings.IsIgnoreExistingCategories)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_PagingMode",
                    PropertyGridTypes.CreateEnum(settings, DefaultCardTypeExtensionSettings.PagingMode, CardControlHelper.PagingModeItems)),
                new PropertyGridItem(
                    "$UI_Cards_TypesEditor_PageLimit",
                    PropertyGridTypes.CreateInteger(settings, DefaultCardTypeExtensionSettings.PageLimit))
            );
        }

        #endregion
    }
}
