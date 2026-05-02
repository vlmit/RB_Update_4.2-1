#nullable enable

using System;
using System.Diagnostics;
using Tessa.Platform;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Представляет данные, используемые при проверке файла.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public readonly struct TestFileInfo
    {
        #region Properties

        /// <summary>
        /// Идентификатор файла.
        /// </summary>
        public Guid ID { get; }

        /// <summary>
        /// Идентификатор версии файла.
        /// </summary>
        public Guid VersionID { get; }

        /// <summary>
        /// Имя файла.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Контент файла.
        /// </summary>
        public byte[] Content { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TestFileInfo"/>.
        /// </summary>
        /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
        /// <param name="versionID"><inheritdoc cref="VersionID" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        /// <param name="content"><inheritdoc cref="Content" path="/summary"/></param>
        public TestFileInfo(
            Guid id,
            Guid versionID,
            string? name, byte[] content)
        {
            this.ID = id;
            this.VersionID = versionID;
            this.Name = name;
            this.Content = NotNullOrThrow(content);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override string ToString() =>
            $"{nameof(this.ID)}={this.ID:B}"
            + $", {nameof(this.VersionID)}={this.VersionID:B}"
            + $", {nameof(this.Name)}=\"{this.Name}\""
            ;

        #endregion

        #region Private Methods

        private string GetDebuggerDisplay() =>
            $"{DebugHelper.GetTypeName(this)}: {this}";

        #endregion
    }
}
