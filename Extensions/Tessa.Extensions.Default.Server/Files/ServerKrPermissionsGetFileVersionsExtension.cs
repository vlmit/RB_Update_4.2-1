using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Files
{
    public sealed class ServerKrPermissionsGetFileVersionsExtension(IKrPermissionsManager permissionsManager,
        IKrFileOwnershipChecker krFileOwnershipChecker) :
        CardGetFileVersionsExtension
    {
        #region Fields

        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);
        private readonly IKrFileOwnershipChecker krFileOwnershipChecker = NotNullOrThrow(krFileOwnershipChecker);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequest(ICardGetFileVersionsExtensionContext context)
        {
            // если запрос уже пришёл с сервера, то не проверяем права
            if (context.Request.ServiceType == CardServiceType.Default)
            {
                return;
            }

            if (context.Request.CardID is not { } cardID)
            {
                return;
            }

            var result = await KrFileAccessHelper.CheckAccessAsync(
                context.Request,
                context,
                cardID,
                this.permissionsManager);

            // Проверяем доступ
            if (result is { Result: true })
            {
                context.Info[nameof(ServerKrPermissionsGetFileVersionsExtension)] = result.Info;
            }
        }

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardGetFileVersionsExtensionContext context)
        {
            if (context.RequestIsSuccessful
                && context.Response.TryGetFileVersions() is { } fileVersions
                && context.Info.TryGetValue(nameof(ServerKrPermissionsGetFileVersionsExtension), out var obj)
                && obj is Dictionary<string, object> info
                && info.TryGetValue(KrPermissionsHelper.FileReadAccessSettings.InfoKey, out var accessSettingObj)
                && accessSettingObj is int accessSetting)
            {
                var lastVersionNum = fileVersions.Max(x => x.Number);
                switch (accessSetting)
                {
                    case KrPermissionsHelper.FileReadAccessSettings.OnlyLastVersion:
                        fileVersions.RemoveAll(x => x.Number != lastVersionNum);
                        break;

                    case KrPermissionsHelper.FileReadAccessSettings.OnlyLastAndOwnVersions:
                        for (var i = fileVersions.Count - 1; i >= 0; i--)
                        {
                            var version = fileVersions[i];
                            if (version.Number != lastVersionNum &&
                                !await this.krFileOwnershipChecker.IsOwnAsync(
                                    version.CreatedByID,
                                    context.Session.User.ID,
                                    true,
                                    cardId: context.Request.CardID,
                                    cacheHolder: context.Info,
                                    cancellationToken: context.CancellationToken))
                            {
                                fileVersions.RemoveAt(i);
                            }
                        }
                        break;
                }
            }
        }

        #endregion
    }
}
