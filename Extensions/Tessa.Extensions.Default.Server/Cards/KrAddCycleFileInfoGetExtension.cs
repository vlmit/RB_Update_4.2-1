#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Normalization;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Расширение на загрузку карточки, добавляющее в <see cref="CardInfoStorageObject.Info"/> информацию о распределении файлов по циклам согласования.
    /// </summary>
    /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
    /// <param name="cycleGroupingSettings"><inheritdoc cref="ICycleGroupingSettings" path="/summary"/></param>
    public sealed class KrAddCycleFileInfoGetExtension(
        IKrTypesCache krTypesCache,
        ICycleGroupingSettings cycleGroupingSettings,
        INormalizationBatchProcessor normalizationBatchProcessor) :
        CardGetExtension
    {
        #region Nested Types

        /// <summary>
        /// Объект идентифицирующий цикл согласования.
        /// </summary>
        /// <param name="HistoryGroupParentRowID">Идентификатор родительской группы истории заданий.</param>
        /// <param name="Cycle">Номер цикла согласования.</param>
        private readonly record struct CycleKey(
            Guid? HistoryGroupParentRowID,
            int Cycle)
        {
            #region Base Overrides

            /// <inheritdoc/>
            public override int GetHashCode() =>
                HashCode.Combine(
                    this.HistoryGroupParentRowID,
                    this.Cycle);

            #endregion

            #region IEquatable<CycleKey> Members

            /// <inheritdoc/>
            public bool Equals(CycleKey other) =>
                this.HistoryGroupParentRowID == other.HistoryGroupParentRowID
                && this.Cycle == other.Cycle;

            #endregion
        }

        /// <summary>
        /// Информация о цикле согласования.
        /// </summary>
        [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
        private sealed class CycleInfo
        {
            #region Properties

            /// <summary>
            /// Дата и время начала цикла согласования.
            /// </summary>
            public DateTime Start { get; set; }

            /// <summary>
            /// Дата и время окончания цикла согласования.
            /// </summary>
            public DateTime End { get; set; }

            /// <summary>
            /// Номер цикла.
            /// </summary>
            public int Cycle { get; init; }

            #endregion

            #region Public Methods

            /// <summary>
            /// Проверяет, что заданное значение входит в диапазон [<see cref="Start"/>; <see cref="End"/>).
            /// </summary>
            /// <param name="value">Проверяемое значение.</param>
            /// <returns>Значение <see langword="true"/>, если проверяемое значение входит в цикл, иначе - <see langword="false"/>.</returns>
            public bool InRange(DateTime? value) =>
                this.Start <= value && value < this.End;

            #endregion

            #region Private Methods

            private string GetDebuggerDisplay() =>
                $"{DebugHelper.GetTypeName(this)}"
                + $": {this.Cycle}: [{this.Start}; {this.End})";

            #endregion
        }

        #endregion

        #region Fields

        private readonly IKrTypesCache krTypesCache = NotNullOrThrow(krTypesCache);
        private readonly ICycleGroupingSettings cycleGroupingSettings = NotNullOrThrow(cycleGroupingSettings);
        private readonly INormalizationBatchProcessor normalizationBatchProcessor = NotNullOrThrow(normalizationBatchProcessor);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(
            ICardGetExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || context.Request.ServiceType == CardServiceType.Default
                || !context.ValidationResult.IsSuccessful()
                || context.CardType!.Flags.HasNot(CardTypeFlags.AllowFiles)
                || context.Request.RestrictionFlags.Has(CardGetRestrictionFlags.RestrictFiles)
                || context.Request.RestrictionFlags.Has(CardGetRestrictionFlags.RestrictTaskHistory)
                || context.Response!.TryGetCard() is not { } card
                || card.TryGetFiles() is not { Count: > 0 } files
                || card.TryGetTaskHistoryGroups() is not { Count: > 0 } historyGroups
                || card.TryGetTaskHistory() is not { Count: > 0 } historyItems
                || !(await KrComponentsHelper.GetKrComponentsAsync(
                    card,
                    this.krTypesCache,
                    context.CancellationToken)).Has(KrComponents.Routes))
            {
                return;
            }

            if (await this.GetCyclesAsync(
                    historyGroups,
                    historyItems,
                    context.CancellationToken) is not { Count: > 0 } cycles)
            {
                return;
            }

            card.Info[CycleGroupingHelper.MaxCycleNumberKey] = Int32Boxes.Box(cycles.Count);

            List<CardFile>? copiesFiles = null;
            List<Guid>? originalFiles = null;

            foreach (var file in files)
            {
                if (file.OriginalFileID.HasValue)
                {
                    copiesFiles ??= [];
                    copiesFiles.Add(file);
                }
                else
                {
                    originalFiles ??= [];
                    originalFiles.Add(file.RowID);
                }
            }

            if (copiesFiles is not null)
            {
                AddFilesByCycles(
                    card,
                    copiesFiles,
                    cycles);
            }

            if (originalFiles is not null)
            {
                await this.AddFilesModifiedByCyclesAsync(
                    context.DbScope!,
                    card,
                    originalFiles,
                    cycles,
                    context.CardMetadata,
                    context.ValidationResult,
                    context.CancellationToken);
            }
        }

        #endregion

        #region Private Methods

        private static void AddFilesByCycles(
            Card card,
            IEnumerable<CardFile> copiesFiles,
            IEnumerable<CycleInfo> cycles)
        {
            var filesList = new Dictionary<string, object>();

            foreach (var file in copiesFiles)
            {
                foreach (var cycle in cycles)
                {
                    if (cycle.InRange(file.Card.Created))
                    {
                        filesList.Add(
                            file.RowID.ToString(),
                            Int32Boxes.Box(cycle.Cycle));
                    }
                }
            }

            card.Info[CycleGroupingHelper.FilesByCyclesKey] = filesList;
        }

        private async ValueTask AddFilesModifiedByCyclesAsync(
            IDbScope dbScope,
            Card card,
            IEnumerable<Guid> originalFiles,
            IReadOnlyCollection<CycleInfo> cycles,
            ICardMetadata cardMetadata,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var cycleGroupingFileInfoListByCycle = await GetCycleGroupingFileInfoListByCycleAsync(
                dbScope,
                cycles,
                originalFiles,
                cancellationToken);

            if (cycleGroupingFileInfoListByCycle.Count > 0)
            {
                var batchRequest = await this.normalizationBatchProcessor.CreateRequestAsync(cancellationToken);
                var usersSourceID = (await cardMetadata.GetNormalizationInfoAsync(cancellationToken)).UsersSourceID;
                foreach (var groupingInfo in cycleGroupingFileInfoListByCycle.SelectMany(x => x.Value))
                {
                    batchRequest.Add(usersSourceID, new(groupingInfo.VersionCreatedByID), groupingInfo, static (key, value, groupingInfo, _) =>
                        groupingInfo.VersionCreatedByName = value ?? NormalizationHelper.UnknownValueForPlatformSources);
                }

                var batchResponse = await this.normalizationBatchProcessor.ProcessAsync(batchRequest, cancellationToken);
                validationResult.Add(batchResponse.Result.ConvertToSuccessful());
            }

            var filesModifiedByCycles = cycleGroupingFileInfoListByCycle
                .SelectMany(static p =>
                {
                    // Группировка версий файлов по файлами.
                    var groupByFile = p.Value.GroupBy(static q => q.FileID);

                    return groupByFile.SelectMany(static q =>
                    {
                        // Группировка файлов по автору.
                        var groupByCreatedBy = q.GroupBy(static e => e.VersionCreatedByID);

                        return groupByCreatedBy.SelectMany(static e =>
                        {
                            var maxAuthorVersion = e.Max(static t => t.VersionNumber);
                            return e.Where(t => t.VersionNumber == maxAuthorVersion);
                        });
                    });
                })
                .Select(static x => x.ToSerializedDictionary())
                .ToArray();

            card.Info[CycleGroupingHelper.FilesModifiedByCyclesKey] = filesModifiedByCycles;
        }

        private async Task<IReadOnlyList<CycleInfo>> GetCyclesAsync(
            IReadOnlyList<CardTaskHistoryGroup> historyGroups,
            IReadOnlyList<CardTaskHistoryItem> historyItems,
            CancellationToken cancellationToken)
        {
            // 1. Поиск групп, которые учитываются при определении распределения файлов по циклам согласования.
            var taskGroupTypeIDList = await this.cycleGroupingSettings.GetCycleTaskGroupTypeIDListAsync(cancellationToken);

            if (taskGroupTypeIDList.Count == 0)
            {
                return [];
            }

            var taskHistoryGroupsFiltered = historyGroups
                .Where(i => i.TypeID.HasValue
                    && taskGroupTypeIDList.Contains(i.TypeID.Value))
                .ToDictionary(static i => i.RowID);

            if (taskHistoryGroupsFiltered.Count == 0)
            {
                return [];
            }

            // 2. Подготовка данных.
            // При загрузке история заданий сортируется по возрастанию CardTaskHistoryItem.Created. См. CardGetStrategy.LoadTaskHistoryAsync.
            // Если это изменится, то необходимо будет добавить сортировку.

            // 3. Предварительная фильтрация записей истории заданий по типу заданий.
            var ignoreTaskTypeIDList = await this.cycleGroupingSettings.GetIgnoreTaskTypeIDListAsync(cancellationToken);

            var preFilteredHistoryItems = ignoreTaskTypeIDList.Count == 0
                ? historyItems
                : historyItems
                    .Where(i => !ignoreTaskTypeIDList.Contains(i.TypeID))
                    .ToArray();

            if (preFilteredHistoryItems.Count == 0)
            {
                return [];
            }

            // 4. Поиск записей в истории заданий и определение параметров циклов.
            var historyItemsByCycles = new Dictionary<CycleKey, (HashSet<Guid, CardTaskHistoryItem> Items, CycleInfo CycleInfo)>();
            var utcNow = DateTime.UtcNow;

            foreach (var historyItem in preFilteredHistoryItems)
            {
                if (!historyItem.GroupRowID.HasValue)
                {
                    continue;
                }

                if (taskHistoryGroupsFiltered.TryGetValue(historyItem.GroupRowID.Value, out var historyGroup))
                {
                    var key = new CycleKey(
                        historyGroup.ParentRowID,
                        historyGroup.Iteration);

                    if (!historyItemsByCycles.TryGetValue(key, out var cardTaskHistoryItems))
                    {
                        cardTaskHistoryItems = (
                            new HashSet<Guid, CardTaskHistoryItem>(static i => i.RowID),
                            new CycleInfo()
                            {
                                Start = historyItem.Created,
                                End = historyItem.Completed ?? utcNow,
                                Cycle = historyItemsByCycles.Count + 1,
                            });
                        historyItemsByCycles.Add(key, cardTaskHistoryItems);
                    }
                    else
                    {
                        var historyItemCompleted = historyItem.Completed ?? utcNow;
                        if (cardTaskHistoryItems.CycleInfo.End < historyItemCompleted)
                        {
                            cardTaskHistoryItems.CycleInfo.End = historyItemCompleted;
                        }
                    }

                    cardTaskHistoryItems.Items.Add(historyItem);
                }
            }

            if (historyItemsByCycles.Count == 0)
            {
                return [];
            }

            foreach (var (key, cycleData) in historyItemsByCycles)
            {
                int oldGroupItemsCount;

                do
                {
                    oldGroupItemsCount = cycleData.Items.Count;

                    foreach (var historyItem in preFilteredHistoryItems)
                    {
                        if (historyItem.ParentRowID.HasValue
                            && cycleData.Items.ContainsKey(historyItem.ParentRowID.Value))
                        {
                            cycleData.Items.Add(historyItem);

                            var historyItemCompleted = historyItem.Completed ?? utcNow;
                            if (cycleData.CycleInfo.End < historyItemCompleted)
                            {
                                cycleData.CycleInfo.End = historyItemCompleted;
                            }
                        }
                    }
                } while (oldGroupItemsCount < cycleData.Items.Count);
            }

            // 5. Проверка границ циклов.
            if (historyItemsByCycles.Count > 1)
            {
                using var en = historyItemsByCycles.GetEnumerator();

                // Значение гарантированно есть.
                en.MoveNext();

                var oldCycleInfo = en.Current.Value.CycleInfo;

                while (en.MoveNext())
                {
                    var currentCycleInfo = en.Current.Value.CycleInfo;
                    var currentCycleStart = currentCycleInfo.Start;

                    if (currentCycleStart < oldCycleInfo.End)
                    {
                        oldCycleInfo.End = currentCycleStart;
                    }

                    oldCycleInfo = currentCycleInfo;
                }
            }

            return historyItemsByCycles
                .Select(static i => i.Value.CycleInfo)
                .ToArray();
        }

        private static async Task<Dictionary<int, List<CycleGroupingFileInfo>>> GetCycleGroupingFileInfoListByCycleAsync(
            IDbScope dbScope,
            IReadOnlyCollection<CycleInfo> cycles,
            IEnumerable<Guid> files,
            CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();

            var db = dbScope.Db;
            db
                .SetCommand(dbScope.BuilderFactory
                        .Select()
                        .C("Created")
                        .C("ID")
                        .C("SourceID")
                        .C("Size")
                        .C("CreatedByID")
                        .C("RowID")
                        .C("Number")
                        .From(Names.FileVersions).NoLock()
                        .Where()
                        .C("ID").InArray(files, "Files", out var dpFiles)
                        .Build(),
                    DataParameters.Get(dpFiles))
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);

            var result = new Dictionary<int, List<CycleGroupingFileInfo>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var versionCreated = reader.GetDateTimeUtc(0);

                if (!cycles.TryFirst(i => i.InRange(versionCreated), out var cycleInfo))
                {
                    continue;
                }

                var fileRowID = reader.GetGuid(1);
                var versionSourceID = (CardFileSourceType) reader.GetInt16(2);
                var versionSize = reader.GetInt64(3);
                var versionCreatedByID = reader.GetGuid(4);
                var versionRowID = reader.GetGuid(5);
                var versionNumber = reader.GetInt32(6);

                if (!result.TryGetValue(cycleInfo.Cycle, out var cycleGroupingFileInfoList))
                {
                    cycleGroupingFileInfoList = [];
                    result[cycleInfo.Cycle] = cycleGroupingFileInfoList;
                }

                cycleGroupingFileInfoList.Add(new CycleGroupingFileInfo
                {
                    Cycle = cycleInfo.Cycle,
                    FileID = fileRowID,
                    VersionID = versionRowID,
                    VersionNumber = versionNumber,
                    VersionSize = versionSize,
                    VersionSource = versionSourceID,
                    VersionCreated = versionCreated,
                    VersionCreatedByID = versionCreatedByID
                });
            }

            return result;
        }

        #endregion
    }
}
