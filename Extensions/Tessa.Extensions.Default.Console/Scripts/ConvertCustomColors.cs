using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Initialization;
using Tessa.Platform.Storage;
using DataType = LinqToDB.DataType;

namespace Tessa.Extensions.Default.Console.Scripts
{
    /// <summary>
    /// Скрипт для преобразования кастомных пользовательских цветов в набор кастомных палитр.
    /// </summary>
    [ConsoleScript]
    public sealed class ConvertCustomColors :
        ServerConsoleScriptBase
    {
        #region Nested Types

        private sealed record DefaultPalette(Guid RowID, string? Alias, string? Caption);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            // открытие соединений для чтения и записи данных
            await using var dbReader = await this.CreateDbManagerAsync(cancellationToken);
            await using var dbWriter = await this.CreateDbManagerAsync(cancellationToken);

            var dbms = dbReader.Dbms;
            var builderFactory = new QueryBuilderFactory(dbms);

            var defaultPalettes = new List<DefaultPalette>();

            dbReader
                .SetCommand(builderFactory
                    .Select().C(null, "RowID", "Alias", "Caption")
                    .From("Palettes").NoLock()
                    .Build())
                .LogCommand();

            await using (var reader = await dbReader.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    defaultPalettes.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));
                }
            }

            dbReader
                .SetCommand(builderFactory
                    .Select().C(null, "ID", "Settings")
                    .From("PersonalRoleSatellite").NoLock()
                    .Build())
                .LogCommand();

            await using var reader2 = await dbReader.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader2.ReadAsync(cancellationToken))
            {
                Guid roleId = reader2.GetGuid(0);
                string? settingsJson = await reader2.GetSequentialNullableStringAsync(1, dbms, cancellationToken);

                if (string.IsNullOrEmpty(settingsJson))
                {
                    continue;
                }

                var userPalettes = new List<object>();
                var settings = StorageHelper.DeserializeFromTypedJson(settingsJson)!;
                if (settings.TryGet<Dictionary<string, object?>>("CustomBackgroundColorsVirtual") is { } customBackgroundColorsVirtual &&
                    customBackgroundColorsVirtual.TryGet<Dictionary<string, object?>>("Fields") is { } backgroundColors)
                {
                    backgroundColors.TryGetValue("Color1", out var backgroundColor1);
                    backgroundColors.TryGetValue("Color2", out var backgroundColor2);
                    backgroundColors.TryGetValue("Color3", out var backgroundColor3);
                    backgroundColors.TryGetValue("Color4", out var backgroundColor4);
                    var defaultBackgroundPalette = defaultPalettes
                        .FirstOrDefault(x => x.Alias?.Equals(PaletteHelper.BackgroundPalette, StringComparison.OrdinalIgnoreCase) == true);
                    if (defaultBackgroundPalette is null)
                    {
                        continue;
                    }

                    userPalettes.Add(new Dictionary<string, object?>
                    {
                        { "PaletteAlias", PaletteHelper.BackgroundPalette },
                        { "PaletteCaption", defaultBackgroundPalette.Caption },
                        { "PaletteRowID", defaultBackgroundPalette.RowID.ToString() },
                        { "RowID", Guid.NewGuid().ToString() },
                        { "Color1", backgroundColor1 },
                        { "Color2", backgroundColor2 },
                        { "Color3", backgroundColor3 },
                        { "Color4", backgroundColor4 },
                    });
                    settings.Remove("CustomBackgroundColorsVirtual");
                }

                if (settings.TryGet<Dictionary<string, object?>>("CustomForegroundColorsVirtual") is { } customForegroundColorsVirtual &&
                    customForegroundColorsVirtual.TryGet<Dictionary<string, object?>>("Fields") is { } foregroundColors)
                {
                    foregroundColors.TryGetValue("Color1", out var foregroundColor1);
                    foregroundColors.TryGetValue("Color2", out var foregroundColor2);
                    foregroundColors.TryGetValue("Color3", out var foregroundColor3);
                    foregroundColors.TryGetValue("Color4", out var foregroundColor4);
                    var defaultForegroundPalette = defaultPalettes
                        .FirstOrDefault(x => x.Alias?.Equals(PaletteHelper.ForegroundPalette, StringComparison.OrdinalIgnoreCase) == true);
                    if (defaultForegroundPalette is null)
                    {
                        continue;
                    }

                    userPalettes.Add(new Dictionary<string, object?>
                    {
                        { "PaletteAlias", PaletteHelper.ForegroundPalette },
                        { "PaletteCaption", defaultForegroundPalette.Caption },
                        { "PaletteRowID", defaultForegroundPalette.RowID.ToString() },
                        { "RowID", Guid.NewGuid().ToString() },
                        { "Color1", foregroundColor1 },
                        { "Color2", foregroundColor2 },
                        { "Color3", foregroundColor3 },
                        { "Color4", foregroundColor4 },
                    });
                    settings.Remove("CustomForegroundColorsVirtual");
                }

                if (settings.TryGet<Dictionary<string, object?>>("CustomBlockColorsVirtual") is { } customBlockColorsVirtual &&
                    customBlockColorsVirtual.TryGet<Dictionary<string, object?>>("Fields") is { } blockColors)
                {
                    blockColors.TryGetValue("Color1", out var blockColor1);
                    blockColors.TryGetValue("Color2", out var blockColor2);
                    blockColors.TryGetValue("Color3", out var blockColor3);
                    blockColors.TryGetValue("Color4", out var blockColor4);
                    var defaultBlockPalette = defaultPalettes
                        .FirstOrDefault(x => x.Alias?.Equals(PaletteHelper.BlockPalette, StringComparison.OrdinalIgnoreCase) == true);
                    if (defaultBlockPalette is null)
                    {
                        continue;
                    }

                    userPalettes.Add(new Dictionary<string, object?>
                    {
                        { "PaletteAlias", PaletteHelper.BlockPalette },
                        { "PaletteCaption", defaultBlockPalette.Caption },
                        { "PaletteRowID", defaultBlockPalette.RowID.ToString() },
                        { "RowID", Guid.NewGuid().ToString() },
                        { "Color1", blockColor1 },
                        { "Color2", blockColor2 },
                        { "Color3", blockColor3 },
                        { "Color4", blockColor4 },
                    });
                    settings.Remove("CustomBlockColorsVirtual");
                }

                settings["UserPalettesVirtual"] = new Dictionary<string, object?>
                {
                    { "Rows", userPalettes }
                };

                var newSettingsJson = StorageHelper.SerializeToTypedJson(settings);
                try
                {
                    dbWriter
                        .SetCommand(
                            builderFactory
                                .Update("PersonalRoleSatellite")
                                .C("Settings").Equals().P("Settings")
                                .Where().C("ID").Equals().P("ID")
                                .Build(),
                            dbWriter.Parameter("ID", roleId, DataType.Guid),
                            dbWriter.Parameter("Settings", newSettingsJson, DataType.BinaryJson))
                        .LogCommand();

                    await dbWriter.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await this.Logger.LogExceptionAsync($"Error while updating color settings for personal role ID={roleId}.", ex);
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Upgrades custom user colors. No params required.");
        }

        #endregion
    }
}
