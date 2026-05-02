using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Views.Workplaces.Json;

namespace Tessa.Extensions.Default.Console.Scripts
{
    /// <summary>
    /// Скрипт для апгрейда версии JSON рабочих мест в БД.
    /// </summary>
    [ConsoleScript]
    public sealed class UpgradeWorkplacesSql :
        ServerConsoleScriptBase
    {
        #region Base Overrides

        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            await using var db = await this.CreateDbManagerAsync(cancellationToken);
            var dbms = db.Dbms;
            var builderFactory = new QueryBuilderFactory(dbms);

            db
                .SetCommand(
                    builderFactory
                        .Select().C(null, "ID", "Metadata")
                        .From("Workplaces").NoLock()
                        .Build())
                .LogCommand();

            var models = new List<(Guid ID, string? Metadata)>();
            await using (var reader = await db.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetGuid(0);
                    var metadata = await reader.GetSequentialNullableStringAsync(1, dbms, cancellationToken);
                    models.Add((id, metadata));
                }
            }

            foreach (var (id, metadata) in models)
            {
                JsonWorkplaceMetadata parsedMetadata;
                try
                {
                    parsedMetadata = NotNullOrThrow(metadata.FromJsonString<JsonWorkplaceMetadata>());
                }
                catch (Exception)
                {
                    await this.Logger.WriteLineAsync($"Workplace with ID {id} is not in JSON format.");
                    continue;
                }

                var jsonFormat = parsedMetadata.FormatVersion;
                if (jsonFormat is < TessaJsonSerializationContext.MinimalSupportedWorkplaceJsonVersion or >= TessaJsonSerializationContext.WorkplaceJsonVersion)
                {
                    continue;
                }

                await this.Logger.WriteLineAsync($"Upgrading JSON format metadata for workplace with ID {id}.");
                parsedMetadata.FormatVersion = TessaJsonSerializationContext.WorkplaceJsonVersion;
                var upgradedMetadata = parsedMetadata.ToJsonString(indented: false);

                await db
                    .SetCommand(
                        builderFactory
                            .Update("Workplaces")
                            .C("Metadata").Equals().P("Metadata")
                            .Where().C("ID").Equals().P("ID")
                            .Build(),
                        db.Parameter("ID", id, DataType.Guid),
                        db.Parameter("Metadata", upgradedMetadata, DataType.BinaryJson))
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);
            }
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Upgrades workplaces metadata JSON format in the table \"Workplaces\". No params required.");
        }

        #endregion
    }
}
