using System;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Console.MigrateFiles
{
    public sealed class FileVersionInfo
    {
        #region Constructors

        public FileVersionInfo(Guid cardID, Guid fileID, Guid versionRowID, bool isDeleted)
        {
            this.CardID = cardID;
            this.FileID = fileID;
            this.VersionRowID = versionRowID;
            this.IsDeleted = isDeleted;
        }

        #endregion

        #region Properties

        private Guid CardID { get; }

        private Guid FileID { get; }

        public Guid VersionRowID { get; }

        public bool IsDeleted { get; }

        #endregion

        #region Methods

        public CardContentContext CreateContext(CardFileSourceType source, IValidationResultBuilder validationResult) =>
            new(this.CardID, this.FileID, this.VersionRowID, source, validationResult);

        #endregion
    }
}
