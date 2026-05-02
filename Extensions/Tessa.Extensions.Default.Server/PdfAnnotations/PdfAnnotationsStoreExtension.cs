#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public sealed class PdfAnnotationsStoreExtension : CardStoreExtension
    {
        #region Fields

        private readonly IPdfAnnotationsStrategy pdfAnnotationsStrategy;
        private readonly ICardFileVersionStrategy cardFileVersionStrategy;
        private readonly ICardContentStrategy cardContentStrategy;
        private readonly IKrPermissionsManager permissionsManager;
        private readonly ICardGetStrategy cardGetStrategy;
        private readonly IConfigurationInfoProvider configurationInfoProvider;
        private readonly IKrTypesCache krTypesCache;
        private readonly IErrorManager errorManager;

        #endregion

        #region Constructors

        public PdfAnnotationsStoreExtension(
            IPdfAnnotationsStrategy pdfAnnotationsStrategy,
            ICardFileVersionStrategy cardFileVersionStrategy,
            ICardContentStrategy cardContentStrategy,
            IKrPermissionsManager permissionsManager,
            ICardGetStrategy cardGetStrategy,
            IConfigurationInfoProvider configurationInfoProvider,
            IKrTypesCache krTypesCache,
            IErrorManager errorManager)
        {
            this.pdfAnnotationsStrategy = NotNullOrThrow(pdfAnnotationsStrategy);
            this.cardFileVersionStrategy = NotNullOrThrow(cardFileVersionStrategy);
            this.cardContentStrategy = NotNullOrThrow(cardContentStrategy);
            this.permissionsManager = NotNullOrThrow(permissionsManager);
            this.cardGetStrategy = NotNullOrThrow(cardGetStrategy);
            this.configurationInfoProvider = NotNullOrThrow(configurationInfoProvider);
            this.krTypesCache = NotNullOrThrow(krTypesCache);
            this.errorManager = NotNullOrThrow(errorManager);
        }

        #endregion

        #region Base Overrides

        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            if (PdfAnnotationsHelper.GetPdfAnnotationsInfo(context.Request) is { Data.Annotations.Count: > 0 } pdfAnnotationsInfo)
            {
                await this.StoreAnnotationsAsync(context, pdfAnnotationsInfo);
            }

            if (context.Request.Card.TryGetFiles() is { Count: > 0 } files)
            {
                await this.DeleteAnnotationsAsync(context, files);

                await this.UpdateNewFilesWithOldAnnotations(context, files);
            }
        }

        #endregion

        #region Private Methods

        private async Task UpdateNewFilesWithOldAnnotations(ICardStoreExtensionContext context, ListStorage<CardFile> files)
        {
            if (PdfAnnotationsHelper.GetOldNewVersionIDs(context.Request.Card) is not { } fileIdOldVersionIds)
            {
                return;
            }

            var oldVersionIDLastVersionIDs = files.Aggregate(
                new Dictionary<Guid, Guid>(),
                (d, f) =>
                {
                    if (fileIdOldVersionIds.TryGetValue(f.RowID, out var oldVersionID) && oldVersionID != f.VersionRowID)
                    {
                        d[oldVersionID] = f.VersionRowID;
                    }
                    return d;
                });

            if (oldVersionIDLastVersionIDs.Count == 0)
            {
                return;
            }

            var pdfInputDatas = fileIdOldVersionIds.Where(x => oldVersionIDLastVersionIDs.ContainsKey(x.Value)).Select(
                x => new PdfAnnotationsData
                {
                    CardID = context.Request.Card.ID,
                    FileID = x.Key,
                    FileVersionRowID = x.Value
                }).ToList();
            var existedPdfAnnotations = await this.pdfAnnotationsStrategy.TryGetInfoAsync(pdfInputDatas, context.CancellationToken);
            if (existedPdfAnnotations.Count > 0)
            {
                foreach (var data in existedPdfAnnotations)
                {
                    if (oldVersionIDLastVersionIDs.TryGetValue(data.FileVersionRowID, out var currentVersionID))
                    {
                        data.ID = currentVersionID;
                        data.FileVersionRowID = currentVersionID;
                        await this.pdfAnnotationsStrategy.StoreAsync(data, context.Session.User.ID, context.CancellationToken);
                    }
                }
            }
        }

        private async Task StoreAnnotationsAsync(ICardStoreExtensionContext context, PdfAnnotationsInfo pdfAnnotationsInfo)
        {
            await using (context.DbScope!.Create())
            {
                var cardID = pdfAnnotationsInfo.Data!.CardID;
                var cardTypeID = await this.cardGetStrategy.GetTypeIDAsync(cardID, cancellationToken: context.CancellationToken);
                if (cardTypeID is null)
                {
                    context.ValidationResult.AddInstanceNotFoundError(
                        this,
                        this.configurationInfoProvider.GetFlags(),
                        cardID);
                    return;
                }

                bool checkPermissions = await KrComponentsHelper.HasBaseAsync(cardTypeID.Value, this.krTypesCache, context.CancellationToken);
                if (checkPermissions)
                {
                    var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                        new KrPermissionsCreateContextParams
                        {
                            CardID = cardID,
                            CardTypeID = cardTypeID,
                            ValidationResult = context.ValidationResult,
                            AdditionalInfo = context.Info,
                            ExtensionContext = context,
                            ServerToken = context.Info.TryGetServerToken(),
                            PrevToken = KrToken.TryGet(context.Request.Info),
                            ServiceType = context.Request.ServiceType,
                        },
                        cancellationToken: context.CancellationToken);

                    if (permContextResult.Status == KrPermissionsCreateContextStatus.Success)
                    {
                        if (!await this.permissionsManager.CheckRequiredPermissionsAsync(
                                permContextResult.Context,
                                KrPermissionFlagDescriptors.EditCard))
                        {
                            return;
                        }

                        var (fileVersionCardID, fileVersionFileID) = await this.cardFileVersionStrategy.GetCardInfoAsync(pdfAnnotationsInfo.Data.FileVersionRowID);
                        if (cardID != fileVersionCardID || pdfAnnotationsInfo.Data.FileID != fileVersionFileID)
                        {
                            context.ValidationResult.AddCardAndFileNotRelationWithFileVersionError(
                                this,
                                configurationInfoProvider.GetFlags(),
                                cardID,
                                pdfAnnotationsInfo.Data.FileID,
                                pdfAnnotationsInfo.Data.FileVersionRowID);
                            return;
                        }
                    }
                }
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var existedPdfAnnotations = await this.pdfAnnotationsStrategy.TryGetInfoAsync(pdfAnnotationsInfo.Data, context.CancellationToken);
            var newPdfAnnotations = await this.pdfAnnotationsStrategy.RequestMergedAnnsBeforeStoreAsync(pdfAnnotationsInfo.Data, context.Session.User.ID, context.CancellationToken);
            await this.pdfAnnotationsStrategy.StoreAsync(newPdfAnnotations, context.Session.User.ID, context.CancellationToken);

            context.ContentStoreCompleted += async (s, e) =>
            {
                if (!e.Success)
                {
                    return;
                }

                var deferral = e.Defer();

                try
                {
                    // Delete previous system added version

                    // If there aren't existed pdf annotations then ID is fileVersionRowID to replace
                    var fileVersionRowID = existedPdfAnnotations?.FileVersionRowID ?? pdfAnnotationsInfo.Data.ID!.Value;

                    var cardFileVersionInfo = await this.cardFileVersionStrategy.GetVersionAsync(fileVersionRowID, cancellationToken: e.CancellationToken);
                    if (cardFileVersionInfo is null)
                    {
                        return;
                    }

                    var cardContext = new CardContentContext(
                        pdfAnnotationsInfo.Data.CardID,
                        pdfAnnotationsInfo.Data.FileID,
                        fileVersionRowID,
                        cardFileVersionInfo.Source,
                        new ValidationResultBuilder());

                    await CardSignatureHelper.ReplaceFileSignatureVersionRowIDAsync(context.DbScope, fileVersionRowID, pdfAnnotationsInfo.Data.FileVersionRowID, e.CancellationToken);
                    await this.cardFileVersionStrategy.DeleteAsync(fileVersionRowID);
                    await this.cardContentStrategy.DeleteAsync(cardContext, e.CancellationToken);

                    if (!cardContext.ValidationResult.IsSuccessful())
                    {
                        e.Context.ValidationResult.Add(cardContext.ValidationResult.Build().ConvertToSuccessful());
                    }
                }
                catch (Exception ex)
                {
                    deferral.SetException(ex);
                    
                    context.ValidationResult.AddException(
                        this,
                        ex,
                        message: await LocalizeFormatAsync("$Cards_ErrorGettingFileContentForPdfAnnotation", pdfAnnotationsInfo.Data.FileID));

                    await this.errorManager.ReportErrorSafeAsync(
                        context.Request.Card.TypeID,
                        context.Request.Card.ID,
                        context.Request.Card.TryGetDigest(),
                        new ErrorDescription(context.ValidationResult.Build()));
                }
                finally
                {
                    deferral.Dispose();
                }
            };
        }

        private async Task DeleteAnnotationsAsync(ICardStoreExtensionContext context, ListStorage<CardFile> files)
        {
            var deletedFilesWithoutBackup = files
                .Where(f => f is { State: CardFileState.Deleted, DeletionMode: not CardFileDeletionMode.Backup })
                .Select(x => x.RowID)
                .ToArray();

            if (deletedFilesWithoutBackup.Length > 0)
            {
                await this.pdfAnnotationsStrategy.DeleteFileAnnotationsAsync(deletedFilesWithoutBackup, withBackup: false, context.CancellationToken);
            }

            var deletedFilesWithBackup = files
                .Where(f => f is { State: CardFileState.Deleted, DeletionMode: CardFileDeletionMode.Backup })
                .Select(x => x.RowID)
                .ToArray();

            if (deletedFilesWithBackup.Length > 0)
            {
                await this.pdfAnnotationsStrategy.DeleteFileAnnotationsAsync(deletedFilesWithBackup, withBackup: true, context.CancellationToken);
            }

            var restoredFiles = files
                .Where(f => f is { State: CardFileState.Inserted, DeletionMode: CardFileDeletionMode.Restore })
                .Select(x => x.RowID)
                .ToArray();

            if (restoredFiles.Length > 0)
            {
                await this.pdfAnnotationsStrategy.RestoreFileAnnotationsAsync(restoredFiles, context.CancellationToken);
            }
        }

        #endregion

    }
}
