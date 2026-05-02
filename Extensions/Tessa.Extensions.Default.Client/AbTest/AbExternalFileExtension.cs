using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.UI;
using Tessa.UI.Files;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Расширение, инициализирующее контекстное меню для файлов значениями по умолчанию.
    /// </summary>
    public sealed class AbExternalFileExtension :
        FileExtension
    {
        #region Base Overrides

        public override async Task OpeningMenu(IFileExtensionContext context)
        {
            // Проверяем тип карточки
            if (UIContext.Current.CardEditor.CardModel is not { } model
                || model.CardType.ID != AbCardTypes.AbCarTypeID)
            {
                return;
            }

            // Отключаем пункт "Копировать ссылку"
            if (context.File is AbExternalFile
                && context.Actions.TryGet(FileMenuActionNames.CopyLink) is { } copyLinkAction)
            {
                copyLinkAction.IsEnabled = false;
                copyLinkAction.IsCollapsed = true;
            }
        }

        #endregion
    }
}
