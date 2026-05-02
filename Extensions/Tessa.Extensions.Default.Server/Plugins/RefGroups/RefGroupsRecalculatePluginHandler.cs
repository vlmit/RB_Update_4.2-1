#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using Tessa.Platform.RefGroups;

namespace Tessa.Extensions.Default.Server.Plugins.RefGroups
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.RefGroupsRecalculatePlugin"/>
    /// </summary>
    public sealed class RefGroupsRecalculatePluginHandler :
        IPluginHandler
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IRefGroupsManager refGroupsManager;
        private readonly IRefGroupTypesManager refGroupTypesManager;
        private readonly IRefGroupValuesCalculator refGroupValuesCalculator;
        private readonly IRefGroupTypeValuesCalculator refGroupTypeValuesCalculator;

        #endregion

        #region Constructors


        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="refGroupValuesCalculator"><inheritdoc cref="IRefGroupValuesCalculator" path="/summary"/></param>
        /// <param name="refGroupTypeValuesCalculator"><inheritdoc cref="IRefGroupTypeValuesCalculator" path="/summary"/></param>
        /// <param name="refGroupsManager"><inheritdoc cref="IRefGroupsManager" path="/summary"/></param>
        /// <param name="refGroupTypesManager"><inheritdoc cref="IRefGroupTypesManager" path="/summary"/></param>
        public RefGroupsRecalculatePluginHandler(
            IRefGroupValuesCalculator refGroupValuesCalculator,
            IRefGroupTypeValuesCalculator refGroupTypeValuesCalculator,
            IRefGroupsManager refGroupsManager,
            IRefGroupTypesManager refGroupTypesManager)
        {
            this.refGroupsManager = NotNullOrThrow(refGroupsManager);
            this.refGroupTypesManager = NotNullOrThrow(refGroupTypesManager);
            this.refGroupValuesCalculator = NotNullOrThrow(refGroupValuesCalculator);
            this.refGroupTypeValuesCalculator = NotNullOrThrow(refGroupTypeValuesCalculator);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);

            logger.Info("Getting reference group types to calculate.");
            var groupTypes = await this.refGroupTypesManager.GetAllGroupTypesAsync(context.CancellationToken);

            if (groupTypes.Count == 0)
            {
                logger.Info("Reference group types not found. Calculation skipped.");
                return;
            }

            logger.Info($"{groupTypes.Count} reference group types found.");

            foreach (var groupType in groupTypes)
            {
                // Пересчет значений типа групп.
                logger.Info($"Calculate reference group type ID: \"{groupType.ID}\", Name: \"{groupType.Name}\".");
                groupType.CalculatedValues = await this.refGroupTypeValuesCalculator
                    .CalculateAsync(groupType.ID, true, context.ValidationResult, context.CancellationToken);

                // Если ошибка, дальнейший пересчет отменяется. 
                if (!context.ValidationResult.IsSuccessful())
                {
                    logger.Error($"Reference group type ID: \"{groupType.ID}\", Name: \"{groupType.Name}\" wasn't calculated.");
                    logger.LogResult(context.ValidationResult);
                    continue;
                }

                logger.Info($"Reference group type ID: \"{groupType.ID}\", Name: \"{groupType.Name}\" calculated.");

                // Пересчет значений групп, входящих в тип.
                logger.Info($"Getting reference groups of type ID \"{groupType.ID}\" to calculate.");
                var groups = await this.refGroupsManager.GetGroupsByTypeAsync(groupType.ID, context.CancellationToken);

                if (groups.Count == 0)
                {
                    logger.Info($"Reference groups of type ID \"{groupType.ID}\" not found. Calculation skipped.");
                    continue;
                }

                logger.Info($"{groups.Count} reference groups of type ID \"{groupType.ID}\" found.");

                await this.refGroupValuesCalculator.CalculateAllGroupsForTypeAsync(groupType.ID, context.ValidationResult, context.CancellationToken);

                if (!context.ValidationResult.IsSuccessful())
                {
                    logger.Error($"Reference groups of type ID: \"{groupType.ID}\", Name: \"{groupType.Name}\" wasn't calculated.");
                    logger.LogResult(context.ValidationResult);
                    continue;
                }

                logger.Info($"All reference groups of type ID: \"{groupType.ID}\", Name: \"{groupType.Name}\" calculated.");
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new PluginSettings(DefaultPluginNames.RefGroupsRecalculatePlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion
    }
}
