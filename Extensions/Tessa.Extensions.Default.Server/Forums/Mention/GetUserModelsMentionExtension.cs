#nullable enable

using System.Threading.Tasks;
using Tessa.Forums.Mentions;

namespace Tessa.Extensions.Default.Server.Forums.Mention
{
    /// <summary>
    /// Расширение на упоминание пользователей в обсуждении, которое преобразует список идентификаторов пользователей
    /// в модель <see cref="ForumUserMentionModel"/> для дальнейшего использования в расширениях.
    /// </summary>
    public class GetUserModelsMentionExtension(
        IForumUserMentionStrategy userMentionStrategy) : ForumUserMentionExtension
    {
        #region Private Fields

        private readonly IForumUserMentionStrategy userMentionStrategy = NotNullOrThrow(userMentionStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequest(IForumUserMentionExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            context.UserModels = await this.userMentionStrategy.GetTopicParticipantModelsAsync(
                context.TopicID,
                context.UserIDs,
                context.CancellationToken);
        }

        #endregion
    }
}
