using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.IdentityModel.Tokens;
using Tessa.Cards;
using Tessa.Discovery;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using CsvWriter = Tessa.Platform.IO.CsvWriter;

namespace Tessa.Extensions.Default.Console
{
    public static class DefaultConsoleHelper
    {
        #region Static Methods

        public static List<string> GetSourceFiles(
            IEnumerable<string>? source,
            string patternMatch,
            bool throwIfNotFound = true,
            bool checkPatternMatch = false,
            string? displayPatternMatch = null)
        {
            var result = new List<string>();

            foreach (var sourceItem in source ?? [])
            {
                // если указан throwIfNotFound, то при наличии хотя бы одного параметра без скриптов будет ошибка с указанием имени папки
                result.AddRange(GetSourceFiles(sourceItem, patternMatch, throwIfNotFound, checkPatternMatch, displayPatternMatch));
            }

            // если параметров не указано, то здесь возвращается пустой массив
            return result;
        }


        public static string NormalizeFolderAndCreateIfNotExists(string? sourceFolder)
        {
            sourceFolder = sourceFolder.NormalizePathOnCurrentPlatform() ?? string.Empty;

            if (sourceFolder == ".")
            {
                sourceFolder = Directory.GetCurrentDirectory();
            }

            if (sourceFolder.Length > 0 && !Directory.Exists(sourceFolder))
            {
                Directory.CreateDirectory(sourceFolder);
            }

            return sourceFolder;
        }


        public static List<string> GetSourceFiles(
            string? source,
            string patternMatch,
            bool throwIfNotFound = true,
            bool checkPatternMatch = false,
            string? displayPatternMatch = null)
        {
            // в качестве patternMatch мы ожидаем паттерн только для файла, не для части пути,
            // иначе checkPatternMatch будет работать некорректно; пример: *.card

            source = source.NormalizePathOnCurrentPlatform();

            if (source == ".")
            {
                source = Directory.GetCurrentDirectory();
            }

            // можем вернуть все файлы в заданной папке или в её подпапках (чаще всего)
            if (Directory.Exists(source))
            {
                // выводим сначала отсортированный список для первого расширения, потом для второго,
                // но между собой их не перемешиваем (если надо - вызывающий код сам перемешает)
                var filePathList = patternMatch
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(pattern => Directory
                        .GetFiles(source, pattern.Trim(), SearchOption.AllDirectories)
                        .OrderBy(x => x))
                    .ToList();

                if (filePathList.Count > 0)
                {
                    return filePathList;
                }
            }

            // или заданный файл
            if (File.Exists(source))
            {
                bool addSourceToResult;
                if (checkPatternMatch && !string.Equals(source, patternMatch, StringComparison.OrdinalIgnoreCase))
                {
                    // требуется определить, действительно ли указанный файл совпадает с patternMatch-ем;
                    // например, для *.jcardlib надо проверить, что расширение у source равно .jcardlib
                    var sourceFolder = Path.GetDirectoryName(source);
                    if (string.IsNullOrEmpty(sourceFolder))
                    {
                        sourceFolder = Directory.GetCurrentDirectory();
                    }

                    // ReSharper disable once AssignNullToNotNullAttribute
                    var rootedSource = Path.Combine(sourceFolder, Path.GetFileName(source));

                    // для всех файлов в папке вместе с source проверяем, что хотя бы один из файлов,
                    // найденных по patternMatch, совпадает с указанным source
                    addSourceToResult = false;

                    var filesMath = patternMatch
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .SelectMany(pattern => Directory
                            .EnumerateFiles(sourceFolder, pattern.Trim(), SearchOption.AllDirectories));

                    foreach (var fileMatch in filesMath)
                    {
                        if (string.Equals(fileMatch, rootedSource, StringComparison.OrdinalIgnoreCase))
                        {
                            addSourceToResult = true;
                            break;
                        }
                    }
                }
                else
                {
                    addSourceToResult = true;
                }

                var result = new List<string>();
                if (addSourceToResult)
                {
                    result.Add(source);
                }

                return result;
            }

            // или все подходящие файлы, например, "Configuration\Localization\*.jlocalization"
            var folder = Path.GetDirectoryName(source);
            if (string.IsNullOrEmpty(folder))
            {
                folder = Directory.GetCurrentDirectory();
            }

            if (Directory.Exists(folder) && !string.IsNullOrEmpty(source))
            {
                var matchPattern = Path.GetFileName(source);
                if (!string.IsNullOrEmpty(matchPattern))
                {
                    var filePathList = Directory
                        .GetFiles(folder, matchPattern, SearchOption.TopDirectoryOnly)
                        .OrderBy(x => x)
                        .ToList();

                    if (filePathList.Count > 0)
                    {
                        return filePathList;
                    }
                }
            }

            if (!throwIfNotFound)
            {
                return [];
            }

            throw new FileNotFoundException(
                $"Couldn't locate {displayPatternMatch ?? patternMatch} file in \"{source}\"",
                source);
        }


        public static async Task DropAndCreateDatabaseAsync(
            IConsoleLogger logger,
            IConfigurationManager configurationManager,
            string? configurationString,
            string? databaseName,
            bool dropOld,
            bool createNew,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(logger);
            ThrowIfNull(configurationManager);

            var configuration = configurationManager.Configuration;
            var (_, configurationConnection) = configuration.GetConfigurationDataProvider(configurationString);

            var factory = configuration
                .GetConfigurationDataProviderFromType(configurationConnection.DataProvider)
                .GetDbProviderFactory();

            var connectionBuilder = NotNullOrThrow(factory.CreateConnectionStringBuilder());
            connectionBuilder.ConnectionString = configurationConnection.ConnectionString;

            var dbms = factory.GetDbms();
            switch (dbms)
            {
                case Dbms.SqlServer:
                    if (string.IsNullOrEmpty(databaseName))
                    {
                        databaseName = (string) connectionBuilder["Initial Catalog"];
                    }

                    connectionBuilder["Database"] = "master";
                    break;

                case Dbms.PostgreSql:
                    if (string.IsNullOrEmpty(databaseName))
                    {
                        databaseName = (string) connectionBuilder["Database"];
                    }

                    connectionBuilder["Database"] = "postgres";
                    break;

                default:
                    throw new NotSupportedException();
            }

            var masterConnectionString = connectionBuilder.ConnectionString;

            DbConnection? connection = null;
            DbCommand? command = null;

            try
            {
                connection = NotNullOrThrow(factory.CreateConnection());
                connection.ConnectionString = masterConnectionString;
                command = connection.CreateCommand();

                await connection.OpenAsync(cancellationToken);

                if (dropOld)
                {
                    await logger.InfoAsync("Dropping database \"{0}\" if it exists", databaseName);

                    await DatabaseHelper.DropDatabaseAsync(command, dbms, databaseName, cancellationToken);
                }

                if (createNew)
                {
                    await DatabaseHelper.CreateDatabaseAsync(command, dbms, databaseName, cancellationToken: cancellationToken);
                }
            }
            finally
            {
                if (command is not null)
                {
                    await command.DisposeAsync();
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }
            }
        }


        public static async Task<Dbms> CheckConnectionAsync(
            IConsoleLogger logger,
            IConfigurationManager configurationManager,
            string? configurationString,
            int timeoutSecondsOverride,
            bool outputConnectionString,
            string? databaseName = null,
            bool keepOriginalDatabase = false,
            IDbConnectionStringCleaner? connectionStringCleaner = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(logger);
            ThrowIfNull(configurationManager);

            var configuration = configurationManager.Configuration;
            var (_, configurationConnection) = configuration.GetConfigurationDataProvider(configurationString);

            var factory = configuration
                .GetConfigurationDataProviderFromType(configurationConnection.DataProvider)
                .GetDbProviderFactory();

            var dbms = factory.GetDbms();

            string databaseValue;
            string timeoutKey;
            switch (dbms)
            {
                case Dbms.SqlServer:
                    databaseValue = "master";
                    timeoutKey = "Connect Timeout";
                    break;

                case Dbms.PostgreSql:
                    databaseValue = "postgres";
                    timeoutKey = "Timeout";
                    break;

                default:
                    throw new NotSupportedException(nameof(Dbms));
            }

            var connectionBuilder = NotNullOrThrow(factory.CreateConnectionStringBuilder());
            connectionBuilder.ConnectionString = configurationConnection.ConnectionString;
            if (!keepOriginalDatabase)
            {
                connectionBuilder["Database"] = string.IsNullOrEmpty(databaseName) ? databaseValue : databaseName.Trim();
            }

            if (timeoutSecondsOverride >= 0)
            {
                connectionBuilder[timeoutKey] = timeoutSecondsOverride;
            }

            var connectionString = connectionBuilder.ToString();
            var cleanedConnectionString = connectionStringCleaner is not null
                ? await connectionStringCleaner.CleanConnectionSafeAsync(dbms, connectionString, cancellationToken)
                : connectionString;

            await logger.InfoAsync("Connection string is:{0}{1}", Environment.NewLine, cleanedConnectionString);

            if (outputConnectionString)
            {
                await logger.WriteLineAsync(cleanedConnectionString);
            }

            await using var connection = NotNullOrThrow(factory.CreateConnection());
            connection.ConnectionString = connectionString;
            await connection.OpenAsync(cancellationToken);

            return dbms;
        }


        public static string EscapeStringFromConsole(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            var sb = StringBuilderHelper.Acquire(s.Length);

            Span<char> hexNumber = stackalloc char[4];
            hexNumber.Clear();

            var hexIndex = 0;
            var escapeMode = false;

            foreach (var c in s)
            {
                if (escapeMode)
                {
                    var continueEscaping = false;

                    switch (c)
                    {
                        case '\\':
                            sb.Append('\\');
                            break;

                        case 'n':
                            sb.Append('\n');
                            break;

                        case 'r':
                            sb.Append('\r');
                            break;

                        case 't':
                            sb.Append('\t');
                            break;

                        case 'q':
                            sb.Append('\"');
                            break;

                        default:
                            if (char.IsDigit(c) || c is >= 'a' and <= 'f' or >= 'A' and <= 'F')
                            {
                                hexNumber[hexIndex++] = char.ToLowerInvariant(c);

                                if (hexIndex == hexNumber.Length)
                                {
                                    // парсинг 16-ричного числа без ведущего 0x
                                    try
                                    {
                                        var code = uint.Parse(hexNumber, NumberStyles.HexNumber);
                                        sb.Append((char) code);
                                    }
                                    catch (Exception)
                                    {
                                        // значит не число, а просто какие-то числобуквы
                                        for (var i = 0; i < hexIndex; i++)
                                        {
                                            sb.Append(hexNumber[i]);
                                        }
                                    }

                                    // ниже выход из эскейпинга со сбросом hexIndex
                                }
                                else
                                {
                                    // ждём следующих цифр
                                    continueEscaping = true;
                                }
                            }
                            else
                            {
                                // непонятное записываем as-is вместе с "задержанными" символами из hexNumber, потом выходим из режима
                                for (var i = 0; i < hexIndex; i++)
                                {
                                    sb.Append(hexNumber[i]);
                                }

                                sb.Append(c);
                            }

                            break;
                    }

                    if (!continueEscaping)
                    {
                        escapeMode = false;
                        hexIndex = 0;
                    }
                }
                else if (c == '\\')
                {
                    escapeMode = true;
                }
                else
                {
                    sb.Append(c);
                }
            }

            // "задержанные" цифры, которых не дождались, и строка закончилась
            for (var i = 0; i < hexIndex; i++)
            {
                sb.Append(hexNumber[i]);
            }

            return sb.ToStringAndRelease();
        }


        public static async Task<string> ExecuteReaderAndReturnCsvAsync(
            DbManager db,
            char separator = ';',
            int rowCount = 0,
            bool showHeaders = false,
            CancellationToken cancellationToken = default)
        {
            MemoryStream? memoryStream = null;
            CsvWriter? csvWriter = null;
            DbDataReader? reader = null;
            StreamReader? streamReader = null;

            try
            {
                memoryStream = new MemoryStream();
                csvWriter = new CsvWriter(memoryStream, Encoding.UTF8, 65535, leaveOpen: true) { NewLine = "\n", Separator = separator };

                reader = await db.ExecuteReaderAsync(cancellationToken);
                var rowIndex = 0;

                while (await reader.ReadAsync(cancellationToken))
                {
                    if (rowCount > 0 && rowIndex >= rowCount)
                    {
                        break;
                    }

                    var fieldCount = reader.FieldCount;
                    if (showHeaders && rowIndex == 0)
                    {
                        for (var i = 0; i < fieldCount; i++)
                        {
                            if (i > 0)
                            {
                                await csvWriter.WriteSeparatorAsync();
                            }

                            await csvWriter.WriteAsync(reader.GetName(i));
                        }

                        await csvWriter.WriteLineAsync();
                    }

                    if (rowIndex++ > 0)
                    {
                        await csvWriter.WriteLineAsync();
                    }

                    for (var i = 0; i < fieldCount; i++)
                    {
                        if (i > 0)
                        {
                            await csvWriter.WriteSeparatorAsync();
                        }

                        string? value;
                        if (await reader.IsDBNullAsync(i, cancellationToken))
                        {
                            value = null;
                        }
                        else
                        {
                            var obj = reader[i];
                            value = obj is DateTime dateTime
                                ? FormatDateTime(dateTime, convertToLocal: false)
                                : FormatToString(obj);
                        }

                        await csvWriter.WriteAsync(value);
                    }
                }

                await reader.DisposeAsync();
                reader = null;

                await csvWriter.DisposeAsync();
                csvWriter = null;

                memoryStream.Position = 0L;

                streamReader = new StreamReader(memoryStream, Encoding.UTF8);
                return await streamReader.ReadToEndAsync(cancellationToken);
            }
            finally
            {
                streamReader?.Dispose();

                if (reader is not null)
                {
                    await reader.DisposeAsync();
                }

                if (csvWriter is not null)
                {
                    await csvWriter.DisposeAsync();
                }

                if (memoryStream is not null)
                {
                    await memoryStream.DisposeAsync();
                }
            }
        }


        public static void AddOperationToValidationResult(
            string operationPrefix,
            string cardName,
            ValidationResult result,
            IValidationResultBuilder validationResult,
            bool ignoreExistentCards = false,
            bool ignoreRepairMessages = false)
        {
            if (result.Items.Count == 0)
            {
                return;
            }

            if (ignoreExistentCards
                && result.Items.Any(x => CardValidationKeys.IsCardExists(x.Key)))
            {
                return;
            }

            var actualResult = result;

            if (ignoreRepairMessages && result.IsSuccessful)
            {
                var newItems = result.Items
                    .Where(x => !CardValidationKeys.IsCardRepair(x.Key))
                    .ToArray();

                if (newItems.Length == 0)
                {
                    actualResult = ValidationResult.Empty;
                }
                else if (newItems.Length < result.Items.Count)
                {
                    actualResult = new ValidationResult(newItems);
                }

                if (actualResult.Items.Count == 0)
                {
                    return;
                }
            }

            var details = actualResult.ToString(ValidationLevel.Detailed);

            if (actualResult.HasErrors)
            {
                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(typeof(DefaultConsoleHelper))
                    .ErrorDetails(
                        string.Format(
                            LocalizeName(operationPrefix)
                            + " \"{0}\" "
                            + LocalizeName("UI_Cards_HasErrorsSuffix"),
                            cardName),
                        details)
                    .End();
            }
            else if (actualResult.HasWarnings)
            {
                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(typeof(DefaultConsoleHelper))
                    .WarningDetails(
                        string.Format(
                            LocalizeName(operationPrefix)
                            + " \"{0}\" "
                            + LocalizeName("UI_Cards_HasWarningsSuffix"),
                            cardName),
                        details)
                    .End();
            }
            else
            {
                ValidationSequence
                    .Begin(validationResult)
                    .SetObjectName(typeof(DefaultConsoleHelper))
                    .InfoDetails(
                        string.Format(
                            LocalizeName(operationPrefix)
                            + " \"{0}\" "
                            + LocalizeName("UI_Cards_HasInfoSuffix"),
                            cardName),
                        details)
                    .End();
            }
        }


        public static StringBuilder GetQuotedItemsText(
            StringBuilder builder,
            string singleMessage,
            string multipleMessage,
            ICollection<string> items)
        {
            if (items.Count == 0)
            {
                return builder;
            }

            if (items.Count == 1)
            {
                return builder
                    .Append(LocalizeName(singleMessage))
                    .Append(" \"")
                    .Append(items.First())
                    .Append("\".");
            }

            builder
                .Append(LocalizeName(multipleMessage))
                .Append(" (")
                .Append(items.Count)
                .Append("):");

            foreach (var item in items)
            {
                builder
                    .AppendLine()
                    .Append('"')
                    .Append(item)
                    .Append('"');
            }

            return builder;
        }


        public static void RemoveCardInfoListDuplicates(List<CardInfo> cardInfoList)
        {
            for (var lastIndex = cardInfoList.Count - 1; lastIndex > 0; lastIndex--)
            {
                var cardID = cardInfoList[lastIndex].CardID;

                var firstIndex = -1;
                for (var i = 0; i < cardInfoList.Count; i++)
                {
                    if (cardInfoList[i].CardID == cardID)
                    {
                        firstIndex = i;
                        break;
                    }
                }

                if (firstIndex >= 0 && firstIndex != lastIndex)
                {
                    // lastIndex является дублем, удаляем
                    cardInfoList.RemoveAt(lastIndex);
                }
            }
        }


        public static async Task<List<CardInfo>?> TryParseCardInfoListAsync(
            IEnumerable<string>? identifiers,
            IConsoleLogger logger,
            string? separatorChar = ";",
            CultureInfo? localizationCulture = null,
            CancellationToken cancellationToken = default)
        {
            var allIdentifiers = string.Join(" ", identifiers ?? []);

            try
            {
                if (!string.IsNullOrWhiteSpace(allIdentifiers))
                {
                    return allIdentifiers
                        .Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => new CardInfo(Guid.Parse(x.Trim())))
                        .ToList();
                }

                if (string.IsNullOrEmpty(separatorChar))
                {
                    separatorChar = ";";
                }
                else if (separatorChar.Length > 1)
                {
                    separatorChar = separatorChar[0].ToString();
                }

                await logger.InfoAsync(
                    "Cards identifiers are expected from console input or previous chained command.{0}" +
                    "Place each id on a new line, use new line with EOF ({1}) to complete input.{0}" +
                    "Provide card name after separator \"{2}\" (optional). Ctrl+C to cancel.",
                    Environment.NewLine,
                    OperatingSystem.IsLinux() ? "Enter, Ctrl+D, Ctrl+D" : "Enter, Ctrl+Z, Enter",
                    separatorChar);

                var cardList = new List<CardInfo>();
                var input = (await System.Console.In.ReadToEndAsync(cancellationToken)).Trim();

                if (!string.IsNullOrEmpty(input))
                {
                    // разделитель между колонками, по умолчанию ";"

                    var configuration = new CsvConfiguration(localizationCulture ?? CultureInfo.InvariantCulture) { Delimiter = separatorChar };

                    using var stringReader = new StringReader(input);
                    using var csvReader = new CsvReader(stringReader, configuration);
                    var length = 0;

                    while (await csvReader.ReadAsync())
                    {
                        if (length == 0)
                        {
                            length = csvReader.Parser.Count;
                            if (length == 0)
                            {
                                break;
                            }
                        }

                        if (csvReader.TryGetField(0, out string? id)
                            && !string.IsNullOrEmpty(id = id?.Trim())
                            && Guid.TryParse(id, out var cardID))
                        {
                            cardList.Add(
                                new CardInfo(
                                    cardID,
                                    length > 1 && csvReader.TryGetField(1, out string? cardName)
                                        ? cardName
                                        : null));
                        }
                    }
                }

                return cardList;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await logger.LogExceptionAsync("Error parsing identifiers", e);
                return null;
            }
        }


        public static async Task<List<Guid>?> TryParseIdentifiersListAsync(
            IEnumerable<string?>? identifiers,
            IConsoleLogger logger,
            CultureInfo? localizationCulture = null,
            CancellationToken cancellationToken = default)
        {
            var allIdentifiers = string.Join(" ", identifiers ?? []);

            try
            {
                if (!string.IsNullOrWhiteSpace(allIdentifiers))
                {
                    return allIdentifiers
                        .Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => Guid.Parse(x.Trim()))
                        .ToList();
                }

                await logger.InfoAsync(
                    "Identifiers are expected from console input or previous chained command.{0}" +
                    "Place each id on a new line, use new line with EOF ({1}) to complete input. Ctrl+C to cancel.",
                    Environment.NewLine,
                    OperatingSystem.IsLinux() ? "Enter, Ctrl+D, Ctrl+D" : "Enter, Ctrl+Z, Enter");

                var identifierList = new List<Guid>();
                var input = (await System.Console.In.ReadToEndAsync(cancellationToken)).Trim();

                if (!string.IsNullOrEmpty(input))
                {
                    // разделитель влияет на эскейпинг, даже если колонка одна
                    var configuration = new CsvConfiguration(localizationCulture ?? CultureInfo.InvariantCulture) { Delimiter = ";" };

                    using var stringReader = new StringReader(input);
                    using var csvReader = new CsvReader(stringReader, configuration);
                    var length = 0;

                    while (await csvReader.ReadAsync())
                    {
                        if (length == 0)
                        {
                            length = csvReader.Parser.Count;
                            if (length == 0)
                            {
                                break;
                            }
                        }

                        if (csvReader.TryGetField(0, out string? id)
                            && !string.IsNullOrEmpty(id = id?.Trim())
                            && Guid.TryParse(id, out var identifier))
                        {
                            identifierList.Add(identifier);
                        }
                    }
                }

                return identifierList;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await logger.LogExceptionAsync("Error parsing identifiers", e);
                return null;
            }
        }


        public static Dictionary<string, string?> ParseParameters(
            IEnumerable<string?>? parameters,
            IEqualityComparer<string>? comparer = null)
        {
            var result = new Dictionary<string, string?>(comparer ?? StringComparer.Ordinal);
            foreach (var parameter in parameters ?? [])
            {
                // ParamName=ParamValue
                // или
                // ParamName (тогда параметр NULL)

                var p = (parameter ?? string.Empty).Trim();
                if (p.Length == 0)
                {
                    continue;
                }

                var separator = p.IndexOf('=', StringComparison.Ordinal);
                if (separator == 0)
                {
                    // строка начинается со знака равенства
                    continue;
                }

                if (separator < 0)
                {
                    // разделителя нет, параметр равен NULL
                    result[p] = null;
                }
                else if (separator < p.Length - 1)
                {
                    // разделитель есть, и значение справа тоже есть
                    result[p[..separator]] = p[(separator + 1)..];
                }
                else
                {
                    // разделитель есть, но значения нет, отрезаем его, а в качестве значения указываем пустую строку
                    result[p[..^1]] = string.Empty;
                }
            }

            return result;
        }


        public static IEnumerable<string> GetSchemePartitions(
            string schemeTsdFileFullPath,
            IEnumerable<string>? includedPaths = null,
            IEnumerable<string>? excludedPaths = null)
        {
            var includedPathArray = includedPaths?.Select(x => x.NormalizePathOnCurrentPlatform()).ToArray() ?? [];
            var excludedPathArray = excludedPaths?.Select(x => x.NormalizePathOnCurrentPlatform()).ToArray() ?? [];

            var directoryPath = Path.GetDirectoryName(schemeTsdFileFullPath);
            var partitions = FileSchemeService.GetPartitionPaths(schemeTsdFileFullPath);

            var actualIncludedPathArray = (string.IsNullOrEmpty(directoryPath)
                    ? includedPathArray.Select(Path.GetFullPath)
                    : includedPathArray.Select(x => Path.GetFullPath(x, directoryPath)))
                .SelectMany(x => Directory.Exists(x)
                    ? (IEnumerable<string>) Directory.GetFiles(x, $"*{FileSchemeService.PartitionFileExtension}", SearchOption.AllDirectories).OrderBy(y => y)
                    : [x])
                .Where(File.Exists)
                .ToArray();

            var actualExcludedPathArray = (string.IsNullOrEmpty(directoryPath)
                    ? excludedPathArray.Select(Path.GetFullPath)
                    : excludedPathArray.Select(x => Path.GetFullPath(x, directoryPath)))
                .SelectMany(x => Directory.Exists(x)
                    ? Directory.GetFiles(x, $"*{FileSchemeService.PartitionFileExtension}", SearchOption.AllDirectories)
                    : [x])
                .ToArray();

            return partitions
                .Concat(actualIncludedPathArray)
                .Except(actualExcludedPathArray, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Изменяет расширение файла в строковом представлении его пути на новое, если текущее расширение совпадает с указанным в <paramref name="oldExtension"/>.
        /// </summary>
        /// <param name="path">Строковое представление пути файла.</param>
        /// <param name="oldExtension">Изменяемое расширение файла.</param>
        /// <param name="newExtension">Новое расширение файла.</param>
        /// <returns>Строковое представление пути файла с измененным расширением.</returns>
        [return: NotNullIfNotNull(nameof(path))]
        public static string? ChangeExtension(string? path, string oldExtension, string newExtension) =>
            path?.EndsWith(oldExtension, StringComparison.OrdinalIgnoreCase) == true
                ? path[..^oldExtension.Length] + newExtension
                : path;

        /// <summary>
        /// Создает JWT-токен.
        /// </summary>
        /// <param name="key">Ключ.</param>
        /// <param name="data">Сериализованные данные.</param>
        /// <param name="expiration">Время жизни токена.</param>
        /// <returns>JWT-токен.</returns>
        public static string CreateJwtToken(DiscoveryKey key, string data, TimeSpan expiration)
        {
            ThrowIfNull(key);

            var claims = new Claim[]
            {
                new("data", data, ClaimValueTypes.String),
            };

            var identity = new ClaimsIdentity(claims);

            // key
            var dsa = ECDsa.Create();
            dsa.ImportSubjectPublicKeyInfo(key.PublicKey, out _);
            dsa.ImportPkcs8PrivateKey(key.PrivateKey, out _);

            var securityKey = new ECDsaSecurityKey(dsa)
            {
                KeyId = Convert.ToBase64String(NotNullOrThrow(key.KeyID))
            };

            var signingCredentials = new SigningCredentials(securityKey, "ES512");

            // token
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(
                issuer: key.Issuer,
                subject: identity,
                expires: DateTime.UtcNow.Add(expiration),
                signingCredentials: signingCredentials);

            return handler.WriteToken(token);
        }

        /// <summary>
        /// Нормализует путь к файлу в соответствии с текущей платформой.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <returns>Нормализованный путь к файлу.</returns>
        [return: NotNullIfNotNull(nameof(filePath))]
        public static string? NormalizeFilePath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return filePath;
            }

            filePath = filePath.NormalizeLineEndingsOnCurrentPlatform();
            if (Path.GetDirectoryName(filePath) is { } resultDirectory)
            {
                filePath = Path.Combine(resultDirectory, Path.GetFileName(filePath));
            }

            return filePath;
        }

        #endregion
    }
}
