#nullable enable

using Tessa.Platform.Storage;
using Tessa.UI.Files;

namespace Tessa.Extensions.Default.Client.UI.CardFiles
{
    /// <summary>
    /// Вспомогательные методы для работы с файлами в UI карточек.
    /// </summary>
    public static class CardFilesHelper
    {
        /// <summary>
        /// Получает контрол файлов по имени.
        /// </summary>
        /// <param name="info">Объект с дополнительной информацией.</param>
        /// <param name="viewControlName">Имя контрола представления.</param>
        /// <returns>Файловый контрол или <c>null</c>, если контрол с заданным именем отсутствует в <paramref name="info"/>.</returns>
        public static ViewFileControl? TryGetFileControl(ISerializableObject info, string viewControlName)
        {
            ThrowIfNull(info);
            ThrowIfNullOrWhiteSpace(viewControlName);

            return info.TryGetValue(viewControlName, out var o)
                ? o as ViewFileControl
                : null;
        }
    }
}
