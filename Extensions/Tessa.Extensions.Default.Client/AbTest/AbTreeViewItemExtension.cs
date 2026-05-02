using System.Threading.Tasks;
using Tessa.UI;
using Tessa.UI.Menu;
using Tessa.UI.Views;
using Tessa.UI.Views.Workplaces.Tree;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Тестовое расширение для узлов дерева рабочего места.
    /// Добавляет в контекстное меню узла дерева РМ новый элемент.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="AbTreeViewItemExtensionConfigurator"/>.
    /// </remarks>
    public sealed class AbTreeViewItemExtension : ITreeItemExtension
    {
        #region ITreeItemExtension Members

        /// <inheritdoc/>
        public void Initialize(ITreeItem model)
        {
            model.ContextMenuGenerators.Add(static ctx =>
            {
                ctx.MenuActions.Add(
                    new MenuAction(
                        "TestMenuItem",
                        "Test menu item",
                        ctx.MenuContext.Icons.Get("Thin1"),
                        new DelegateCommand(static _ => TessaDialog.ShowMessage("Test message"))));

                return ValueTask.CompletedTask;
            });
        }

        #endregion
    }
}
