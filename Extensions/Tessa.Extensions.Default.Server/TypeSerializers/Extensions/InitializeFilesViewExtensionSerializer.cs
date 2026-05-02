#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.TypeSerializers;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform.Storage;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.TypeSerializers.Extensions
{
    /// <summary>
    /// Сериализатор расширения типа <see cref="DefaultCardTypeExtensionTypes.InitializeFilesView"/>.
    /// </summary>
    public sealed class InitializeFilesViewExtensionSerializer : ITypeComponentSerializer<CardTypeExtension>
    {
        #region ITypeComponentSerializer<CardTypeExtension> Members

        /// <inheritdoc/>
        public ValueTask NotifyOnSerializingAsync(
            Dictionary<string, object?> settings,
            CardTypeExtension component,
            ICardSerializableContext context,
            CancellationToken cancellationToken = default)
        {
            settings.RemoveIfEmptyString(DefaultCardTypeExtensionSettings.FilesViewAlias);
            settings.RemoveIfDefault(DefaultCardTypeExtensionSettings.CategoriesViewAlias, CardControlSettings.FileCategoriesFilteredViewAlias);
            settings.RemoveIfEmptyString(DefaultCardTypeExtensionSettings.PreviewControlName);
            settings.RemoveIfEmptyString(DefaultCardTypeExtensionSettings.DefaultGroup);
            settings.RemoveIfDefault(DefaultCardTypeExtensionSettings.IsCategoriesEnabled, false);
            settings.RemoveIfDefault(DefaultCardTypeExtensionSettings.IsManualCategoriesCreationDisabled, false);
            settings.RemoveIfDefault(DefaultCardTypeExtensionSettings.IsNullCategoryCreationDisabled, false);
            settings.RemoveIfDefault(DefaultCardTypeExtensionSettings.IsIgnoreExistingCategories, false);
            // TODO remove View Mapping default keys from a collection, see Tessa.UI.Controls.ViewMapper
            settings.RemoveIfEmptyCollection(DefaultCardTypeExtensionSettings.CategoriesViewMapping);
            settings.RemoveIfDefaultEnum(DefaultCardTypeExtensionSettings.PagingMode, Paging.No);
            settings.RemoveIfDefault(DefaultCardTypeExtensionSettings.PageLimit, DefaultTypeExtensionTypeHelper.DefaultPageLimit);
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
