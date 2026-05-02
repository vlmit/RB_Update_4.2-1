#nullable enable

using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Результат создания контекста проверки прав доступа.
    /// </summary>
    public sealed class KrPermissionsCreateContextResult
    {
        #region Properties

        /// <summary>
        /// Контекст проверки прав доступа или <c>null</c>, если при создании контекста произошла ошибка или проверка доступа не требуется.
        /// </summary>
        public IKrPermissionsManagerContext? Context { get; init; }

        /// <summary>
        /// Статус результата создания контекста.
        /// </summary>
        public KrPermissionsCreateContextStatus Status { get; init; }

        /// <summary>
        /// Результат валидации, куда записываются ошибки при создании контекста прав доступа.
        /// </summary>
        public ValidationResult ValidationResult { get; init; } = ValidationResult.Empty;

        #endregion

        #region Static

        /// <summary>
        /// Результат создания контекста проверки прав доступа для карточки, для которой не требуется проверка прав доступа.
        /// </summary>
        public static readonly KrPermissionsCreateContextResult NotAllowed = new() { Status = KrPermissionsCreateContextStatus.NotAllowed };

        #endregion
    }
}
