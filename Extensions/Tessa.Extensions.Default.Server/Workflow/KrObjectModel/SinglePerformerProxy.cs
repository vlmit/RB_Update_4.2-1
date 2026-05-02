#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    [StorageObjectGenerator]
    public sealed partial class SinglePerformerProxy :
        Performer
    {
        #region Private Fields

        private readonly IDictionary<string, object?> storage;

        #endregion

        #region Constructors

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storage"]'/>
        public SinglePerformerProxy(
            IDictionary<string, object?> storage) =>
            this.storage = NotNullOrThrow(storage);

        #endregion

        #region Properties

        /// <inheritdoc />
        public override Guid PerformerID =>
            this.storage.TryGet<Guid>(KrConstants.KrSinglePerformerVirtual.PerformerID);

        /// <inheritdoc />
        public override string? PerformerName =>
            this.storage.TryGet<string>(KrConstants.KrSinglePerformerVirtual.PerformerName);

        #endregion
    }
}
