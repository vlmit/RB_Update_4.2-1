using Tessa.Properties.Resharper;
using Tessa.UI;
using Tessa.UI.Views.Workplaces.Tree;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Interaction logic for AbCustomFolderView.xaml
    /// </summary>
    public partial class AbCustomFolderView
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AbCustomFolderView"/> class.
        /// </summary>
        /// <param name="folder">
        /// The folder.
        /// </param>
        public AbCustomFolderView([NotNull] IFolderTreeItem folder)
        {
            this.InitializeComponent();

            this.DataContext = folder;
            this.Resources = UIHelper.Generic;
        }

        #endregion
    }
}