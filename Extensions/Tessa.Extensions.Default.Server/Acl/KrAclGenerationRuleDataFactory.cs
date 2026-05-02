#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Roles.Acl;
using Tessa.Roles.Acl.Extensions;
using Tessa.Roles.Queries;

namespace Tessa.Extensions.Default.Server.Acl
{
    /// <inheritdoc cref="IAclGenerationRuleData"/>
    public class KrAclGenerationRuleDataFactory : AclGenerationRuleDataFactory
    {
        #region Fields

        /// <inheritdoc cref="IKrTypesCache"/>
        protected readonly IKrTypesCache KrTypesCache;

        #endregion

        #region Constructors

        /// <inheritdoc cref="AclGenerationRuleDataFactory(IAclGenerationRuleExtensionResolver, IDbScope, IComplexQueryBuilderFactory, IRefGroupsManager)"/>
        /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        public KrAclGenerationRuleDataFactory(
            IAclGenerationRuleExtensionResolver extensionsResolver,
            IDbScope dbScope,
            IComplexQueryBuilderFactory getItemsQueryBuilderFactory,
            IRefGroupsManager refGroupsManager,
            IKrTypesCache krTypesCache)
            : base(extensionsResolver, dbScope, getItemsQueryBuilderFactory, refGroupsManager)
        {
            this.KrTypesCache = NotNullOrThrow(krTypesCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<IAclGenerationRuleData> CreateAsync(
            AclGenerationRuleDataSource data,
            CancellationToken cancellationToken = default)
        {
            var result = new KrAclGenerationRuleData(
                data,
                this.ExtensionsResolver,
                this.GetItemsQueryBuilderFactory,
                this.DbScope,
                this.RefGroupsManager,
                this.KrTypesCache);

            await result.InitializeAsync(cancellationToken);

            return result;
        }

        #endregion
    }
}
