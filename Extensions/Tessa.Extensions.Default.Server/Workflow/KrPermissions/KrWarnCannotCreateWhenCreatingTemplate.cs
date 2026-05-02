using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Расширение проверяет воможность создания карточки текущим пользователем по создаваемому
    /// шаблону и, если прав для создание карточки по этому шаблону недостаточно, предупреждает
    /// пользователя об этом.
    /// </summary>
    public sealed class KrWarnCannotCreateWhenCreatingTemplate : CardNewExtension
    {
        #region Fields

        private readonly IKrPermissionsManager permissionsManager;

        #endregion

        #region Constructors

        public KrWarnCannotCreateWhenCreatingTemplate(IKrPermissionsManager permissionsManager)
        {
            ThrowIfNull(permissionsManager);

            this.permissionsManager = permissionsManager;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequest(ICardNewExtensionContext context)
        {
            Card template;
            if (context.CardType is null
                || context.CardType.InstanceType != CardInstanceType.Card
                || context.CardType.Flags.Has(CardTypeFlags.Singleton)
                || (template = context.Request.TryGetTemplateCard()) is null)
            {
                return;
            }

            KrProcessSharedHelper.TryGetDocTypeID(template, out var docTypeID);
            var permContextResult = await permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardTypeID = template.TypeID,
                    DocTypeID = docTypeID,
                    AdditionalInfo = context.Info,
                    PrevToken = KrToken.TryGet(context.Request.Info),
                    ExtensionContext = context,
                    ServerToken = context.Info.TryGetServerToken(),
                    ServiceType = context.Request.ServiceType,
                },
                cancellationToken: context.CancellationToken);

            switch (permContextResult.Status)
            {
                case KrPermissionsCreateContextStatus.Success:
                var result = await permissionsManager.CheckRequiredPermissionsAsync(
                    permContextResult.Context,
                    KrPermissionFlagDescriptors.CreateCard);

                    if (!result)
                    {
                        context.ValidationResult.AddWarning(this, "$KrMessages_WarnCantCreateCardBasedOnTemplate");
                    }
                    break;

                case KrPermissionsCreateContextStatus.Fail:
                    context.ValidationResult.AddWarning(this, "$KrMessages_WarnCantCreateCardBasedOnTemplate");
                    context.ValidationResult.Add(permContextResult.ValidationResult.ConvertToSuccessful());
                    break;
            }
        }

        #endregion
    }
}
