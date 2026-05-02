using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.Extensions.Platform.Client.Tiles;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Forms;
using Tessa.UI.Files;
using Tessa.UI.Notifications;
using Tessa.UI.Tiles;

namespace Tessa.Extensions.Default.Client.AbTest
{
    public static class AbExternalSystemHelper
    {
        public static async Task RequestAndAddFileAsync(
            IAdvancedCardDialogManager cardDialogManager,
            ICardRepository cardRepository,
            CreateDialogFormFuncAsync createDialogFormFuncAsync,
            INotificationUIManager notificationUIManager)
        {
            ICardEditorModel mainEditor = UIContext.Current.CardEditor;
            if (mainEditor is null)
            {
                return;
            }

            ICardModel mainModel = mainEditor.CardModel;
            if (mainModel is null)
            {
                return;
            }

            (_, ICardModel model) = await createDialogFormFuncAsync("AbCarExternalSystemRequest", formCreationOptions: FormCreationOptions.AlwaysCreateTabbedForm);

            await cardDialogManager.ShowCardAsync(
                model,
                prepareEditorActionAsync: (editor, ct) =>
                {
                    editor.CardModel.FileContainer.ContainerFileAdding += async (s, e) =>
                    {
                        var maxFileSize = 20; // Max file size for 1C test dialog.
                        if (e.File.Size / 1024 > maxFileSize)
                        {
                            var message = await LocalizeFormatAsync("$AbTest_MaxFileSizeExceeded", maxFileSize);
                            await TessaDialog.ShowMessageAsync(message);
                            e.Cancel = true;
                        }
                    };

                    editor.StatusBarIsVisible = false;

                    editor.Toolbar.Actions.AddRange(
                        new CardToolbarAction(
                            "Ok",
                            "$UI_Common_OK",
                            editor.Toolbar.CreateIcon("Int426"),
                            new DelegateCommand(
                                async _ =>
                                {
                                    var request = new CardRequest { RequestType = AbRequestTypes.GetExternalSystemData };
                                    request.DynamicInfo.Name = model.Card.DynamicEntries.MainInfo.Name;
                                    request.DynamicInfo.Driver = model.Card.DynamicEntries.MainInfo.DriverName;
                                    var filesToUpload = new List<object>();
                                    foreach (var fileAttached in editor.CardModel.FileContainer.Files)
                                    {
                                        await using var fileContentStream = await fileAttached.Versions.Last.File.Content.GetAsync(CancellationToken.None);
                                        var fileContent = await fileContentStream.ReadAllBytesAsync(CancellationToken.None);
                                        var contentBase64 = Convert.ToBase64String(fileContent);
                                        filesToUpload.Add(new Dictionary<string, object>
                                        {
                                            ["name"] = fileAttached.Name,
                                            ["content"] = contentBase64
                                        });
                                    }

                                    request.DynamicInfo.Files = filesToUpload;

                                    // показываем сплэш и задаём блокирующую операцию для карточки:
                                    // пользователь не сможет закрыть вкладку или отрефрешить карточку, пока она не закончится

                                    CardResponse response;
                                    using (TessaSplash.Create("$AbTest_Splash_LoadingFromExternalSystem"))
                                    using (editor.SetOperationInProgress(blocking: true))
                                    {
                                        await Task.Delay(2000, ct); // для примера добавим задержки
                                        response = await cardRepository.RequestAsync(request, ct);
                                    }

                                    ValidationResult result = response.ValidationResult.Build();
                                    await TessaDialog.ShowNotEmptyAsync(result);

                                    if (!result.IsSuccessful)
                                    {
                                        return;
                                    }

                                    string content = response.Info.Get<string>("Xml");

                                    const string fileName = "response.xml";

                                    IFile file = mainModel.FileContainer.Files.FirstOrDefault(x => x.Name == fileName);
                                    if (file != null)
                                    {
                                        await mainModel.FileControlManager.ResetIfInPreviewAsync(file, cancellationToken: CancellationToken.None);
                                        await file.ReplaceTextAsync(content, cancellationToken: CancellationToken.None);
                                    }
                                    else
                                    {
                                        await mainModel.FileContainer
                                            .BuildFile(fileName)
                                            .SetContentText(content, isLocal: true)
                                            .AddWithNotificationAsync(cancellationToken: CancellationToken.None);
                                    }

                                    await notificationUIManager.ShowTextOrMessageBoxAsync("$AbTest_LoadedFromExternalSystem");
                                    await editor.CloseAsync(cancellationToken: CancellationToken.None);
                                }),
                            tooltip: TileHelper.GetToolTip("$KrTiles_SaveAndClose_Tooltip",
                                TileKeys.SaveAndCloseCard),
                            order: -2,
                            gestures: [TileKeys.SaveAndCloseCard]),
                        new CardToolbarAction(
                            TileNames.Cancel,
                            "$UI_Common_Cancel",
                            editor.Toolbar.CreateIcon("Int626"),
                            new DelegateCommand(async _ => await editor.CloseAsync(cancellationToken: CancellationToken.None)),
                            tooltip: "$UI_Common_Cancel",
                            order: 40)
                    );

                    editor.Context.SetDialogClosingAction((dialogContext, args) => TaskBoxes.False);
                    return new(true);
                },
                options: new ShowCardOptions
                {
                    DisplayValue = "$AbTest_CardTypes_Tabs_ExternalSystemRequest",
                    WithTabControlBackground = true
                }
            );
        }
    }
}
