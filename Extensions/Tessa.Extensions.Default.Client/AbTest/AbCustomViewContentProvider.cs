using System;
using System.Collections.Generic;
using Tessa.UI;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Workplaces.Tree;

namespace Tessa.Extensions.Default.Client.AbTest
{
    internal sealed class AbCustomViewContentProvider(IFolderTreeItem folder) : ViewModel<EmptyModel>, IContentProvider
    {
        #region IContentProvider Members

        /// <inheritdoc/>
        public IDictionary<Guid, IWorkplaceViewComponent> Components { get; } = new Dictionary<Guid, IWorkplaceViewComponent>();

        /// <inheritdoc/>
        public object Content { get; } = new AbCustomFolderView(NotNullOrThrow(folder));

        /// <inheritdoc/>
        public void Refresh() => this.OnPropertyChanged(nameof(this.Content));

        #endregion

        #region IViewContextMarker Members

        /// <inheritdoc/>
        public IViewContext ViewContext => null;

        #endregion
    }
}
