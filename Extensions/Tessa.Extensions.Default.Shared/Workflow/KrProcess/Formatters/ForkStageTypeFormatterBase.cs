#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tessa.Cards;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Базовый абстрактный класс форматтера этапов типов "Ветвление".
    /// </summary>
    public abstract class ForkStageTypeFormatterBase :
        StageTypeFormatterBase
    {
        #region Protected Methods

        /// <summary>
        /// Добавляет форматированную строку с описанием вторичных процессов этапа в <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">Конструктор строки.</param>
        /// <param name="context"><inheritdoc cref="IStageTypeFormatterContext" path="/summary"/></param>
        /// <param name="settingsRows">Параметры этапа.</param>
        /// <param name="isClient">Признак формирования строки на клиенте.</param>
        protected static void AppendSecondaryProcessesNames(
            StringBuilder builder,
            IStageTypeFormatterContext context,
            IList? settingsRows,
            bool isClient)
        {
            if (!(settingsRows?.Count > 0))
            {
                return;
            }

            var stageRowID = context.StageRow.RowID;

            foreach (var settingsRow in settingsRows.Cast<IDictionary<string, object?>>())
            {
                if ((CardRowState) settingsRow.TryGet<int>(CardRow.SystemStateKey) == CardRowState.Deleted
                    || settingsRow.TryGet<Guid?>(KrConstants.StageRowIDReferenceToOwner) != stageRowID)
                {
                    continue;
                }

                AppendString(
                    builder,
                    settingsRow.TryGet<string>(KrConstants.KrForkSecondaryProcessesSettingsVirtual.SecondaryProcessName),
                    null,
                    true,
                    limit: isClient ? DefaultSettingMax : -1);
            }
        }

        #endregion
    }
}
