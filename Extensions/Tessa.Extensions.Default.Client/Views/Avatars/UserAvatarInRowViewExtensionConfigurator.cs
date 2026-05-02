#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Content.Avatars;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls.AutoComplete;
using Tessa.UI.Controls.Helpers;
using Tessa.UI.Views.Extensions;

namespace Tessa.Extensions.Default.Client.Views.Avatars
{
    /// <summary>
    /// Конфигуратор расширения <see cref="UserAvatarInRowViewExtension"/>.
    /// </summary>
    /// <param name="createDialogFormFunc"><inheritdoc cref="CreateDialogFormFuncAsync" path="/summary"/></param>
    /// <param name="autoCompleteDialogProvider"><inheritdoc cref="AutoCompleteDialogProvider" path="/summary"/></param>
    public sealed class UserAvatarInRowViewExtensionConfigurator(
        CreateDialogFormFuncAsync createDialogFormFunc,
        AutoCompleteDialogProvider autoCompleteDialogProvider)
        : ExtensionSettingsConfiguratorBase(ViewExtensionConfiguratorType.Form, null, "$UserAvatarInRowViewExtension_Description")
    {
        #region Fields

        private readonly CreateDialogFormFuncAsync createDialogFormFunc = NotNullOrThrow(createDialogFormFunc);

        private readonly AutoCompleteDialogProvider autoCompleteDialogProvider = NotNullOrThrow(autoCompleteDialogProvider);

        private static readonly Dictionary<AvatarShape, string> avatarShapeLocalizations =
            new()
            {
                [AvatarShape.Circle] = "$UserAvatarInRowViewExtension_AvatarShape_Circle",
                [AvatarShape.Square] = "$UserAvatarInRowViewExtension_AvatarShape_Square"
            };

        private static readonly Dictionary<AvatarSize, string> avatarSizeLocalizations =
            new()
            {
                [AvatarSize.Small] = "$UserAvatarInRowViewExtension_AvatarSize_Small",
                [AvatarSize.Medium] = "$UserAvatarInRowViewExtension_AvatarSize_Medium",
                [AvatarSize.Large] = "$UserAvatarInRowViewExtension_AvatarSize_Large"
            };

        private static readonly Dictionary<AvatarContentKind, string> avatarContentKindLocalizations =
            new()
            {
                [AvatarContentKind.Avatar] = "$UserAvatarInRowViewExtension_AvatarContentKind_Avatar",
                [AvatarContentKind.Photo] = "$UserAvatarInRowViewExtension_AvatarContentKind_Photo",
            };


        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<(IFormViewModelBase?, Action?)> GetConfiguratorFormAsync(
            IExtensionConfigurationContext context,
            Action markedAsDirtyAction,
            CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings().FromSerializedDictionary<UserAvatarInRowViewExtensionSettings>() ?? new();

            var (form, cardModel) = await this.createDialogFormFunc(
                "ViewExtensions",
                "UserAvatarInRowViewExtension",
                cancellationToken: cancellationToken,
                modifyModelAsync: this.ModifyCardModelAsync);

            if (form is null || cardModel is null)
            {
                TessaDialog.ShowError("$CardTypes_MetadataEditor_ViewExtensionDialog_NotFound");
                return (null, null);
            }

            var section = cardModel.Card.Sections["UserAvatarInRowViewExtension"];
            section.Fields[nameof(UserAvatarInRowViewExtensionSettings.DestinationColumn)] = settings.DestinationColumn;
            section.Fields[nameof(UserAvatarInRowViewExtensionSettings.IDColumn)] = settings.IDColumn;
            section.Fields["AvatarSizeName"] = avatarSizeLocalizations[settings.AvatarSize];
            section.Fields["AvatarShapeName"] = avatarShapeLocalizations[settings.AvatarShape];
            section.Fields["AvatarContentKindName"] = avatarContentKindLocalizations[settings.AvatarContentKind];

            section.FieldChanged += (o, e) =>
            {
                markedAsDirtyAction();

                switch (e.FieldName)
                {
                    case nameof(UserAvatarInRowViewExtensionSettings.DestinationColumn):
                        settings.DestinationColumn = e.FieldValue?.ToString();
                        break;
                    case nameof(UserAvatarInRowViewExtensionSettings.IDColumn):
                        settings.IDColumn = e.FieldValue?.ToString();
                        break;
                    case "AvatarSizeName":
                        if (e.FieldValue is not null)
                        {
                            settings.AvatarSize = avatarSizeLocalizations.First(x => x.Value == e.FieldValue.ToString()).Key;
                        }

                        break;
                    case "AvatarShapeName":
                        if (e.FieldValue is not null)
                        {
                            settings.AvatarShape = avatarShapeLocalizations.First(x => x.Value == e.FieldValue.ToString()).Key;
                        }

                        break;
                    case "AvatarContentKindName":
                        if (e.FieldValue is not null)
                        {
                            settings.AvatarContentKind = avatarContentKindLocalizations.First(x => x.Value == e.FieldValue.ToString()).Key;
                        }

                        break;
                }
            };

            return (form, () => context.SaveSettings(settings.ToSerializedDictionary()));
        }

        /// <inheritdoc />
        public override void Initialize(IExtensionConfigurationContext context) =>
            context.SaveSettings(new UserAvatarInRowViewExtensionSettings().ToSerializedDictionary());

        #endregion

        #region Private methods

        private ValueTask ModifyCardModelAsync(ICardModel cardModel, CancellationToken cancellationToken = default)
        {
            cardModel.ControlInitializers.Add(
                (controlViewModel, cm, cr, ct) =>
                {
                    switch (controlViewModel)
                    {
                        case AutoCompleteEntryViewModel autoComplete:
                            switch (autoComplete.Name)
                            {
                                case "AvatarSize":
                                    this.ChangeAutocompleteSource(autoComplete, avatarSizeLocalizations);
                                    break;
                                case "AvatarShape":
                                    this.ChangeAutocompleteSource(autoComplete, avatarShapeLocalizations);
                                    break;
                                case "AvatarContentKind":
                                    this.ChangeAutocompleteSource(autoComplete, avatarContentKindLocalizations);
                                    break;
                            }

                            break;
                    }

                    return ValueTask.CompletedTask;
                });

            return ValueTask.CompletedTask;
        }

        private void ChangeAutocompleteSource<TKey>(AutoCompleteEntryViewModel autoComplete, IDictionary<TKey, string> localizations)
            where TKey : Enum
        {
            var avatarContentKindView = new NamedRecordsView<string>(
                "NamedValue",
                localizations.Values,
                x => [Guid.Empty, x],
                x => x);

            autoComplete.View = avatarContentKindView;
            autoComplete.ViewComboBox = avatarContentKindView;

            this.autoCompleteDialogProvider.ChangeAutoCompleteDialog(autoComplete);
        }

        #endregion
    }
}
