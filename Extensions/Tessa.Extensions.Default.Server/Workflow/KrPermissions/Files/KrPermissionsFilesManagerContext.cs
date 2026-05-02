#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <inheritdoc cref="IKrPermissionsFilesManagerContext"/>
    public sealed class KrPermissionsFilesManagerContext(
        ISession session,
        KrPermissionsFileAccessSettingFlag requiredAccessFlags,
        IEnumerable<IKrPermissionsFileRule> rules,
        Card? card = null,
        Guid? cardId = null,
        Guid? docTypeId = null,
        bool writeDisallowedErrors = false,
        bool serverOperation = false,
        Dictionary<string, object?>? info = null) : IKrPermissionsFilesManagerContext
    {
        #region Fields

        private Guid fileID;
        private Dictionary<string, object?>? info = info;
        private Dictionary<string, object?>? fileInfo;

        #endregion

        #region IKrPermissionsFilesManagerContext Implementation

        /// <inheritdoc/>
        public ISession Session { get; } = NotNullOrThrow(session);

        /// <inheritdoc/>
        public Card? Card { get; } = card;

        /// <inheritdoc/>
        public Guid? CardId { get; } = cardId;

        /// <inheritdoc/>
        public Guid? DocTypeId { get; } = docTypeId;

        /// <inheritdoc/>
        public KrPermissionsFileAccessSettingFlag RequiredAccessFlags { get; private set; } = requiredAccessFlags;

        /// <inheritdoc/>
        public bool WriteValidationResult { get; } = writeDisallowedErrors;

        /// <inheritdoc/>
        public bool ServerOperation { get; } = serverOperation;

        /// <inheritdoc/>
        public IReadOnlyList<IKrPermissionsFileRule> OrderedRules { get; } = NotNullOrThrow(rules).OrderByDescending(x => x.Priority).ToImmutableList();

        /// <inheritdoc/>
        public CardFile? File { get; private set; }

        /// <inheritdoc/>
        public Guid FileID => this.File?.RowID ?? this.fileID;

        /// <inheritdoc/>
        public Guid? VersionID { get; private set; }

        /// <inheritdoc/>
        public CardFile? StoreFile { get; private set; }

        /// <inheritdoc/>
        public Dictionary<string, object?> FileInfo => this.fileInfo ??= new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public Dictionary<string, object?> Info => this.info ??= new Dictionary<string, object?>(StringComparer.Ordinal);

        /// <inheritdoc/>
        public CancellationToken CancellationToken { get; init; }

        /// <inheritdoc/>
        public void SetFile(
            CardFile file,
            Guid? versionID = null,
            CardFile? storeFile = null,
            KrPermissionsFileAccessSettingFlag? permissionFileAccessFlags = null)
        {
            this.BackupFileInfo();

            this.File = NotNullOrThrow(file);
            this.fileID = file.RowID;
            this.VersionID = versionID;
            this.StoreFile = storeFile;
            this.UpdateForStoreFile();
            if (permissionFileAccessFlags is not null)
            {
                this.RequiredAccessFlags = permissionFileAccessFlags.Value;
            }

            this.RestoreFileInfo();
        }

        /// <inheritdoc/>
        public void SetFile(
            Guid fileID,
            Guid? versionID = null,
            CardFile? storeFile = null,
            KrPermissionsFileAccessSettingFlag? permissionFileAccessFlags = null)
        {
            this.BackupFileInfo();

            this.File = null;
            this.fileID = fileID;
            this.VersionID = versionID;
            this.StoreFile = storeFile;
            this.UpdateForStoreFile();
            if (permissionFileAccessFlags is not null)
            {
                this.RequiredAccessFlags = permissionFileAccessFlags.Value;
            }

            this.RestoreFileInfo();
        }

        #endregion

        #region Private Methods

        private void UpdateForStoreFile()
        {
            if (this.StoreFile is null)
            {
                return;
            }

            KrPermissionsFileAccessSettingFlag newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.None;
            switch (this.StoreFile.State)
            {
                case CardFileState.Inserted:
                    newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.Add;
                    if (CardSignatureHelper.AnySignatureRow(this.StoreFile, (file, signatureRow) => signatureRow.State == CardRowState.Inserted))
                    {
                        newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.Sign;
                    }

                    this.File = this.StoreFile;
                    break;

                case CardFileState.Deleted:
                    newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.Delete;
                    break;

                case CardFileState.Replaced:
                    newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.Edit;
                    break;

                case CardFileState.Modified:
                    newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.None;
                    if (CardSignatureHelper.AnySignatureRow(this.StoreFile, (file, signatureRow) => signatureRow.State == CardRowState.Inserted))
                    {
                        newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.Sign;
                    }
                    if (this.StoreFile.Flags.HasAny(CardFileFlags.UpdateCategory | CardFileFlags.UpdateName | CardFileFlags.UpdateOptions))
                    {
                        newRequiredAccessFlags |= KrPermissionsFileAccessSettingFlag.Edit;
                    }

                    break;

                case CardFileState.ModifiedAndReplaced:
                    newRequiredAccessFlags = KrPermissionsFileAccessSettingFlag.Edit;
                    if (CardSignatureHelper.AnySignatureRow(this.StoreFile, (_, _) => true))
                    {
                        newRequiredAccessFlags |= KrPermissionsFileAccessSettingFlag.Sign;
                    }

                    break;
            }

            this.RequiredAccessFlags = newRequiredAccessFlags;
        }

        private void BackupFileInfo()
        {
            if (this.FileID == Guid.Empty)
            {
                return;
            }

            this.Info[GetFileInfoKey(this.FileID)] = this.FileInfo;
        }

        private void RestoreFileInfo()
        {
            if (this.FileID != Guid.Empty
                && this.Info.TryGetValue(GetFileInfoKey(this.FileID), out var fileInfoObj)
                && fileInfoObj is Dictionary<string, object?> fileInfo)
            {
                this.fileInfo = fileInfo;
            }
            else
            {
                this.fileInfo = null;
            }
        }

        private static string GetFileInfoKey(Guid fileID)
        {
            return $"FileInfo_{fileID}";
        }

        #endregion
    }
}
