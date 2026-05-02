using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;
using Tessa.Views;
using Tessa.Views.Json;
using Tessa.Views.Json.Converters;
using Unity;

namespace Tessa.Extensions.Default.Console.Scripts
{
    /// <summary>
    /// Скрипт для апгрейда версии JSON представлений и исправления типов параметров в представлениях в БД.
    /// </summary>
    [ConsoleScript]
    public sealed class UpgradeViewsSql :
        ServerConsoleScriptBase
    {
        #region Base Overrides

        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            var viewModelConverter = this.Container.Resolve<IJsonViewModelConverter>();
            var jsonViewModelUpgrader = this.Container.Resolve<IJsonViewModelUpgrader>();

            var viewDataAccessor = this.Container.Resolve<ViewDataAccessor>();
            var views = await viewDataAccessor.GetViewsAsync(false, cancellationToken);

            await using var db = await this.CreateDbManagerAsync(cancellationToken);
            var builderFactory = new QueryBuilderFactory(db.Dbms);

            foreach (var view in views)
            {
                var jsonViewModel = viewModelConverter.ConvertToJsonViewModel(view);
                if (string.IsNullOrEmpty(jsonViewModel.JsonMetadataSource))
                {
                    await this.Logger.WriteLineAsync($"View \"{jsonViewModel.Alias}\" is not in JSON format.");
                }

                var shouldUpdateMetadata = false;
                var result = new ValidationResultBuilder();
                if (await jsonViewModelUpgrader.UpgradeAsync(jsonViewModel, repairTypes: false, result, jsonIndented: false, cancellationToken) || !result.IsSuccessful())
                {
                    await this.Logger.WriteLineAsync($"Upgrading JSON format metadata for view \"{jsonViewModel.Alias}\".");
                    if (!result.IsSuccessful())
                    {
                        this.Result = -1;
                        await this.Logger.LogResultAsync(result.Build());
                    }
                    else
                    {
                        shouldUpdateMetadata = true;
                    }
                }

                if (!shouldUpdateMetadata)
                {
                    continue;
                }

                await db
                    .SetCommand(
                        builderFactory
                            .Update("Views")
                            .C("JsonMetadataSource").Equals().P("JsonMetadataSource")
                            .Where().C("ID").Equals().P("ID")
                            .Build(),
                        db.Parameter("ID", jsonViewModel.ID, DataType.Guid),
                        db.Parameter("JsonMetadataSource", jsonViewModel.JsonMetadataSource, DataType.BinaryJson))
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);
            }
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Upgrades views metadata JSON format in the table \"Views\". No params required.");
        }

        #endregion
    }
}
