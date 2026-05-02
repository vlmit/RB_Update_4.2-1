using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow
{
    /// <summary>
    /// При сохранении карточки шаблона этапа и карточки группы этапов устанавливает блокировку на изменение шаблона этапа.
    /// </summary>
    /// <param name="lockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy"/></param>
    public sealed class KrStageCardTemplateStoreExtension
        (IKrStageTemplateLockStrategy lockStrategy) : CardStoreExtension
    {
        #region Fields

        private readonly IKrStageTemplateLockStrategy lockStrategy = NotNullOrThrow(lockStrategy);

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardStoreExtensionContext context)
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
