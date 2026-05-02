#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Scheme;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.Views
{
    /// <summary>
    /// Перехватчик представления с удалёнными файлами, который
    /// выполняет проверку параметров в запросе перед получением данных.
    /// </summary>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="cardFileBackupSettings"><inheritdoc cref="ICardFileBackupSettings" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    /// <param name="cardTypePermissionsManager"><inheritdoc cref="ICardTypePermissionsManager" path="/summary"/></param>
    public sealed class DeletedFilesViewInterceptor(
        IDbScope dbScope,
        ISession session,
        ICardFileBackupSettings cardFileBackupSettings,
        IKrPermissionsManager krPermissionsManager,
        ICardTypePermissionsManager cardTypePermissionsManager)
        : ViewInterceptorBase(["DeletedFiles"])
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ISession session = NotNullOrThrow(session);

        private readonly ICardFileBackupSettings cardFileBackupSettings = NotNullOrThrow(cardFileBackupSettings);

        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        private readonly ICardTypePermissionsManager cardTypePermissionsManager = NotNullOrThrow(cardTypePermissionsManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            var metadata = await view.GetMetadataAsync(cancellationToken);

            if (request.GetParameterCriteriaCount("Card") is not 1
                || !await this.cardFileBackupSettings.CanDeleteWithBackupAsync(cancellationToken: cancellationToken))
            {
                return string.IsNullOrEmpty(request.SubsetName) // из подмножеств только Count
                    ? new(metadata)
                    : new TessaViewResult { Columns = { (NotNullOrThrow(metadata.RowCountSubset), SchemeType.Int64) } };
            }

            if (request.Parameters.IsDefinedByName("ShowAll")
                && (request.GetFirstParameterValue<Guid?>("Card") is not { } cardID
                    || await this.GetCardTypeIDAsync(cardID, cancellationToken) is not { } cardTypeID
                    || !await this.CanRestoreAllFilesAsync(cardID, cardTypeID, cancellationToken)))
            {
                request.Parameters.RemoveAllByName("ShowAll");
            }

            return await view.GetDataAsync(request, cancellationToken);
        }

        #endregion

        #region Private Methods

        private async Task<Guid?> GetCardTypeIDAsync(Guid cardID, CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();

            var parameter = DataParameter.Guid("ID", cardID);

            var query = this.dbScope.BuilderFactory
                .Select().C("TypeID")
                .From(Names.Instances).NoLock()
                .Where().C("ID").Equals().P(parameter.Name!)
                .Build();

            return await this.dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<Guid?>(cancellationToken);
        }

        private async Task<bool> CanRestoreAllFilesAsync(Guid cardID, Guid cardTypeID, CancellationToken cancellationToken)
        {
            if (!await this.cardTypePermissionsManager.CardTypeUseCustomPermissionsAsync(cardTypeID, cancellationToken))
            {
                return this.session.User.IsAdministrator();
            }

            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams { CardID = cardID, CardTypeID = cardTypeID },
                cancellationToken);

            return permissionsContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.RestoreAllDeletedFiles),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => this.session.User.IsAdministrator(),
                _ => throw ArgumentOutOfRange(permissionsContextResult.Status)
            };
        }

        #endregion
    }
}
