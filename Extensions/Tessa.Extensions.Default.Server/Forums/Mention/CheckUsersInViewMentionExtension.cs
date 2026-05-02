#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Forums;
using Tessa.Forums.Mentions;
using Tessa.Normalization;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Forums.Mention
{
    /// <summary>
    /// Расширение на упоминание пользователей в обсуждении, которое проверяет,
    /// могут ли быть упомянуты указанные пользователи.
    /// </summary>
    /// <param name="mentionSettings"><inheritdoc cref="IForumUserMentionSettings" path="/summary"/></param>
    /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
    /// <param name="normalizationBatchProcessor"><inheritdoc cref="INormalizationBatchProcessor" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    public class CheckUsersInViewMentionExtension(
        IForumUserMentionSettings mentionSettings,
        IViewService viewService,
        INormalizationBatchProcessor normalizationBatchProcessor,
        ICardMetadata cardMetadata) : ForumUserMentionExtension
    {
        #region Private Fields

        private readonly IForumUserMentionSettings mentionSettings = NotNullOrThrow(mentionSettings);

        private readonly IViewService viewService = NotNullOrThrow(viewService);

        private readonly INormalizationBatchProcessor normalizationBatchProcessor = NotNullOrThrow(normalizationBatchProcessor);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequest(IForumUserMentionExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var newlyMentioned = context.UserModels
                .Where(x => x.New || x.ReadOnly)
                .Select(x => x.UserID)
                .ToArray();

            if (newlyMentioned is not { Length: > 0 })
            {
                return;
            }

            var view = await this.viewService.GetByNameAsync(this.mentionSettings.PermissionsViewAlias, context.CancellationToken);

            if (view is null)
            {
                context.ValidationResult.AddError(this, await LocalizeFormatAsync(
                    "$UI_Cards_Exception_ViewNotFound",
                    this.mentionSettings.PermissionsViewAlias));
                return;
            }

            // Вызываем представление в цикле для каждого пользователя отдельно, т.к. план запроса оптимальнее для одного значения в параметре.
            foreach (var userID in newlyMentioned)
            {
                var request = new TessaViewRequest(view.Alias);

                request.Parameters.Add(
                    new RequestParameter(this.mentionSettings.UserIDParamAlias)
                        .Add(EqualsToCriteriaOperator.Instance, userID));

                if (!string.IsNullOrWhiteSpace(this.mentionSettings.TopicIDParamAlias))
                {
                    request.Parameters.Add(
                        new RequestParameter(this.mentionSettings.TopicIDParamAlias)
                            .Add(EqualsToCriteriaOperator.Instance, context.TopicID));
                }

                var response = await view.GetDataAsync(request, context.CancellationToken);
                var idIndex = response.GetColumnIndex(this.mentionSettings.UserIDColumnAlias);

                if (idIndex >= 0
                    && response.Rows.Any(x => x[idIndex] is Guid userIDFromView && userIDFromView == userID))
                {
                    continue;
                }

                var userName = await this.GetUserNameByIDAsync(
                    userID,
                    context.DbScope,
                    context.ValidationResult,
                    context.CancellationToken);

                context.ValidationResult.Add(
                    ForumValidationKeys.NoRightsToMentionUserError,
                    ValidationResultType.Error,
                    await LocalizeFormatAsync("$Forum_ValidationMessage_CantMentionUserFormat", userName));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Получает имя пользователя по его идентификатору.
        /// </summary>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Имя пользователя.</returns>
        private async Task<string> GetUserNameByIDAsync(
            Guid userID,
            IDbScope dbScope,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var batchRequest = await this.normalizationBatchProcessor.CreateRequestAsync(cancellationToken);
            var userIDKey = new NormalizationKey(userID);
            var info = await this.cardMetadata.GetNormalizationInfoAsync(cancellationToken);
            batchRequest.Add(info.UsersSourceID, userIDKey);

            var batchResponse = await this.normalizationBatchProcessor.ProcessAsync(batchRequest, cancellationToken);
            validationResult.Add(batchResponse.Result.ConvertToSuccessful());
            return (batchResponse.TryGet(info.UsersSourceID, userIDKey) ?? userID.ToString())!;
        }

        #endregion
    }
}
