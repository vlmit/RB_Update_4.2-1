#nullable enable

using LinqToDB.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Files;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.TextRecognition.Constants;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Расширение, в котором выполняется проверка прав на исходную карточку и файл.
    /// </summary>
    /// <remarks>
    /// Создает экземпляр класса <see cref="OcrOperationPermissionsGetExtension"/>.
    /// </remarks>
    /// <param name="permissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public sealed class OcrOperationPermissionsGetExtension(IKrPermissionsManager permissionsManager)
        : CardGetExtension
    {
        #region Private Fields

        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(ICardGetExtensionContext context)
        {
            if (!context.Request.CardID.HasValue
                || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var cardFileRequest = await TryGetFileContentRequestAsync(
                context.DbScope!,
                context.Request.CardID.Value,
                context.CancellationToken);

            if (cardFileRequest?.CardID is null)
            {
                return;
            }

            try
            {
                // проверяем доступ по основной карточке
                await KrFileAccessHelper.CheckAccessAsync(
                    cardFileRequest,
                    context,
                    cardFileRequest.CardID.Value,
                    this.permissionsManager,
                    context.CancellationToken);
            }
            finally
            {
                // чтобы типовое расширение на проверку прав не проверяло токен не от своей карточки
                KrToken.Remove(context.Request.Info);
            }
        }

        /// <inheritdoc/>
        public override Task AfterRequest(ICardGetExtensionContext context)
        {
            if (context.ValidationResult.IsSuccessful()
                && context.Response?.Card is { } card
                && card.Sections.TryGetValue(nameof(OcrOperations), out var operations)
                && operations.Fields.TryGet<Guid?>(OcrOperations.CardID).HasValue)
            {
                card.Permissions.Sections.GetOrAddEntry(nameof(OcrOperations))
                    .SetFieldPermissions(OcrOperations.DocTypeID, CardPermissionFlags.ProhibitModify)
                    .SetFieldPermissions(OcrOperations.DocTypeTitle, CardPermissionFlags.ProhibitModify);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private static async Task<CardGetFileContentRequest?> TryGetFileContentRequestAsync(
            IDbScope dbScope,
            Guid ocrCardId,
            CancellationToken cancellationToken = default)
        {
            await using (dbScope.Create())
            {
                var parameter = DataParameter.Guid(nameof(ocrCardId), ocrCardId);

                var query = dbScope.BuilderFactory
                    .Select()
                        .C(OcrOperations.CardID).As(nameof(CardGetFileContentRequest.CardID))
                        .C(OcrOperations.CardTypeID).As(nameof(CardGetFileContentRequest.CardTypeID))
                        .C(OcrOperations.CardTypeName).As(nameof(CardGetFileContentRequest.CardTypeName))
                        .C(OcrOperations.FileID).As(nameof(CardGetFileContentRequest.FileID))
                        .C(OcrOperations.FileName).As(nameof(CardGetFileContentRequest.FileName))
                        .C(OcrOperations.FileTypeID).As(nameof(CardGetFileContentRequest.FileTypeID))
                        .C(OcrOperations.FileTypeName).As(nameof(CardGetFileContentRequest.FileTypeName))
                        .C(OcrOperations.VersionRowID).As(nameof(CardGetFileContentRequest.VersionRowID))
                    .From(nameof(OcrOperations)).NoLock()
                    .Where().C(OcrOperations.ID).Equals().P(parameter.Name!)
                    .Build();

                return await dbScope.Db
                    .SetCommand(query, parameter)
                    .LogCommand()
                    .ExecuteAsync<CardGetFileContentRequest>(cancellationToken);
            }
        }

        #endregion
    }
}
