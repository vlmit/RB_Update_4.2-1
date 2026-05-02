using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow
{
    /// <summary>
    /// При удалении карточки шаблона этапа и карточки группы этапов устанавливает блокировку на изменение шаблона этапа.
    /// </summary>
    /// <param name="lockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy"/></param>
    public sealed class KrStageCardTemplateDeleteExtension
        (IKrStageTemplateLockStrategy lockStrategy) : CardDeleteExtension
    {
        #region Fields

        private readonly IKrStageTemplateLockStrategy lockStrategy = NotNullOrThrow(lockStrategy);

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardDeleteExtensionContext context)
        {
            var result = await this.lockStrategy.ObtainWriterLockAsync(context.CancellationToken);
            if (!result.IsSuccessful)
            {
                context.ValidationResult.AddError(this, "$KrMessages_StageTemplatesStoreErrorMessage");
                return;
            }
        }

        #endregion
    }
}
