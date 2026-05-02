#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Хэлперы, выполняющие проверку результатов валидации с помощью <see cref="Assert"/>.
    /// </summary>
    public static class ValidationAssert
    {
        #region Public Methods

        /// <summary>
        /// Проверяет указанные результаты валидации с помощью заданного объекта валидации. Создаёт исключение <see cref="AssertionException"/>, если проверка не пройдена.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObject">Объект валидации выполняющих проверку результатов валидации.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasMessages(
            ValidationResult result,
            ValidationResultItemValidator validationObject,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            ThrowIfNull(result);
            ThrowIfNull(validationObject);

            IReadOnlyCollection<IValidationResultItem> actualItems = result.Items;
            exceptItemsFunc ??= TestValidationKeys.ExceptPendingActionValidationResult;
            actualItems = exceptItemsFunc(actualItems);

            EqualValidationResultCount(
                actualItems.Count,
                validationObject.ExpectedCount,
                result);

            HasValidValidationObject(
                actualItems,
                validationObject,
                0,
                result);
        }

        /// <summary>
        /// Проверяет указанные результаты валидации с помощью заданных объектов валидации. Создаёт исключение <see cref="AssertionException"/>, если проверка не пройдена.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObjects">Коллекция объектов валидации выполняющих проверку результатов валидации. Порядок задания объектов валидации не имеет значения.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        /// <param name="inOrder">Значение <see langword="true"/>, если порядок следования результатов валидации должен соответствовать порядку следования валидаторов, указанных в <paramref name="validationObjects"/>, иначе - <see langword="false"/>.</param>
        public static void HasMessages(
            ValidationResult result,
            IReadOnlyCollection<ValidationResultItemValidator> validationObjects,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null,
            bool inOrder = false)
        {
            ThrowIfNull(result);
            ThrowIfNull(validationObjects);

            IReadOnlyCollection<IValidationResultItem> actualItems = result.Items;
            exceptItemsFunc ??= TestValidationKeys.ExceptPendingActionValidationResult;
            actualItems = exceptItemsFunc(actualItems);

            EqualValidationResultCount(
                actualItems.Count,
                validationObjects.Sum(i => i.ExpectedCount),
                result);

            if (validationObjects.Count == 0)
            {
                return;
            }

            var i = 0;

            foreach (var validationObject in validationObjects)
            {
                var (lastItem, lastItemOrder) = HasValidValidationObject(
                    actualItems,
                    validationObject,
                    i,
                    result);

                if (inOrder)
                {
                    if (validationObject.ExpectedCount != 1)
                    {
                        throw new InvalidOperationException($"Parameter {nameof(inOrder)} is only applicable for validators with {nameof(ValidationResultItemValidator)}.{nameof(ValidationResultItemValidator.ExpectedCount)} equal to 1.");
                    }

                    if (lastItemOrder != i)
                    {
                        var sb = StringBuilderHelper.Acquire()
                            .AppendLine($"The validation result satisfying validator #{i + 1} ({validationObject.Name}) must have the sequence number {i + 1}. But was: {lastItemOrder + 1}.")
                            .Append("Bad validation result item: ")
                            .AppendLine(lastItem!.ToString())
                            .AppendLine()
                            .AppendLine("Validation results being verified:")
                            ;

                        TestTextHelper.PrintTable(
                            sb,
                            actualItems
                                .Select(static (i, j) => new[]
                                {
                                    (j + 1).ToString(),
                                    i.ToString(),
                                })
                                .ToArray(),
                            [
                                "Serial number",
                                "Validation result item"
                            ]);

                        sb
                            .AppendLine()
                            .AppendLine()
                            .AppendLine("Validation results being verified (containing elements which were excluded from the check):")
                            .Append(result.ToString(ValidationLevel.Detailed));

                        Assert.Fail(sb.ToStringAndRelease());
                    }
                }

                i++;
            }
        }

        /// <summary>
        /// Проверяет успешно ли была пройдена валидация. Создаёт исключение <see cref="AssertionException"/>, если проверка не пройдена.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        public static void IsSuccessful(ValidationResult result)
        {
            ThrowIfNull(result);

            if (result.HasErrors)
            {
                Assert.Fail($"Validation result contains error messages.{Environment.NewLine}{result:D}");
            }
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла неудачно.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        public static void IsNotSuccessful(ValidationResult result)
        {
            ThrowIfNull(result);

            if (result.IsSuccessful)
            {
                Assert.Fail($"Validation result should contain error messages, but doesn't.{Environment.NewLine}{result:D}");
            }
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла успешно и содержит
        /// только сообщения удовлетворяющее заданному объекту валидации.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObject">Объект валидации выполняющих проверку результатов валидации.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasInfo(
            ValidationResult result,
            ValidationResultItemValidator validationObject,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            HasInfo(result);
            HasMessages(result, validationObject, exceptItemsFunc);
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла успешно и содержит
        /// только сообщения удовлетворяющее заданным объектам валидации.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObjects">Коллекция объектов валидации выполняющих проверку результатов валидации. Порядок задания объектов валидации не имеет значения.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasInfo(
            ValidationResult result,
            IReadOnlyCollection<ValidationResultItemValidator> validationObjects,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            HasInfo(result);
            HasMessages(result, validationObjects, exceptItemsFunc);
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла успешно, но с предупреждениями, и содержит
        /// только сообщения удовлетворяющее заданному объекту валидации.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObject">Объект валидации выполняющих проверку результатов валидации.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasWarnings(
            ValidationResult result,
            ValidationResultItemValidator validationObject,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            HasWarnings(result);
            HasMessages(result, validationObject, exceptItemsFunc);
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла успешно, но с предупреждениями, и содержит
        /// только сообщения удовлетворяющее заданным объектам валидации.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObjects">Коллекция объектов валидации выполняющих проверку результатов валидации. Порядок задания объектов валидации не имеет значения.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasWarnings(
            ValidationResult result,
            IReadOnlyCollection<ValidationResultItemValidator> validationObjects,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            HasWarnings(result);
            HasMessages(result, validationObjects, exceptItemsFunc);
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла с ошибками и содержит
        /// только сообщения удовлетворяющее заданному объекту валидации.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObject">Объект валидации выполняющих проверку результатов валидации.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasErrors(
            ValidationResult result,
            ValidationResultItemValidator validationObject,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            HasErrors(result);
            HasMessages(result, validationObject, exceptItemsFunc);
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла с ошибками и содержит
        /// только сообщения удовлетворяющее заданным объектам валидации.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        /// <param name="validationObjects">Коллекция объектов валидации выполняющих проверку результатов валидации. Порядок задания объектов валидации не имеет значения.</param>
        /// <param name="exceptItemsFunc">Метод исключающий из обработки результаты валидации при проверке наличия указанных сообщений. Если задано значение по умолчанию для типа, то используется метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>. В сообщения с информацией об ошибке включаются все сообщения из проверяемых результатов валидации.</param>
        public static void HasErrors(
            ValidationResult result,
            IReadOnlyCollection<ValidationResultItemValidator> validationObjects,
            Func<IReadOnlyCollection<IValidationResultItem>, IReadOnlyCollection<IValidationResultItem>>? exceptItemsFunc = null)
        {
            HasErrors(result);
            HasMessages(result, validationObjects, exceptItemsFunc);
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла успешно.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        public static void IsSuccessful(
            IValidationResultBuilder result)
        {
            ThrowIfNull(result);

            if (!result.IsSuccessful())
            {
                Assert.Fail($"Validation result contains error messages.{Environment.NewLine}{result:D}");
            }
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла неудачно.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        public static void IsNotSuccessful(
            IValidationResultBuilder result)
        {
            ThrowIfNull(result);

            if (result.IsSuccessful())
            {
                Assert.Fail($"Validation result should contain error messages, but doesn't.{Environment.NewLine}{result:D}");
            }
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что результаты валидации не содержат предупреждений.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        public static void HasNoWarnings(
            ValidationResult result)
        {
            ThrowIfNull(result);

            if (result.HasWarnings)
            {
                Assert.Fail($"Validation result contain warning messages.{Environment.NewLine}{result:D}");
            }
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что результат валидации не содержит сообщений.
        /// </summary>
        /// <param name="result">Проверяемый результат валидации.</param>
        public static void HasEmpty(
            ValidationResult result)
        {
            ThrowIfNull(result);

            Assert.That(result.Items.Count, Is.Zero, $"Expected empty validation result, but was:{Environment.NewLine}{result:D}");
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что результат валидации не содержит сообщений.
        /// </summary>
        /// <param name="result">Проверяемый результат валидации.</param>
        public static void HasEmpty(
            IValidationResultBuilder result) =>
            HasEmpty(NotNullOrThrow(result).Build());

        #endregion

        #region Private methods

        /// <summary>
        /// Проверяет успешно ли была пройдена валидация и содержится ли хотя бы одно информационное сообщение в результатах валидации.  Создаёт исключение <see cref="AssertionException"/>, если проверка не пройдена.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        private static void HasInfo(
            ValidationResult result)
        {
            ThrowIfNull(result);

            IsSuccessful(result);
            Assert.That(result.HasInfo, Is.True, $"Validation result does not contain info messages.{Environment.NewLine}{result:D}");
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла успешно, но с предупреждениями.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        private static void HasWarnings(
            ValidationResult result)
        {
            ThrowIfNull(result);

            IsSuccessful(result);
            Assert.That(result.HasWarnings, Is.True, $"Validation result does not contain warning messages.{Environment.NewLine}{result:D}");
        }

        /// <summary>
        /// Проверяет с помощью <see cref="Assert"/>, что валидация прошла с ошибками.
        /// </summary>
        /// <param name="result">Проверяемые результаты валидации.</param>
        private static void HasErrors(
            ValidationResult result)
        {
            ThrowIfNull(result);

            Assert.That(result.HasErrors, Is.True, $"Validation result does not contain error messages.{Environment.NewLine}{result:D}");
        }

        private static void EqualValidationResultCount(
            int actualCount,
            int expectedCount,
            ValidationResult result)
        {
            Assert.That(
                actualCount,
                Is.EqualTo(expectedCount),
                $"Expected {expectedCount} validation messages, but was {actualCount}.{Environment.NewLine}{result:D}");
        }

        private static (IValidationResultItem? Item, int Order) HasValidValidationObject(
            IEnumerable<IValidationResultItem> actualItems,
            ValidationResultItemValidator validationObject,
            int sequentialNumber,
            ValidationResult result)
        {
            var completedChecksCount = 0;
            var currentItemAt = 0;
            (IValidationResultItem? Item, int Order) lastOkItem = (null, -1);

            var validationResults = new ValidationResultBuilder();

            foreach (var actualItem in actualItems)
            {
                // Валидация IValidationResultItem всегда синхронная.
                var resultTask = validationObject.GetResultAsync(actualItem);
                var validationResult = resultTask.IsCompleted
                    ? resultTask.Result
                    : resultTask.AsTask().GetAwaiter().GetResult();

                if (validationResult.IsSuccessful)
                {
                    completedChecksCount++;

                    if (validationObject.ExpectedCount == 1)
                    {
                        lastOkItem = (actualItem, currentItemAt);
                    }
                }
                else
                {
                    validationResults.Add(validationResult);
                }

                currentItemAt++;
            }

            if (validationObject.ExpectedCount != completedChecksCount)
            {
                Assert.Fail(
                    $"Can't check validation result using validation object: serial number {Int32Boxes.Box(sequentialNumber + 1)}: {validationObject.Name} / Expected number of operations: {Int32Boxes.Box(validationObject.ExpectedCount)}. Actual number of operations: {Int32Boxes.Box(completedChecksCount)}." +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"Validation results:" +
                    $"{Environment.NewLine}" +
                    $"{validationResults.Build().ToString(ValidationLevel.Message)}" +
                    $"{Environment.NewLine}" +
                    $"{Environment.NewLine}" +
                    $"Validation results being verified (containing elements which were excluded from the check):" +
                    $"{Environment.NewLine}" +
                    $"{result.ToString(ValidationLevel.Detailed)}");
            }

            return lastOkItem;
        }

        #endregion
    }
}
