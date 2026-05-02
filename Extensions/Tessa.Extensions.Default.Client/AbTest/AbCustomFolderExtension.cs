using System;
using Tessa.UI.Views;
using Tessa.UI.Views.Workplaces.Tree;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="AbCustomFolderExtensionConfigurator"/>
    /// </remarks>
    public sealed class AbCustomFolderExtension : ITreeItemExtension
    {
        #region ITreeItemExtension Members

        /// <inheritdoc/>
        public void Initialize(ITreeItem model)
        {
            if (model is not IFolderTreeItem folder)
            {
                throw new InvalidOperationException($"Extension {nameof(AbCustomFolderExtension)} is available for use with folders only.");
            }

            folder.SwitchExpandOnSingleClick = false;
            folder.ContentProviderFactory = (item, viewModel, strategy) => new AbCustomViewContentProvider(folder);
        }

        /// <inheritdoc/>
        public void Clone(ITreeItem source, ITreeItem cloned, ICloneableContext context) =>
            this.Initialize(cloned);

        #endregion
    }
}
