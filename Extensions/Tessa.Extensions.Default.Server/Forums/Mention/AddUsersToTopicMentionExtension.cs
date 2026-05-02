#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Tessa.Forums;
using Tessa.Forums.Mentions;

namespace Tessa.Extensions.Default.Server.Forums.Mention
{
    /// <summary>
    /// Расширение на упоминание пользователей в обсуждении,
    /// которое добавляет вновь упомянутых пользователей в обсуждение
    /// и обновляет пользователей с правами "только для чтения".
    /// </summary>
    public class AddUsersToTopicMentionExtension : ForumUserMentionExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override async Task ProcessMention(IForumUserMentionExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var usersIDsToAdd = context.UserModels
                .Where(x => x.New || !x.Personal)
                .Select(x => x.UserID)
                .ToArray();

            if (usersIDsToAdd is { Length: > 0 })
            {
                var (_, result) = await context.ForumProvider.AddParticipantsAsync(
                    context.TopicID,
                    usersIDsToAdd,
                    isReadOnly: false,
                    canEditMessages: true,
                    isSubscribed: true,
                    serviceMessageMode: ForumServiceMessageMode.MessageWithoutNotification,
                    cancellationToken: context.CancellationToken);

                context.ValidationResult.Add(result);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }
            }

            var usersIDsToUpdate = context.UserModels
                .Where(x => x is { ReadOnly: true, Personal: true, New: false })
                .Select(x => x.UserID)
                .ToArray();

            if (usersIDsToUpdate is { Length: > 0 })
            {
                var result = await context.ForumProvider.UpdateParticipantsAsync(
                    context.TopicID,
                    usersIDsToUpdate,
                    isReadOnly: false,
                    canEditMessages: true,
                    isSubscribed: true,
                    cancellationToken: context.CancellationToken);

                context.ValidationResult.Add(result);
            }
        }

        #endregion
    }
}
