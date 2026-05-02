#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.StageTypeRequests
{
    /// <summary>
    /// Расширение на сохранение карточки диалога. Устанавливает флаг, отменяющий закрытие диалога, если обработка диалога была прервана в сценарии диалога.
    /// </summary>
    public sealed class DialogStoreExtension :
        CardStoreExtension
    {
        #region Base Overrides

        /// <inheritdoc />
        public override Task AfterRequest(
            ICardStoreExtensionContext context)
        {
            // Это случай основной карточки. С помощью ключа в ValidationResult передаем признак в ответ о том
            // что необходимо не закрывать окно диалога.
            var hasDialog = context.ValidationResult.RemoveAll(DefaultValidationKeys.CancelDialog);
            if (hasDialog != 0)
            {
                context.Response!.SetKeepTaskDialog();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
