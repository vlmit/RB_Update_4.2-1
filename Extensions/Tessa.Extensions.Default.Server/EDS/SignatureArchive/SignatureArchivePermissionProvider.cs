using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    /// <inheritdoc cref="ISignatureArchivePermissionProvider"/>
    public sealed class SignatureArchivePermissionProvider(
        IDbScope dbScope,
        ISession session,
        ICardCache cardCache)
        : ISignatureArchivePermissionProvider
    {
        #region Private Fields

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        /// <inheritdoc cref="ISession" path="/summary"/>
        private readonly ISession session = NotNullOrThrow(session);

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        #endregion

        #region Constants

        private const string signatureArchiveAdminsTableName = "SignatureArchiveAdministrators";

        #endregion

        #region ISignatureArchivePermissionProvider implementation

        /// <inheritdoc/>
        public async ValueTask<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
        {
            if (this.session.User.ID == Session.SystemID)
            {
                return true;
            }

            var cache = await this.cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, cancellationToken);
            if (cache.IsSuccess
                && cache.GetValue().Sections[signatureArchiveAdminsTableName].Rows.Any(r => r.Get<Guid>("RoleID") == this.session.User.ID))
            {
                return true;
            }

            await using var _ = this.dbScope.Create();
            var builderFactory = await this.dbScope.GetBuilderFactoryAsync(cancellationToken);

            var query = builderFactory.Cached(this, signatureArchiveAdminsTableName, builder => builder
                .Select().Top(1)
                    .V(true)
                .From("RoleUsers", "ru").NoLock()
                .InnerJoin(signatureArchiveAdminsTableName, "t").NoLock()
                    .On().C("t", "RoleID")
                    .Equals()
                    .C("ru", "ID")
                 .Where().C("ru", "UserID")
                    .Equals()
                    .P("UserID")
                .And()
                    .C("ru", "IsDeputy")
                    .Equals()
                    .V(false)
                .Limit(1)
                .Build());

            return await this.dbScope.Db
                .SetCommand(
                    query,
                    this.dbScope.Db.Parameter("UserID", this.session.User.ID, LinqToDB.DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(cancellationToken);
        }
    }

    #endregion
}
