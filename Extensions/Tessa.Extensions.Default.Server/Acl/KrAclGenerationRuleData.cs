#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Internal.Common;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Roles.Acl;
using Tessa.Roles.Acl.Extensions;
using Tessa.Roles.Queries;
using Tessa.Roles.Queries.Parts;
using Tessa.Roles.Triggers;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Acl
{
    /// <inheritdoc cref="IAclGenerationRuleData"/>
    public class KrAclGenerationRuleData : AclGenerationRuleData
    {
        #region Fields

        private readonly IKrTypesCache krTypesCache;

        #endregion

        #region Constructors

        /// <inheritdoc cref="AclGenerationRuleData(AclGenerationRuleDataSource,IAclGenerationRuleExtensionResolver,IComplexQueryBuilderFactory,IDbScope,IRefGroupsManager)"/>
        public KrAclGenerationRuleData(
            AclGenerationRuleDataSource source,
            IAclGenerationRuleExtensionResolver extensionsResolver,
            IComplexQueryBuilderFactory getItemsQueryBuilderFactory,
            IDbScope dbScope,
            IRefGroupsManager refGroupsManager,
            IKrTypesCache krTypesCache)
            : base(
                  source,
                  extensionsResolver,
                  getItemsQueryBuilderFactory,
                  dbScope,
                  refGroupsManager)
        {
            this.krTypesCache = NotNullOrThrow(krTypesCache);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Список типов документов правила расчёта ACL.
        /// </summary>
        public HashSet<Guid> DocTypes { get; } = new();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask InitializeTypesAsync(
            List<Guid> types,
            CancellationToken cancellationToken = default)
        {
            var allDocTypes = await this.krTypesCache.GetDocTypesAsync(cancellationToken);
            var docTypesToAdd = new HashSet<KrDocType>();
            var cardTypesToAdd = new List<Guid>();

            foreach (var typeID in types)
            {
                var group = await this.RefGroupsManager.GetGroupAsync(typeID, RefGroupsHelper.DocumentTypeRefGroupTypeID, cancellationToken);
                if (group is not null)
                {
                    if (group.CalculatedValues is not { Count: > 0 })
                    {
                        continue;
                    }

                    foreach (Guid groupValue in group.CalculatedValues.Select(x => x.ID))
                    {
                        if (allDocTypes.TryFirst(x => x.ID == groupValue, out var docType))
                        {
                            docTypesToAdd.Add(docType);
                        }
                        else
                        {
                            this.CardTypes.Add(groupValue);
                        }
                    }
                }
                else if (allDocTypes.TryFirst(x => x.ID == typeID, out var docType))
                {
                    docTypesToAdd.Add(docType);
                }
                else
                {
                    cardTypesToAdd.Add(typeID);
                }
            }

            await base.InitializeTypesAsync(cardTypesToAdd, cancellationToken);

            foreach (var docType in docTypesToAdd)
            {
                this.DocTypes.Add(docType.ID);
            }
        }

        /// <inheritdoc/>
        protected override async ValueTask InitializeGetAllCardsQueryAsync(CancellationToken cancellationToken = default)
        {
            // Если только типы карточек, то вызываем типовую реализацию.
            if (this.DocTypes is not { Count: > 0 })
            {
                await base.InitializeGetAllCardsQueryAsync(cancellationToken);
                return;
            }

            var builder = this.GetItemsQueryBuilderFactory.Create();
            builder.AddMainQuery(
                new IComplexQueryPart[]
                {
                    new FuncQueryPart((b, _) => b.Select().C(nameof(AclGenerationRuleData), Names.Instances_ID, AclHelper.TypeIDKey)),
                    new ExtensionResultPlaceholder(),
                    new FuncQueryPart((builder, dataParameters) =>
                    {
                        builder
                            .From()
                            .StartExpression(true)
                                .Select()
                                    .C(KrConstants.DocumentCommonInfo.Name, KrConstants.DocumentCommonInfo.ID)
                                    .C(KrConstants.DocumentCommonInfo.Name, KrConstants.DocumentCommonInfo.DocTypeID).As(AclHelper.TypeIDKey)
                                .From(KrConstants.DocumentCommonInfo.Name).NoLock()
                                .Where().C(KrConstants.DocumentCommonInfo.DocTypeID).InArray(this.DocTypes, "DocTypes", out var arrayParameter);
                        dataParameters.Add(arrayParameter);

                        if (this.CardTypes is { Count: > 0 })
                        {
                            builder
                                .Union()
                                .Select()
                                    .C(Names.Instances, Names.Instances_ID)
                                    .C(Names.Instances, Names.Instances_TypeID).As(AclHelper.TypeIDKey)
                                .From(Names.Instances).NoLock()
                                .Where().C(Names.Instances, Names.Instances_TypeID).InArray(this.CardTypes, "CardTypes", out var arrayParameter2);
                                dataParameters.Add(arrayParameter2);
                        }

                        builder.EndExpression(true).As(nameof(AclGenerationRuleData));
                    }, true),
                    new ExtensionJoinPlaceholder(nameof(AclGenerationRuleData), Names.Instances_ID),
                    ConstParts.WhereStartedPart,
                    new ExtensionFilterPlaceholder()
                });

            await this.ModifyBuilderWithExtensionsAsync(builder, cancellationToken);

            this.GetAllCardsQuery = await builder.BuildAsync(cancellationToken);
        }

        /// <inheritdoc/>
        protected override async ValueTask ModifyBuilderWithRuleAsync(IComplexQueryBuilder builder, CancellationToken cancellationToken = default)
        {
            // Если только типы карточек, то вызываем типовую реализацию.
            if (this.DocTypes is not { Count: > 0 })
            {
                await base.ModifyBuilderWithRuleAsync(builder, cancellationToken);
                return;
            }

            builder.AddSubQueryJoin(
                nameof(AclGenerationRuleData),
                new FuncQueryPart((builder, dataProperties) =>
                {
                    builder
                        .StartExpression()
                        .Select()
                            .C(KrConstants.DocumentCommonInfo.Name, KrConstants.DocumentCommonInfo.ID)
                            .C(KrConstants.DocumentCommonInfo.Name, KrConstants.DocumentCommonInfo.DocTypeID).As(AclHelper.TypeIDKey)
                        .From(KrConstants.DocumentCommonInfo.Name).NoLock()
                        .Where().C(KrConstants.DocumentCommonInfo.Name, KrConstants.DocumentCommonInfo.DocTypeID).InArray(this.DocTypes, "DocTypes", out var arrayParameter);
                    dataProperties.Add(arrayParameter);

                    if (this.CardTypes is { Count: > 0 })
                    {
                        builder
                            .Union()

                            .Select()
                                .C(Names.Instances, Names.Instances_ID)
                                .C(Names.Instances, Names.Instances_TypeID).As(AclHelper.TypeIDKey)
                            .From(Names.Instances).NoLock()
                            .Where().C(Names.Instances, Names.Instances_TypeID).InArray(this.CardTypes, "CardTypes", out var arrayParameter2);
                        dataProperties.Add(arrayParameter2);
                    }

                    builder.EndExpression().As(nameof(AclGenerationRuleData));
                }, true),
                Names.Instances_ID,
                false);
        }

        /// <inheritdoc/>
        protected override async ValueTask InitializeTriggersAsync(CancellationToken cancellationToken = default)
        {
            await base.InitializeTriggersAsync(cancellationToken);

            foreach (var trigger in this.Triggers)
            {
                for (int i = trigger.Types.Count - 1; i >= 0; i--)
                {
                    var typeID = trigger.Types[i];
                    var group = await this.RefGroupsManager.GetGroupAsync(typeID, RefGroupsHelper.DocumentTypeRefGroupTypeID, cancellationToken);
                    if (group is not null)
                    {
                        trigger.Types.RemoveAt(i);
                        if (group.CalculatedValues is not { Count: > 0 })
                        {
                            continue;
                        }

                        trigger.Types.AddRange(group.CalculatedValues.Select(x => (Guid) x.ID));
                    }
                }
                trigger.Types.RemoveDuplicates();

                trigger.CheckTriggerCardAsyncFunc = this.CheckTriggerAsync;
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask<bool> CheckTriggerAsync(UpdateTrigger trigger, Card triggerCard, CancellationToken cancellationToken)
        {
            if (trigger.Types.Contains(triggerCard.TypeID))
            {
                return true;
            }

            var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(triggerCard, this.DbScope, cancellationToken);
            return docTypeID is not null && trigger.Types.Contains(docTypeID.Value);
        }

        #endregion
    }
}
