#nullable enable

using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform;

using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Обеспечивает установку флага <see cref="Keys.HiddenByDefaultKey"/> для <see cref="DefaultTaskTypes.KrUniversalTaskTypeName"/>,
    /// созданных из действия WE и имеющих варианты завершения для ФР, не требующих брать задание в работу.
    /// </summary>
    public sealed class KrUniversalTaskOptionsGetExtension : CardGetExtension
    {
        #region Base Overrides

        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            if (!context.RequestIsSuccessful ||
                context.Response!.TryGetCard() is not { } card)
            {
                return;
            }

            await using (context.DbScope!.Create())
            {
                var db = context.DbScope.Db;
                var factory = context.DbScope.BuilderFactory;

                foreach (var task in card.Tasks.Where(p =>
                             p.TypeID == DefaultTaskTypes.KrUniversalTaskTypeID
                             && p.Flags.Has(CardTaskFlags.Locked)))
                {
                    db.SetCommand(
                        factory
                            .Select()
                                .C("op", KrUniversalTaskOptions.Settings)
                            .From(KrUniversalTaskOptions.Name, "op").NoLock()
                            .Where()
                                .C("op", "ID").Equals().P("TaskRowID")
                            .Build(),
                        db.Parameter("TaskRowID", task.RowID, DataType.Guid))
                    .LogCommand();
                    
                    await using var reader = await db.ExecuteReaderAsync(context.CancellationToken);
                    while (await reader.ReadAsync(context.CancellationToken))
                    {
                        var settingsJson = reader.GetNullableString(0);
                        var settings =
                            string.IsNullOrEmpty(settingsJson)
                                ? null
                                : StorageHelper.DeserializeFromTypedJson(settingsJson);
                        
                        var optionFunctionRoleIDs =
                            settings?.TryGet<List<Guid>>(Keys.OptionFunctionRoles);
                        if (optionFunctionRoleIDs is not null &&
                            optionFunctionRoleIDs.Any(p =>
                                task.TaskSessionRoles.Any(q => q.FunctionRoleID == p)))
                        {
                            if (task.IsLockedEffective)
                            {
                                task.Info.Add(Keys.HiddenByDefaultKey, BooleanBoxes.True);
                            }
                            break;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
