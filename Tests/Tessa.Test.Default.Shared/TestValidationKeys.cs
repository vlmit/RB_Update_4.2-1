using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Предоставляет ключи валидации используемые в тестах.
    /// </summary>
    public static class TestValidationKeys
    {
        #region Properties

        /// <summary>
        /// Ключи сообщений валидации, добавляемые в результат валидации при обработке отложенных действий.
        /// </summary>
        public static IReadOnlySet<ValidationKey> PendingActionValidationKeys { get; }

        #endregion

        #region ValidationKey Fields

        /// <summary>
        /// При выполнении подготовительного действия "{0}" отложенного действия "{1}" возникли сообщения. Дополнительная информация расположена в последующих сообщениях.<para/>
        /// {0} - Имя подготовительного действия.<para/>
        /// {1} - Имя действия.
        /// </summary>
        public static readonly ValidationKey PendingActionPreparationActionMessages =
            new ValidationKey(
                new Guid(0x2697c6d4, 0x50ac, 0x4484, 0xad, 0xd2, 0xe1, 0x99, 0x76, 0xba, 0x1, 0xa2),
                nameof(PendingActionPreparationActionMessages),
                "Messages occurred while executing preparation action \"{0}\". Pending action \"{1}\". Additional information can be found in the following messages.");

        /// <summary>
        /// При выполнении подготовительного действия "{0}" отложенного действия "{1}" возникло исключение.<para/>
        /// {0} - Имя подготовительного действия.<para/>
        /// {1} - Имя действия.
        /// </summary>
        public static readonly ValidationKey PendingActionPreparationActionException =
            new ValidationKey(
                new Guid(0xe4b2532b, 0x31c1, 0x4648, 0xb1, 0xf5, 0xe3, 0x6e, 0x2c, 0x63, 0x60, 0x8b),
                nameof(PendingActionPreparationActionException),
                "An exception was thrown while executing preparation action \"{0}\". Pending action \"{1}\".");

        /// <summary>
        /// При выполнении действия "{0}", выполняющегося после отложенного действия "{1}", возникли сообщения. Дополнительная информация расположена в последующих сообщениях.<para/>
        /// {0} - Имя текущего действия.<para/>
        /// {1} - Имя действия.
        /// </summary>
        public static readonly ValidationKey PendingActionAfterActionMessages =
            new ValidationKey(
                new Guid(0x1c91bb4f, 0xf06d, 0x4942, 0x92, 0x76, 0x1d, 0x2a, 0xdb, 0xad, 0x3, 0x3a),
                nameof(PendingActionAfterActionMessages),
                "Messages occurred while executing after action \"{0}\". Pending action \"{1}\". Additional information can be found in the following messages.");

        /// <summary>
        /// При выполнении действия "{0}", выполняющегося после отложенного действия "{1}", возникло исключение.<para/>
        /// {0} - Имя подготовительного действия.<para/>
        /// {1} - Имя действия.
        /// </summary>
        public static readonly ValidationKey PendingActionAfterActionException =
            new ValidationKey(
                new Guid(0x7cf1273c, 0x8bc5, 0x45af, 0x9b, 0x15, 0x8a, 0xb, 0x5c, 0x4, 0x9f, 0xc),
                nameof(PendingActionAfterActionException),
                "An exception was thrown while executing after action \"{0}\". Pending action \"{1}\".");

        /// <summary>
        /// При выполнении отложенного действия \"{0}\" возникли сообщения. Дополнительная информация расположена в последующих сообщениях.<para/>
        /// {0} - Имя действия.
        /// </summary>
        public static readonly ValidationKey PendingActionMessages =
            new ValidationKey(
                new Guid(0xb339be5d, 0x8c47, 0x47f0, 0xb5, 0xb9, 0x9, 0x9a, 0xff, 0xe3, 0x11, 0xa7),
                nameof(PendingActionMessages),
                "Messages occurred while executing pending action \"{0}\". Additional information can be found in the following messages.");

        /// <summary>
        /// При выполнении отложенного действия "{0}" возникло исключение.<para/>
        /// {0} - Имя действия.
        /// </summary>
        public static readonly ValidationKey PendingActionException =
            new ValidationKey(
                new Guid(0x75d22c77, 0x992b, 0x4114, 0xba, 0xf6, 0x8d, 0xea, 0x14, 0xd8, 0xa0, 0x66),
                nameof(PendingActionException),
                "An exception was thrown while executing pending action \"{0}\".");

        /// <summary>
        /// Трассировка выполнения отложенных действий:{<see cref="Environment.NewLine"/>}{0}.<para/>
        /// {0} - Строковое представление трассировки отложенных действий.
        /// </summary>
        public static readonly ValidationKey PendingActionTrace =
            new ValidationKey(
                new Guid(0x89de37e0, 0xe5d5, 0x43ac, 0xb9, 0x54, 0xc2, 0xb7, 0x73, 0x67, 0x1e, 0x92),
                nameof(PendingActionTrace),
                $"Tracing for performing pending actions:{Environment.NewLine}{{0}}");

        #endregion

        #region Constructor

        /// <summary>
        /// Регистрирует все ключи валидации, используемые в тестах.
        /// </summary>
        static TestValidationKeys()
        {
            PendingActionValidationKeys = new HashSet<ValidationKey>()
            {
                PendingActionPreparationActionMessages,
                PendingActionPreparationActionException,
                PendingActionAfterActionMessages,
                PendingActionAfterActionException,
                PendingActionMessages,
                PendingActionException,
                PendingActionTrace,
            };

            var registry = ValidationKeyRegistry.Instance;

            foreach (var validationKey in PendingActionValidationKeys)
            {
                registry.Register(validationKey);
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Исключает, из заданной коллекции сообщений валидации, информационные сообщения добавленные при обработке отложенных действий.
        /// </summary>
        /// <param name="validationResult">Коллекция сообщений валидации.</param>
        /// <returns>Коллекция сообщений валидаций из которой исключены информационные сообщения добавленные при обработке отложенных действий.</returns>
        public static IReadOnlyCollection<IValidationResultItem> ExceptPendingActionValidationResult(
            IReadOnlyCollection<IValidationResultItem> validationResult)
        {
            ThrowIfNull(validationResult);

            return validationResult.Count > 0
                ? validationResult.Where(i => !PendingActionValidationKeys.Contains(i.Key)).ToArray()
                : validationResult;
        }

        /// <summary>
        /// Исключает, из заданной коллекции сообщений валидации, информационные сообщения добавленные при обработке отложенных действий.
        /// </summary>
        /// <param name="validationResult">Результат валидации.</param>
        /// <returns>Результат валидации из которой исключены информационные сообщения добавленные при обработке отложенных действий.</returns>
        public static ValidationResult ExceptPendingActionValidationResult(
            ValidationResult validationResult)
        {
            ThrowIfNull(validationResult);

            return validationResult.Items.Count > 0
                ? new ValidationResult(validationResult.Items.Where(i => !PendingActionValidationKeys.Contains(i.Key)))
                : validationResult;
        }

        #endregion
    }
}
