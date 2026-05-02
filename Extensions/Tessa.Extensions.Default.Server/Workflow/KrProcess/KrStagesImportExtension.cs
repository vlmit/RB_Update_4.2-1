#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Расширение, выполняющее форматирование параметров этапов при импорте карточки.
    /// </summary>
    /// <param name="formatterContainer"><inheritdoc cref="IStageTypeFormatterContainer" path="/summary"/></param>
    public sealed class KrStagesImportExtension(
        IStageTypeFormatterContainer formatterContainer) :
        CardStoreExtension
    {
        #region Fields

        private readonly IStageTypeFormatterContainer formatterContainer = NotNullOrThrow(formatterContainer);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(
            ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.TryGetCard() is not { } card
                || card.TryGetSections() is not { } sections
                || !sections.TryGetValue(KrConstants.KrStages.Name, out var section)
                || section.TryGetRows() is not { Count: > 0 } rows)
            {
                return;
            }

            var stageInfoByRowID = new Dictionary<Guid, (Guid StageTypeID, string Settings)>();
            if (card.StoreMode == CardStoreMode.Update
                && rows.Any(x => x.State == CardRowState.Modified))
            {
                var db = context.DbScope!.Db;
                var dbms = db.Dbms;
                var queryBuilder = context.DbScope.BuilderFactory;

                await using var reader = await db
                    .SetCommand(
                        queryBuilder
                            .Select()
                                .C("RowID")
                                .C(KrConstants.KrStages.StageTypeID)
                                .C(KrConstants.KrStages.Settings)
                            .From(nameof(KrConstants.KrStages)).NoLock()
                            .Where().C("ID").Equals().P("ID")
                            .Build(),
                        db.Parameter("ID", card.ID, DataType.Guid))
                    .LogCommand()
                    .ExecuteReaderAsync(
                        CommandBehavior.SequentialAccess,
                        context.CancellationToken);

                while (await reader.ReadAsync(context.CancellationToken))
                {
                    var rowID = reader.GetGuid(0);
                    var stageTypeID = reader.GetNullableGuid(1);
                    var settings = await reader.GetSequentialNullableStringAsync(2, dbms, context.CancellationToken);

                    if (stageTypeID.HasValue && !string.IsNullOrEmpty(settings))
                    {
                        stageInfoByRowID[rowID] = (stageTypeID.Value, settings);
                    }
                }
            }

            foreach (var row in rows)
            {
                if (card.StoreMode != CardStoreMode.Insert
                    && row.State is not CardRowState.Inserted and not CardRowState.Modified)
                {
                    continue;
                }

                if (row.State == CardRowState.None)
                {
                    row.State = CardRowState.Inserted;
                }

                var stageTypeID = row.TryGet<Guid?>(KrConstants.KrStages.StageTypeID);
                if (!stageTypeID.HasValue
                    && stageInfoByRowID.TryGetValue(row.RowID, out var info))
                {
                    stageTypeID = info.StageTypeID;
                }

                if (!stageTypeID.HasValue)
                {
                    continue;
                }

                var json = row.TryGet<string>(KrConstants.KrStages.Settings);
                if (json is null
                    && stageInfoByRowID.TryGetValue(row.RowID, out info))
                {
                    json = info.Settings;
                }

                Dictionary<string, object?>? settings = null;
                if (!string.IsNullOrEmpty(json))
                {
                    settings = StorageHelper.DeserializeFromTypedJson(json);
                }

                await this.FormatRowAsync(
                    context.Session,
                    row,
                    card,
                    stageTypeID.Value,
                    settings ?? [],
                    context.CancellationToken);
            }
        }

        #endregion

        #region Private Methods

        private async Task FormatRowAsync(
            ISession session,
            CardRow innerRow,
            Card innerCard,
            Guid stageTypeID,
            IDictionary<string, object?> settings,
            CancellationToken cancellationToken = default)
        {
            var formatter = this.formatterContainer.ResolveFormatter(stageTypeID);
            if (formatter is null)
            {
                innerRow.Fields[KrConstants.KrStages.DisplaySettings] = string.Empty;
                return;
            }

            var info = new Dictionary<string, object?>();
            var ctx = new StageTypeFormatterContext(
                session,
                info,
                innerCard,
                innerRow,
                settings,
                cancellationToken)
            {
                DisplayTimeLimit = innerRow.TryGet<string>(KrConstants.KrStages.DisplayTimeLimit) ?? string.Empty,
                DisplayParticipants = innerRow.TryGet<string>(KrConstants.KrStages.DisplayParticipants) ?? string.Empty,
                DisplaySettings = innerRow.TryGet<string>(KrConstants.KrStages.DisplaySettings) ?? string.Empty
            };

            await formatter.FormatServerAsync(ctx);

            innerRow.Fields[KrConstants.KrStages.DisplaySettings] = ctx.DisplaySettings;
        }

        #endregion
    }
}
