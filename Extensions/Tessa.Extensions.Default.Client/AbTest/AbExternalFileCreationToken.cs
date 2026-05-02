#nullable enable
using Tessa.Files;

namespace Tessa.Extensions.Default.Client.AbTest
{
    public class AbExternalFileCreationToken : FileCreationToken
    {
        #region Properties

        /// <summary>
        /// Описание файла.
        /// </summary>
        public string? Description { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SetCore(IFileCreationToken token)
        {
            base.SetCore(token);
            this.Description = (token as AbExternalFileCreationToken)?.Description;
        }

        /// <inheritdoc/>
        protected override void SetCore(IFile file)
        {
            base.SetCore(file);
            this.Description = (file as AbExternalFile)?.Description;
        }

        #endregion
    }
}
