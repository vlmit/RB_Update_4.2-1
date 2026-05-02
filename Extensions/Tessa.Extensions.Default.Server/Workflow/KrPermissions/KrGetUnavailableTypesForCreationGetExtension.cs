using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Расширение на запрос <see cref="Tessa.Extensions.Default.Shared.DefaultRequestTypes.GetUnavailableTypes"/>,
    /// которое рассчитывает список типов карточек и типов документов, недоступных для создания пользователем. 
    /// </summary>
    public sealed class KrGetUnavailableTypesForCreationGetExtension : CardRequestExtension
    {
        #region Fields

        private readonly IKrTypesCache typesCache;
        private readonly IKrPermissionsManager permissionsManager;
        private readonly IKrPermissionsCacheContainer permissionsCacheContainer;

        #endregion

        #region Constructors

        public KrGetUnavailableTypesForCreationGetExtension(
            IKrTypesCache typesCache,
            IKrPermissionsManager permissionsManager,
            IKrPermissionsCacheContainer permissionsCacheContainer)
        {
            ThrowIfNull(typesCache);
            ThrowIfNull(permissionsManager);
            ThrowIfNull(permissionsCacheContainer);

            this.typesCache = typesCache;
            this.permissionsManager = permissionsManager;
            this.permissionsCacheContainer = permissionsCacheContainer;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            var cardTypes = await this.typesCache.GetCardTypesAsync(context.CancellationToken);
            var docTypes = await this.typesCache.GetDocTypesAsync(context.CancellationToken);

            var permissionsCache = await this.permissionsCacheContainer.TryGetCacheAsync(context.Response.ValidationResult, context.CancellationToken);
            var getCacheSuccessful = context.Response.ValidationResult.IsSuccessful();

            List<object> unavaibleTypes = new List<object>();
            await using (context.DbScope.Create())
            {
                foreach (var cardType in cardTypes)
                {
                    if (!cardType.UseDocTypes)
                    {
                        if (getCacheSuccessful)
                        {
                            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                                new KrPermissionsCreateContextParams
                                {
                                    CardTypeID = cardType.ID,
                                    AdditionalInfo = context.Info,
                                    ExtensionContext = context,
                                    ServerToken = context.Info.TryGetServerToken(),
                                    PermissionsCache = permissionsCache,
                                    ServiceType = context.Request.ServiceType,
                                },
                                cancellationToken: context.CancellationToken);

                            if (permContextResult.Status == KrPermissionsCreateContextStatus.Fail
                                || !await this.permissionsManager.CheckRequiredPermissionsAsync(
                                    permContextResult.Context,
                                    KrPermissionFlagDescriptors.CreateCard))
                            {
                                unavaibleTypes.Add(cardType.ID);
                            }
                        }
                        else
                        {
                            unavaibleTypes.Add(cardType.ID);
                        }
                    }
                }

                foreach (var docType in docTypes)
                {
                    if (getCacheSuccessful)
                    {
                        var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                            new KrPermissionsCreateContextParams
                            {
                                CardTypeID = docType.CardTypeID,
                                DocTypeID = docType.ID,
                                AdditionalInfo = context.Info,
                                ExtensionContext = context,
                                ServerToken = context.Info.TryGetServerToken(),
                                PermissionsCache = permissionsCache,
                                ServiceType = context.Request.ServiceType,
                            },
                            cancellationToken: context.CancellationToken);

                        if (permContextResult.Status == KrPermissionsCreateContextStatus.Fail
                            || !await this.permissionsManager.CheckRequiredPermissionsAsync(
                                permContextResult.Context,
                                KrPermissionFlagDescriptors.CreateCard))
                        {
                            unavaibleTypes.Add(docType.ID);
                        }
                    }
                    else
                    {
                        unavaibleTypes.Add(docType.ID);
                    }
                }
            }

            context.Response.Info.Add(KrPermissionsHelper.UnavailableTypesKey, unavaibleTypes.ToArray());
        }

        #endregion
    }
}
