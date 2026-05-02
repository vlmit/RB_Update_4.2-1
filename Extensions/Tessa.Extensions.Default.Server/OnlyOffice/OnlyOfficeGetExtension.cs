#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    /// <summary>
    /// Fill files info with current coedit status.
    /// </summary>
    public sealed class OnlyOfficeGetExtension :
        CardGetExtension
    {
        #region Constructors

        public OnlyOfficeGetExtension(IOnlyOfficeService service) =>
            this.onlyOfficeService = NotNullOrThrow(service);

        #endregion

        #region Fields

        private readonly IOnlyOfficeService onlyOfficeService;

        #endregion

        #region Constants

        public const string NamesKey = StorageHelper.SystemKeyPrefix + "coeditnames";

        public const string DateKey = StorageHelper.SystemKeyPrefix + "coeditdate";

        #endregion

        #region Base Overrides

        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            // 1. we are called from WebClient
            // 2. for non-system type
            // 3. for regular get request (context.Method is default in registration)
            // 4. the card has at least one non-virtual file
            // 5. integration is enabled in ApiScriptUrl setting
            if (!context.RequestIsSuccessful
                || context.Request.ServiceType != CardServiceType.Client
                || context.Session.Token?.ApplicationID != ApplicationIdentifiers.WebClient
                || context.CardType is not { } type
                || type.Flags.HasAny(CardTypeFlags.Hidden | CardTypeFlags.Administrative)
                || type.InstanceType != CardInstanceType.Card
                || context.Response!.TryGetCard()?.TryGetFiles() is not { Count: > 0 } files
                || !await this.onlyOfficeService.IsEnabledAsync(context.CancellationToken))
            {
                return;
            }

            Dictionary<Guid, CardFile>? filesByVersionRowID = null;
            foreach (CardFile x in files)
            {
                if (!x.IsVirtual)
                {
                    (filesByVersionRowID ??= new()).Add(x.VersionRowID, x);
                }
            }

            if (filesByVersionRowID is null)
            {
                // all the files are virtual (i.e. ApprovalList, etc.)
                return;
            }

            var coeditInfos = await this.onlyOfficeService.TryGetCurrentCoeditAsync(
                filesByVersionRowID.Keys,
                context.CancellationToken);

            foreach ((Guid versionRowID, string? userNames, DateTime? lastAccessTime) in coeditInfos)
            {
                // current version may be changed in db since TryGetCurrentCoeditAsync, so here we may have non-existent files
                if (filesByVersionRowID.TryGetValue(versionRowID, out var file))
                {
                    var info = file.Info;
                    info[NamesKey] = userNames;
                    info[DateKey] = lastAccessTime;
                }
            }
        }

        #endregion
    }
}
