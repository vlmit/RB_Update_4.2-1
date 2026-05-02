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
    /// Скрипт для апгрейда версии JSON рабочего места в БД.
    /// </summary>
    [ConsoleScript]
    public sealed class UpgradeSearchQueriesSql :
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
                        .From("SearchQueries").NoLock()
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
                JsonSearchQueryMetadata parsedMetadata;
                try
                {
                    parsedMetadata = NotNullOrThrow(metadata.FromJsonString<JsonSearchQueryMetadata>());
                }
                catch (Exception)
                {
                    await this.Logger.WriteLineAsync($"Search query with ID {id} is not in JSON format.");
                    continue;
                }

                var jsonFormat = parsedMetadata.FormatVersion;
                if (jsonFormat >= TessaJsonSerializationContext.SearchQueryJsonVersion)
                {
                    continue;
                }

                await this.Logger.WriteLineAsync($"Upgrading JSON format metadata for search queries with ID {id}.");
                UpdateVersionToLatest(parsedMetadata);
                var upgradedMetadata = parsedMetadata.ToJsonString(indented: false);

                await db
                    .SetCommand(
                        builderFactory
                            .Update("SearchQueries")
                            .C("Metadata").Equals().P("Metadata")
                            .Where().C("ID").Equals().P("ID")
                            .Build(),
                        db.Parameter("ID", id, DataType.Guid),
                        db.Parameter("Metadata", upgradedMetadata, DataType.BinaryJson))
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);
                continue;

                static void UpdateVersionToLatest(IJsonSearchQueryMetadata metadata)
                {
                    metadata.FormatVersion = TessaJsonSerializationContext.SearchQueryJsonVersion;
                    foreach (var item in metadata.Items)
                    {
                        UpdateVersionToLatest(item);
                    }
                }
            }
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Upgrades search queries metadata JSON format in the table \"SearchQueries\". No params required.");
        }

        #endregion
    }
}
