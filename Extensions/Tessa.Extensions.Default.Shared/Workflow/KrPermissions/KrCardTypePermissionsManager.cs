#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Unity;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <inheritdoc cref="ICardTypePermissionsManager"/>
    /// <remarks>
    /// Тип может быть зарегистрирован для консольной утилиты tadmin, например, для команды tadmin MigrateFiles,
    /// где поднимается серверное API, но только для расширений Tessa.Extensions.Default.Shared.
    /// В этом случае будет отсутствовать зависимость IKrTypesCache, которая здесь помечена как [OptionalDependency]
    /// </remarks>
    public sealed class KrCardTypePermissionsManager :
        ICardTypePermissionsManager
    {
        #region Fields

        private readonly IKrTypesCache? krTypesCache;
        private readonly IDbScope? dbScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="KrCardTypePermissionsManager"/>.
        /// </summary>
        /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        public KrCardTypePermissionsManager(
            [OptionalDependency] IKrTypesCache? krTypesCache = null,
            [OptionalDependency] IDbScope? dbScope = null)
        {
            this.krTypesCache = krTypesCache;
            this.dbScope = dbScope;
        }

        #endregion

        #region ICardTypePermissionsManager Implementation

        /// <inheritdoc/>
        public async ValueTask<bool> CardTypeUseCustomPermissionsAsync(Guid typeID, CancellationToken cancellationToken = default) =>
            this.krTypesCache is not null && await KrComponentsHelper.HasBaseAsync(typeID, this.krTypesCache, cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<bool> CardTypeUseCustomPermissionsOnMetadataAsync(Guid typeID, CancellationToken cancellationToken = default)
        {
            // если krTypesCache равен null, то мы в консольной tadmin, для которой нет смысла вычислять расширенные пермишены
            if (this.dbScope is not null && this.krTypesCache is not null)
            {
                await using var _ = this.dbScope.Create();
                var db = this.dbScope.Db;

                return await db
                    .SetCommand(
                        this.dbScope.BuilderFactory
                            .Select().Top(1).V(true)
                            .From("KrSettingsCardTypes").NoLock()
                            .Where().C("CardTypeID").Equals().P("CardTypeID")
                            .Limit(1)
                            .Build(),
                        db.Parameter("CardTypeID", typeID))
                    .LogCommand()
                    .ExecuteAsync<bool>(cancellationToken);
            }

            return await this.CardTypeUseCustomPermissionsAsync(typeID, cancellationToken);
        }

        #endregion
    }
}
