#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Roles.SmartRoles;

namespace Tessa.Extensions.Default.Server.Roles
{
    /// <summary>
    /// Объект экземпляра кеша генератора умных ролей,
    /// которые учитывают обработку триггеров по типам документов.
    /// </summary>
    public class KrSmartRoleGeneratorCacheObject(IDbScope dbScope, IKrTypesCache typesCache) : SmartRoleGeneratorCacheObject
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);

        #endregion

        #region Properties

        /// <summary>
        /// Генераторы умных ролей, разбитые по типам документов, на которые у них есть триггер.
        /// </summary>
        protected Dictionary<Guid, IList<ISmartRoleGenerator>> GeneratorsByTriggerDocType { get; set; } = new();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<IReadOnlyList<ISmartRoleGenerator>> GetGeneratorsByTriggerCardAsync(
            Card? triggerCard,
            CancellationToken cancellationToken = default)
        {
            if (triggerCard is not null
                && this.GeneratorsByTriggerDocType.Count > 0
                && await KrComponentsHelper.HasBaseAsync(triggerCard.TypeID, this.typesCache, cancellationToken))
            {
                var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(triggerCard, this.dbScope, cancellationToken);
                if (docTypeID is not null
                    && this.GeneratorsByTriggerDocType.TryGetValue(docTypeID.Value, out var triggers))
                {
                    return triggers.AsReadOnlyList();
                }
            }

            return await base.GetGeneratorsByTriggerCardAsync(triggerCard, cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask InitializeAsync(
            IReadOnlyCollection<ISmartRoleGenerator> allGenerators,
            CancellationToken cancellationToken)
        {
            await base.InitializeAsync(allGenerators, cancellationToken);

            var docTypes = new HashSet<Guid, KrDocType>(x => x.ID, await this.typesCache.GetDocTypesAsync(cancellationToken));
            List<KrDocType>? docTypesToMove = null;
            if (this.GeneratorsByTriggerCardType is not null)
            {
                foreach (var (typeID, _) in this.GeneratorsByTriggerCardType)
                {
                    if (!docTypes.TryGetItem(typeID, out var docType))
                    {
                        continue;
                    }

                    docTypesToMove ??= [];
                    docTypesToMove.Add(docType);
                }
            }

            if (docTypesToMove is not null)
            {
                foreach (var docType in docTypesToMove)
                {
                    if (this.GeneratorsByTriggerCardType is null || !this.GeneratorsByTriggerCardType.Remove(docType.ID, out var generators))
                    {
                        continue;
                    }

                    if (this.GeneratorsByTriggerCardType.TryGetValue(docType.CardTypeID, out var cardTypeRules))
                    {
                        generators.AddRange(cardTypeRules.Where(x => !generators.Contains(x)));
                    }

                    this.GeneratorsByTriggerDocType[docType.ID] = generators;
                }
            }
        }

        #endregion
    }
}
