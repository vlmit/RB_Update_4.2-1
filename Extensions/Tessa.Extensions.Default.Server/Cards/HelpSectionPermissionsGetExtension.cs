#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Расширение для выдачи прав на чтение карточки "Раздел справки" при открытии карточки по ссылке "?".
    /// </summary>
    public sealed class HelpSectionPermissionsGetExtension :
        CardGetExtension
    {
        #region Fields

        private readonly ICardTypePermissionsManager permissionsManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="HelpSectionPermissionsGetExtension"/>.
        /// </summary>
        /// <param name="permissionsManager"><inheritdoc cref="ICardTypePermissionsManager" path="/summary"/></param>
        public HelpSectionPermissionsGetExtension(ICardTypePermissionsManager permissionsManager) =>
            this.permissionsManager = NotNullOrThrow(permissionsManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(ICardGetExtensionContext context)
        {
            if (!context.Request.CardID.HasValue
                || !context.Request.CardTypeID.HasValue
                || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            // Карточка открывается по "?" в виде диалога справки
            if (context.Request.Info.ContainsKey(CardHelper.HelpSectionDialogName)
                && context.Request.Info.ContainsKey(CardHelper.HelpSectionLanguage))
            {
                // Если карточка раздела справки не добавлена в типовое решение, то она будет административной и для нее будет разрешено только чтение
                if (await this.permissionsManager.CardTypeUseCustomPermissionsAsync(context.Request.CardTypeID.Value, context.CancellationToken))
                {
                    context.Info.GetOrCreateServerToken().AddPermission(KrPermissionFlagDescriptors.ReadCard);
                }
            }
            // Карточка открывается в обычном режиме (во вкладке)
            else if (!context.Session.User.IsAdministrator()
                && !await this.permissionsManager.CardTypeUseCustomPermissionsAsync(context.Request.CardTypeID.Value, context.CancellationToken))
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .Error(ValidationKeys.UserIsNotAdmin)
                    .End();
            }
        }

        #endregion
    }
}
