using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards
{
    public class KrPermissionRuleDeleteExtension : CardDeleteExtension
    {
        #region Fields

        private readonly IKrPermissionsCacheContainer permissionsCache;
        private readonly IKrPermissionsLockStrategy lockStrategy;

        #endregion

        #region Constructors

        public KrPermissionRuleDeleteExtension(
            IKrPermissionsCacheContainer permissionsCache,
            IKrPermissionsLockStrategy lockStrategy)
        {
            this.permissionsCache = permissionsCache;
            this.lockStrategy = lockStrategy;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardDeleteExtensionContext context)
        {
            var result = await this.lockStrategy.ObtainWriterLockAsync(context.CancellationToken);
            if (result.HasErrors)
            {
                context.ValidationResult.AddError(
                    this,
                    "$KrPermissions_PermissionsDeleteErrorMessage");
            }
            else
            {
                await this.permissionsCache.UpdateVersionAsync(context.CancellationToken);
            }
        }

        #endregion
    }
}
