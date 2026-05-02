using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Console.SqlScript
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            IEnumerable<string>? source,
            bool transaction,
            string? configurationString,
            string? databaseName,
            string[]? parameters,
            bool selectMode,
            bool csvResult = false,
            int topRowCount = 0,
            char csvSeparator = ';',
            bool showHeaders = false,
            CancellationToken cancellationToken = default)
        {
            Func<DbManager>?[] createDbManagerFuncClosure = [null];
            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                (c, ct) => c.Container
                    .RegisterFactory<DbManager>(
                        _ => NotNullOrThrow(createDbManagerFuncClosure[0])(),
                        new PerResolveLifetimeManager())
                    .RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                async (c, ct) =>
                {
                    var configuration = c.ConfigurationManager!.Configuration;
                    var (configurationDataProvider, configurationConnection) = configuration.GetConfigurationDataProvider(configurationString);
                    var factory = configuration
                        .GetConfigurationDataProviderFromType(configurationConnection.DataProvider)
                        .GetDbProviderFactory();
                    var connectionString = configurationConnection.ConnectionString;

                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        var builder = NotNullOrThrow(factory.CreateConnectionStringBuilder());
                        builder.ConnectionString = connectionString;
                        builder["Database"] = databaseName;
                        connectionString = builder.ToString();
                    }

                    await logger.InfoAsync(
                        string.IsNullOrEmpty(configurationString)
                            ? "Opening connection to default database."
                            : "Opening connection to database with connection \"{0}\".",
                        configurationString);

                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        await logger.InfoAsync("Overriding connection to database \"{0}\".", databaseName);
                    }

                    createDbManagerFuncClosure[0] = () => new DbManager(configurationDataProvider.GetDataProvider(connectionString), connectionString);

                    var dbScope = c.Container.Resolve<IDbScope>();
                    var transactionStrategy = c.Container.Resolve<ITransactionStrategy>();

                    await using var _ = dbScope.Create();
                    var db = dbScope.Db;
                    object? outputResult = null;
                    try
                    {
                        string[] sourceArray = source.AsArray() ?? [];
                        if (sourceArray.Length > 0)
                        {
                            await logger.InfoAsync("Executing available scripts from \"{0}\".", string.Join(" ", sourceArray));
                        }

                        List<string> sourceFiles = DefaultConsoleHelper.GetSourceFiles(sourceArray, "*.sql");
                        if (sourceFiles.Count > 0)
                        {
                            foreach (string filePath in sourceFiles)
                            {
                                await logger.InfoAsync("Found script \"{0}\".", filePath);

                                string sqlText = await File.ReadAllTextAsync(filePath, ct);

                                outputResult = await ExecuteScriptAsync(
                                    db,
                                    sqlText,
                                    logger,
                                    transaction,
                                    parameters,
                                    selectMode,
                                    csvResult,
                                    topRowCount,
                                    csvSeparator,
                                    showHeaders,
                                    transactionStrategy,
                                    ct);
                            }
                        }
                        else
                        {
                            await logger.InfoAsync(
                                "Query text is expected from console input or previous chained command.{0}" +
                                "You can separate commands with \"GO\", starting from a new line (Enter, GO, Enter).{0}" +
                                "Use new line with EOF ({1}) to complete input. Ctrl+C to cancel.",
                                Environment.NewLine,
                                OperatingSystem.IsLinux() ? "Enter, Ctrl+D, Ctrl+D" : "Enter, Ctrl+Z, Enter");

                            string sqlText = (await System.Console.In.ReadToEndAsync(ct)).Trim();
                            outputResult = await ExecuteScriptAsync(
                                db,
                                sqlText,
                                logger,
                                transaction,
                                parameters,
                                selectMode,
                                csvResult,
                                topRowCount,
                                csvSeparator,
                                showHeaders,
                                transactionStrategy,
                                ct);
                        }
                    }
                    finally
                    {
                        await db.DisposeAsync();
                        await logger.InfoAsync("Database connection is closed.");
                    }

                    await logger.InfoAsync("Scripts have been executed.");

                    if (selectMode)
                    {
                        string? text = FormatToString(outputResult);
                        await logger.WriteAsync(text);
                    }

                    return 0;
                },
                cancellationToken);
        }

        private static async Task<object?> ExecuteScriptAsync(
            DbManager dbManager,
            string sqlText,
            IConsoleLogger logger,
            bool transaction,
            string[]? parameters,
            bool selectMode,
            bool csvResult,
            int topRowCount,
            char csvSeparator,
            bool showHeaders,
            ITransactionStrategy transactionStrategy,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(sqlText))
            {
                await logger.InfoAsync("Script is empty.");
                return null;
            }

            object? outputResult = null;

            sqlText = sqlText.NormalizeLineEndingsOnCurrentPlatform();

            string[] sqlCommands = SqlHelper.SplitGo(sqlText).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            if (sqlCommands.Length > 0)
            {
                var parsedParameters = DefaultConsoleHelper.ParseParameters(parameters);
                var dbParameters = new DataParameter[parsedParameters.Count];

                int index = 0;
                foreach ((string key, string? value) in parsedParameters)
                {
                    dbParameters[index++] = dbManager.Parameter(key, value, DataType.NVarChar);
                }

                if (transaction)
                {
                    var validationResult = new ValidationResultBuilder();
                    await logger.InfoAsync("Starting transaction.");
                    var isSuccessful = await transactionStrategy.ExecuteInTransactionAsync(
                        validationResult,
                        async x =>
                        {
                            outputResult = await ExecuteSql(
                                sqlCommands,
                                selectMode,
                                logger,
                                dbParameters,
                                dbManager,
                                csvResult,
                                topRowCount,
                                csvSeparator,
                                showHeaders,
                                ct);
                        },
                        ct);
                    if (!isSuccessful)
                    {
                        await logger.LogResultAsync(validationResult.Build());
                        await logger.InfoAsync("Rolling back transaction.");
                    }
                    else
                    {
                        await logger.InfoAsync("Commiting transaction.");
                    }
                }
                else
                {
                    outputResult = await ExecuteSql(
                        sqlCommands,
                        selectMode,
                        logger,
                        dbParameters,
                        dbManager,
                        csvResult,
                        topRowCount,
                        csvSeparator,
                        showHeaders,
                        ct);
                }
            }

            return outputResult;
        }

        private static async Task<object?> ExecuteSql(
            string[] sqlCommands,
            bool selectMode,
            IConsoleLogger logger,
            DataParameter[] dbParameters,
            DbManager dbManager,
            bool csvResult,
            int topRowCount,
            char csvSeparator,
            bool showHeaders,
            CancellationToken ct = default)
        {
            object? outputResult = null;

            for (int i = 0; i < sqlCommands.Length; i++)
            {
                if (sqlCommands.Length == 1 || selectMode)
                {
                    await logger.InfoAsync("Executing command.");
                }
                else
                {
                    await logger.InfoAsync("Executing command {0} / {1}.", i + 1, sqlCommands.Length);
                }

                if (i == 1)
                {
                    // расширяем массив, чтобы добавить параметр @Result
                    Array.Resize(ref dbParameters, dbParameters.Length + 1);
                }

                string text = sqlCommands[i].Trim();

                if (i >= 1)
                {
                    dbParameters[^1] = dbManager.Parameter("Result", outputResult);

                    // эта штука актуальна для Postgres для команды REINDEX DATABASE dbname, где "dbname" не может быть параметром;
                    // если мы находим в тексте запрос не параметр @Result, а спец-строку @Result@, то мы явно её заменяем на текст
                    text = text.Replace("@Result@", FormatToString(outputResult) ?? string.Empty, StringComparison.Ordinal);
                }

                // пробелы мы уже отрезали и значения null исключили
                if (text.StartsWith("--NORESULT", StringComparison.OrdinalIgnoreCase))
                {
                    await logger.InfoAsync("Suppressing \"Result\" parameter with --NORESULT.");
                    dbManager.SetCommand(text);
                }
                else
                {
                    dbManager.SetCommand(text, dbParameters);
                }

                dbManager
                    .SetCommandTimeout(0)
                    .LogCommand();

                bool lastCommand = i + 1 == sqlCommands.Length;
                if (lastCommand && !selectMode)
                {
                    // Последняя команда в режиме EXECUTE не возвращает результат.
                    await dbManager
                        .ExecuteNonQueryAsync(ct);
                }
                else if (lastCommand && csvResult)
                {
                    // в режиме SELECT последняя команда при выводе в CSV читает все строки и колонки.
                    outputResult = await DefaultConsoleHelper.ExecuteReaderAndReturnCsvAsync(dbManager, csvSeparator, topRowCount, showHeaders, ct);
                }
                else
                {
                    // в режиме SELECT без вывода в CSV выполняем последнюю команду и возвращаем результат от первой колонки в первой строке;
                    // в режиме EXECUTE и в SELECT для непоследней команды читается результат,
                    // если он есть, для передачи в следующую команду как параметр @Result;
                    // если результата нет, то передаётся количество строк, как в ExecuteNonQuery.
                    outputResult = await dbManager.ExecuteAsync<object>(ct);
                }
            }

            return outputResult;
        }
    }
}
