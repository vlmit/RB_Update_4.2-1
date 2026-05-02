#nullable enable

using Tessa.Cards;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет константы.
    /// </summary>
    public static class WorkflowCommonConstants
    {
        /// <summary>
        /// Название ключа, по которому в <see cref="CardTask.Settings"/> содержится значение флага, дающего права на редактирование карточки договора исполнителями. Тип значения: <see cref="bool"/>. Значение по умолчанию: <see langword="false"/>.
        /// </summary>
        public const string CanEditAnyFiles = StorageHelper.SystemKeyPrefix + nameof(CanEditAnyFiles);

        /// <summary>
        /// Название ключа, по которому в <see cref="CardTask.Settings"/> содержится значение флага, дающего права на редактирование приложенных файлов исполнителями. Тип значения: <see cref="bool"/>. Значение по умолчанию: <see langword="false"/>.
        /// </summary>
        public const string CanEditCard = StorageHelper.SystemKeyPrefix + nameof(CanEditCard);

        /// <summary>
        /// Название ключа, по которому в <see cref="CardTask.Settings"/> содержится значение флага отключающего автоматическое согласование задания. Тип значения: <see cref="bool"/>. Значение по умолчанию: <see langword="false"/>.
        /// </summary>
        public const string IsDisableAutoApproval = StorageHelper.SystemKeyPrefix + nameof(IsDisableAutoApproval);
    }
}
