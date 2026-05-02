#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Объект, представляющий автора, содержащегося в заданном хранилище.
    /// </summary>
    public sealed class AuthorProxy :
        Author
    {
        #region Fields

        private readonly IDictionary<string, object?> storage;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="storage">Хранилище, для которого создаётся обёртка.</param>
        public AuthorProxy(
            IDictionary<string, object?> storage) =>
            this.storage = NotNullOrThrow(storage);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override Guid AuthorID => this.storage.TryGet<Guid>(KrConstants.KrAuthorSettingsVirtual.AuthorID);

        /// <inheritdoc />
        public override string? AuthorName => this.storage.TryGet<string>(KrConstants.KrAuthorSettingsVirtual.AuthorName);

        #endregion
    }
}
