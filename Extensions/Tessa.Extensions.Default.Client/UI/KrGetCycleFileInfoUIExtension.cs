#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI.Cards;

namespace Tessa.Extensions.Default.Client.UI
{
    public sealed class KrGetCycleFileInfoUIExtension :
        CardUIExtension
    {
        #region Constructors

        public KrGetCycleFileInfoUIExtension(CreateFileSourceForCardFuncAsync createFileSourceForCardFuncAsync) =>
            this.createFileSourceForCardFuncAsync = NotNullOrThrow(createFileSourceForCardFuncAsync);

        #endregion

        #region Fields

        private readonly CreateFileSourceForCardFuncAsync createFileSourceForCardFuncAsync;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task Initializing(ICardUIExtensionContext context)
        {
            if (context.Model.InSpecialMode()
                || context.Card.TryGetInfo() is not { } cardInfo
                || !cardInfo.TryGetValue(CycleGroupingHelper.MaxCycleNumberKey, out var maxCycleNumberObj)
                || maxCycleNumberObj is not int maxCycleNumber)
            {
                return;
            }

            if (cardInfo.TryGetValue(CycleGroupingHelper.FilesByCyclesKey, out var filesByCyclesObj)
                && filesByCyclesObj is Dictionary<string, object> filesByCycles)
            {
                foreach ((var fileIDObj, var cycleIDObj) in filesByCycles)
                {
                    if (context.FileContainer.Files.TryGet(Guid.Parse(fileIDObj)) is { } fileModel)
                    {
                        var cycleID = (int) cycleIDObj;

                        fileModel.Info[CycleGroupingHelper.CycleIDKey] = cycleIDObj;
                        fileModel.Info[CycleGroupingHelper.CycleOrderKey] = Int32Boxes.Box(maxCycleNumber - cycleID);
                        fileModel.Info[CycleGroupingHelper.MaxCycleNumberKey] = Int32Boxes.Box(maxCycleNumber);
                    }
                }
            }

            if (cardInfo.TryGetValue(CycleGroupingHelper.FilesModifiedByCyclesKey, out var filesModifiedByCyclesObj)
                && filesModifiedByCyclesObj is IList { Count: > 0 } filesModifiedByCyclesStorage)
            {
                // clone - пустая карточка с той же информацией по типу и по версии, и др. системной информацией, но без фактических данных;
                // в неё будут добавляться виртуальные файлы, чтобы незахламлять структуру основной карточки

                var clone = context.Card.Clone();
                clone.Sections.Clear();
                clone.Files.Clear();
                clone.Tasks.Clear();
                clone.TaskHistory.Clear();
                clone.TaskHistoryGroups.Clear();
                clone.Info.Clear();

                var cloneFileSource = await this.createFileSourceForCardFuncAsync(clone, context.CancellationToken);

                foreach (Dictionary<string, object?> versionInfoStorage in filesModifiedByCyclesStorage)
                {
                    var cycleGroupingFileInfo = versionInfoStorage.FromSerializedDictionary<CycleGroupingFileInfo>();

                    if (context.FileContainer.Files.FirstOrDefault(p => p.ID == cycleGroupingFileInfo.FileID) is not { } originalFile)
                    {
                        continue;
                    }

                    if (context.Card.Files.FirstOrDefault(p => p.RowID == cycleGroupingFileInfo.FileID) is not { TypeName: not null } originalCardFile)
                    {
                        continue;
                    }

                    // теперь создаём файл и наполняем его
                    var virtualFile =
                        await context.FileContainer.AddVirtualAsync(
                            cloneFileSource,
                            new VirtualFile(
                                originalCardFile.TypeName,
                                originalCardFile.Name,
                                (token, _) =>
                                {
                                    token.Size = cycleGroupingFileInfo.VersionSize;
                                    token.LastVersionTags.AddRange(originalFile.Versions.Last.Tags);
                                    return ValueTask.CompletedTask;
                                }),
                            cancellationToken: context.CancellationToken,
                            versions:
                            [
                                new VirtualFileVersion(
                                    originalCardFile.Name,
                                    (token, _) =>
                                    {
                                        token.Size = cycleGroupingFileInfo.VersionSize;
                                        token.Number = cycleGroupingFileInfo.VersionNumber;
                                        token.Created = cycleGroupingFileInfo.VersionCreated;
                                        token.CreatedByID = cycleGroupingFileInfo.VersionCreatedByID;
                                        token.CreatedByName = cycleGroupingFileInfo.VersionCreatedByName;
                                        token.Tags.AddRange(originalFile.Versions.Last.Tags);
                                        return ValueTask.CompletedTask;
                                    })
                            ]);

                    var virtualCardFile = clone.Files.First(p => p.Card.ID == virtualFile.ID);
                    virtualCardFile.Card.CreatedByID = cycleGroupingFileInfo.VersionCreatedByID;
                    virtualCardFile.Card.CreatedByName = cycleGroupingFileInfo.VersionCreatedByName ?? string.Empty;
                    virtualCardFile.Card.Created = cycleGroupingFileInfo.VersionCreated;
                    virtualCardFile.LastVersion!.Number = cycleGroupingFileInfo.VersionNumber;
                    virtualCardFile.LastVersion.Created = cycleGroupingFileInfo.VersionCreated;
                    virtualCardFile.LastVersion.CreatedByID = cycleGroupingFileInfo.VersionCreatedByID;
                    virtualCardFile.LastVersion.CreatedByName = cycleGroupingFileInfo.VersionCreatedByName;

                    virtualCardFile.ExternalSource = new CardFileContentSource
                    {
                        CardID = context.Card.ID,
                        CardTypeID = context.Model.CardType.ID,
                        FileID = cycleGroupingFileInfo.FileID,
                        Source = cycleGroupingFileInfo.VersionSource,
                        VersionRowID = cycleGroupingFileInfo.VersionID,
                    };

                    virtualFile.Info[CycleGroupingHelper.CreatedKey] = cycleGroupingFileInfo.VersionCreated;
                    virtualFile.Info[CycleGroupingHelper.CreatedByNameKey] = cycleGroupingFileInfo.VersionCreatedByName;
                    virtualFile.Info[CycleGroupingHelper.CycleIDKey] = Int32Boxes.Box(cycleGroupingFileInfo.Cycle);
                    virtualFile.Info[CycleGroupingHelper.CycleOrderKey] = Int32Boxes.Box(maxCycleNumber - cycleGroupingFileInfo.Cycle);
                    virtualFile.Info[CycleGroupingHelper.MaxCycleNumberKey] = maxCycleNumberObj;
                }
            }
        }

        #endregion
    }
}
