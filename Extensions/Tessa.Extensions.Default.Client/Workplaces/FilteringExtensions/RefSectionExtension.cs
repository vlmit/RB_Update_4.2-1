#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workplaces;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Views;
using Tessa.UI.Views.Extensions;
using Tessa.UI.Views.Workplaces.Tree;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Workplaces;

namespace Tessa.Extensions.Default.Client.Workplaces
{
    /// <summary>
    /// Расширение на узел рабочего места, которое задаёт список значений RefSection и параметров представлений,
    /// которые используются для узла при его скрытии или отображении в режиме отбора (по троеточию из ссылочных контролов),
    /// независимо от представлений в узле.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="RefSectionExtensionConfigurator"/>.
    /// </remarks>
    public sealed class RefSectionExtension :
        ViewModel<EmptyModel>,
        ITreeItemExtension,
        IWorkplaceExtensionSettingsRestore,
        IWorkplaceFilteringRule
    {
        #region Fields

        private TreeItemFilteringSettings settings = new();

        #endregion

        #region Private Methods

        private static bool ParametersEquals(IEnumerable<RequestParameter>? contextParameters, List<string> settingsParameters) =>
            contextParameters.AsReadOnlyCollection() is not { Count: > 0 } parameters
            || parameters.All(x => ParserNames.Contains(x.Name, settingsParameters));

        private static bool ConditionEquals(IWorkplaceFilteringContext context, ITreeItemFilteringSettings settings) =>
            ParserNames.HasIntersections(settings.RefSections, context.RefSection)
            && ParametersEquals(context.Parameters, settings.Parameters);

        #endregion

        #region ITreeItemExtension Members

        /// <inheritdoc />
        public void Clone(ITreeItem source, ITreeItem cloned, ICloneableContext context)
        {
        }

        /// <inheritdoc />
        public void Initialize(ITreeItem model)
        {
        }

        /// <inheritdoc />
        public void Initialized(ITreeItem model)
        {
        }

        #endregion

        #region IWorkplaceFilteringRule Members

        /// <inheritdoc />
        public ValueTask<CheckingResult> EvaluateAsync(IWorkplaceComponentMetadata metadata, IWorkplaceFilteringContext context) =>
            new(ConditionEquals(context, this.settings) ? CheckingResult.Positive : CheckingResult.Negative);

        #endregion

        #region IWorkplaceExtensionSettingsRestore Members

        /// <inheritdoc />
        public void Restore(Dictionary<string, object?> metadata) =>
            this.settings = metadata.FromSerializedDictionary<TreeItemFilteringSettings>();

        #endregion
    }
}
