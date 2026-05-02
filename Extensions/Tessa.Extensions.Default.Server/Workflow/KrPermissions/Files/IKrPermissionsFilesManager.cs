#nullable enable

using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <summary>
    /// Объект для проверки доступа к файлу по правилам доступа.
    /// </summary>
    public interface IKrPermissionsFilesManager
    {
        /// <summary>
        /// Рассчитывает доступ к файлу по расширенным правилам доступа.
        /// </summary>
        /// <param name="context">Контекст расчёта доступа к файлу.</param>
        /// <returns>Настройки доступа к файлу.</returns>
        ValueTask<IKrPermissionsFilesManagerResult> CheckPermissionsAsync(IKrPermissionsFilesManagerContext context);

        /// <summary>
        /// Возвращает результат валидации по результату проверки файла.
        /// </summary>
        /// <param name="context">Контекст проверки файла.</param>
        /// <param name="result">Результат проверки файла.</param>
        /// <param name="isReplace">Определяет, что вызывается замена файла, а не изменение. Используется при проверке доступа на изменение файла.</param>
        /// <param name="isCopy">Определяет, что вызывается копирование файла, а не его чтение. Используется при проверке доступа на чтение файла.</param>
        /// <returns>Результат валидации.</returns>
        ValueTask<ValidationResult> GetValidationResultAsync(
            IKrPermissionsFilesManagerContext context,
            IKrPermissionsFilesManagerResult result,
            bool isReplace = false,
            bool isCopy = false);
    }
}
