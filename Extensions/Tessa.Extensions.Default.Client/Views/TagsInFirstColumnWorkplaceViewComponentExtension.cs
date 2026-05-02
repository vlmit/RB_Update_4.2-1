using System.Linq;
using Tessa.Platform;
using Tessa.Tags;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Расширение, которое в рабочем узле, где в метаданных для представления или пользователем в настройках таблицы задана позиция тегов "InColumn", 
    /// перемещает колонку с тегами влево.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="TagsInFirstColumnWorkplaceViewComponentExtensionConfigurator"/>.
    /// </remarks>
    public sealed class TagsInFirstColumnWorkplaceViewComponentExtension : IWorkplaceViewComponentExtension
    {
        #region IWorkplaceViewComponentExtension Implementation

        public void Clone(IWorkplaceViewComponent source, IWorkplaceViewComponent cloned, ICloneableContext context)
        {
        }

        public void Initialize(IWorkplaceViewComponent model)
        {
        }

        public void Initialized(IWorkplaceViewComponent model)
        {
            var tableView = model.Content.OfType<TableView>().FirstOrDefault();
            if (tableView?.Grid is {})
            {
                tableView.Grid.Info[TagsHelper.TagsInFirstColumnKey] = BooleanBoxes.True;
            }
        }

        #endregion
    }
}
