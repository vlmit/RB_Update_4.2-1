#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Cards.Forms;
using Tessa.UI.Cards.Tasks;
using Tessa.UI.Files;
using Tessa.UI.Menu;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.Files
{
    /// <summary>
    /// Расширение, добавляющее в меню файлового контрола пункт для восстановления файла из корзины.
    /// </summary>
    public sealed class RestoreFileControlExtension :
        FileControlExtension
    {
        #region Fields

        private readonly IUIHost uiHost;
        private readonly ISession session;
        private readonly IViewService viewService;
        private readonly ICardFileBackupSettings cardFileBackupSettings;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="RestoreFileControlExtension"/>.
        /// </summary>
        /// <param name="uiHost"><inheritdoc cref="IUIHost" path="/summary"/></param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="cardFileBackupSettings"><inheritdoc cref="ICardFileBackupSettings" path="/summary"/></param>
        public RestoreFileControlExtension(
            IUIHost uiHost,
            ISession session,
            IViewService viewService,
            ICardFileBackupSettings cardFileBackupSettings)
        {
            this.uiHost = NotNullOrThrow(uiHost);
            this.session = NotNullOrThrow(session);
            this.viewService = NotNullOrThrow(viewService);
            this.cardFileBackupSettings = NotNullOrThrow(cardFileBackupSettings);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task OpeningMenu(IFileControlExtensionContext context)
        {
            var cardModel = UIContext.Current.CardEditor?.CardModel;
            if (cardModel is null
                || cardModel.InSpecialMode()
                || IsCardTaskFileControl(cardModel, context.Control)
                || !await this.cardFileBackupSettings.CanDeleteWithBackupAsync(
                    context.ValidationResult,
                    context.CancellationToken)
                || context.Actions.FirstOrDefault(a => a.Name is FileMenuActionNames.Upload)
                    is not { IsCollapsed: false } uploadAction)
            {
                return;
            }

            var uploadActionIndex = context.Actions.IndexOf(uploadAction);

            context.Actions.Insert(
                uploadActionIndex + 1,
                new MenuAction(
                    FileMenuActionNames.Restore,
                    "$UI_Controls_FilesControl_RestoreFile",
                    context.Icons.Get("Thin119"),
                    new DelegateCommand(async _ => await this.RestoreActionAsync(context.CancellationToken)),
                    isCollapsed: !context.Control.Container.Permissions.CanAdd
                ));
        }

        #endregion

        #region Private Methods

        private static bool IsCardTaskFileControl(ICardModel cardModel, IFileControl fileControl) =>
            cardModel.MainForm is DefaultFormTabWithTasksViewModel mainForm
            && mainForm.Tasks.OfType<TaskViewModel>().Any(task =>
                task.TaskModel.ControlBag.OfType<FileListViewModel>().Any(file =>
                    file.FileControl == fileControl));

        private static bool CanRestoreAllDeletedFiles(IUser user, ICardModel cardModel) =>
            user.IsAdministrator()
            || cardModel.Card.TryGetInfo() is { } cardInfo
            && KrToken.TryGet(cardInfo) is { } krToken
            && krToken.HasPermission(KrPermissionFlagDescriptors.RestoreAllDeletedFiles);

        private async Task RestoreActionAsync(CancellationToken cancellationToken)
        {
            var uiContext = UIContext.Current;
            var cardEditor = uiContext.CardEditor;
            var cardModel = cardEditor.CardModel;
            if (cardModel is null)
            {
                return;
            }

            const string viewName = "DeletedFiles";
            List<RequestParameter> viewParameters = new();

            var view = await this.viewService.GetByNameAsync(viewName, cancellationToken)
                ?? throw new InvalidOperationException($"Can not find view with alias \"{viewName}\" at view service.");

            var (viewMetadata, validationResult) = await view.TryGetMetadataAsync(cancellationToken);
            if (viewMetadata is null || !validationResult.IsSuccessful)
            {
                throw new ValidationException(validationResult, new InvalidOperationException($"Can not find metadata for view \"{viewName}\"."));
            }

            if (CanRestoreAllDeletedFiles(this.session.User, cardModel))
            {
                viewParameters.Add(new("ShowAll") { IsTrueCriteriaOperator.Instance });
            }

            viewParameters.Add(new RequestParameter("Card").Add(EqualsToCriteriaOperator.Instance, cardModel.Card.ID));

            var selectedValue = await this.uiHost.ShowViewsDialogAsync(
                viewName,
                viewParameters,
                cancellationToken: cancellationToken);

            var selectedRow = selectedValue?.SelectedRow;
            if (selectedRow is null)
            {
                return;
            }

            var fileName = selectedRow.Get<string>("DeletedName")!;
            var confirmation = await LocalizeFormatAsync("$UI_Controls_FilesControl_RestoreSelectedFileMessage", fileName);
            if (!await TessaDialog.ConfirmAsync(confirmation))
            {
                return;
            }

            var deletedFile = new CardFileDeletedInfo
            {
                RowID = selectedRow.Get<Guid>("DeletedRowID"),
                Deleted = selectedRow.Get<DateTime>("DeletedDate"),
                DeletedByID = selectedRow.Get<Guid>("DeletedByID"),
                DeletedByName = selectedRow.Get<string>("DeletedByName")!
            };

            await cardEditor.SaveCardAsync(
                uiContext,
                request: new CardSavingRequest(
                    CardSavingMode.RefreshOnSuccess,
                    (card, _) =>
                    {
                        card.Info[CardHelper.FilesToRestoreKey] = new List<object> { deletedFile.ToSerializedDictionary() };
                        return ValueTask.CompletedTask;
                    }),
                cancellationToken: cancellationToken);
        }

        #endregion
    }
}
