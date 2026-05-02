using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workplaces;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Workplaces
{
    /// <summary>
    /// Конфигуратор расширения <see cref="RefSectionExtension"/>.
    /// </summary>
    public sealed class RefSectionExtensionConfigurator(CreateDialogFormFuncAsync createDialogFormFunc)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$RefSectionExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<(IFormViewModelBase, Action)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action markedAsDirtyAction,
            CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings().FromSerializedDictionary<TreeItemFilteringSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "RefSectionExtension",
                cancellationToken: cancellationToken);

            if (form is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            var refSectionsSection = cardModel.Card.Sections["RefSections"];
            foreach (var refSection in settings.RefSections)
            {
                var emptyRow = cardModel.CreateEmptyRow("RefSections");
                emptyRow.RowID = Guid.NewGuid();
                emptyRow["Value"] = refSection;

                var newRow = refSectionsSection.Rows.Insert(refSectionsSection.Rows.Count);
                newRow.Set(emptyRow);
                newRow.State = CardRowState.Inserted;
            }

            var parametersSection = cardModel.Card.Sections["Parameters"];
            foreach (var parameter in settings.Parameters)
            {
                var emptyRow = cardModel.CreateEmptyRow("RefSections");
                emptyRow.RowID = Guid.NewGuid();
                emptyRow["Value"] = parameter;

                var newRow = parametersSection.Rows.Insert(parametersSection.Rows.Count);
                newRow.Set(emptyRow);
                newRow.State = CardRowState.Inserted;
            }

            var mainBlock = form switch
            {
                IFormWithBlocksViewModel formWithBlocks => formWithBlocks.Blocks.First(x => x.Name == "MainBlock"),
                IFormWithTabsViewModel formWithTabs => formWithTabs.Tabs.SelectMany(x => x.Blocks).First(x => x.Name == "MainBlock"),
                _ => throw new InvalidOperationException($"Unknown form type created fo form \"ViewExtensions\""),
            };

            var refSectionsGridViewModel = (GridViewModel) mainBlock.Controls.First(x => x.Name == "RefSections");
            refSectionsGridViewModel.RowInvoked += (o, e) => markedAsDirtyAction();
            refSectionsGridViewModel.RowValidating += RowValidating;

            var parametersGridViewModel = (GridViewModel) mainBlock.Controls.First(x => x.Name == "Parameters");
            parametersGridViewModel.RowInvoked += (o, e) => markedAsDirtyAction();
            parametersGridViewModel.RowValidating += RowValidating;

            return (form, () =>
            {
                settings.RefSections = refSectionsSection.Rows.Select(x => x["Value"]?.ToString()).ToList();
                settings.Parameters = parametersSection.Rows.Select(x => x["Value"]?.ToString()).ToList();
                context.SaveSettings(settings.ToSerializedDictionary());
            });

            void RowValidating(object sender, GridRowValidationEventArgs e)
            {
                var value = e.Row["Value"]?.ToString() ?? string.Empty;
                if (!Regex.IsMatch(value, "^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant))
                {
                    e.ValidationResult.AddError(this, "$WorkplaceFilteringExtension_InvalidName_ErrorMessage");
                }
            }
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new TreeItemFilteringSettings().ToSerializedDictionary());

        #endregion
    }
}
