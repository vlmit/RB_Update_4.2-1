#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Forums;
using Tessa.Platform.Validation;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.Views
{
    public sealed class TopicParticipantsInterceptor(
        IForumPermissionsProvider forumPermissionsProvider)
        : ViewInterceptorBase([ForumHelper.TopicParticipantsView])
    {
        #region Fields

        private readonly IForumPermissionsProvider forumPermissionsProvider = NotNullOrThrow(forumPermissionsProvider);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            if (request.Parameters.FindByName(ForumHelper.TopicIDParam)?.CriteriaValues.FirstOrDefault()?.Values.FirstOrDefault()?.Value is not Guid topicId)
            {
                return new TessaViewResult(await view.GetMetadataAsync(cancellationToken));
            }

            var (_, validationResult) = await this.forumPermissionsProvider.ResolveUserPermissionsAsync(
                topicId, checkElevatedPermissions: true, cancellationToken: cancellationToken);

            if (!validationResult.IsSuccessful)
            {
                throw new ValidationException(validationResult);
            }

            return await view.GetDataAsync(request, cancellationToken);
        }

        #endregion
    }
}
