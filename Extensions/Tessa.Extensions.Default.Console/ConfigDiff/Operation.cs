using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Notes;
using Tessa.Notes.Comparers;
using Tessa.Notes.Writers;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Console.ConfigDiff
{
    /// <summary>
    /// Команда сравнения конфигурации с генерацией заметок по изменениям.
    /// </summary>
    /// <param name="logger"><inheritdoc cref="Logger" path="/summary"/></param>
    public class Operation(IConsoleLogger logger)
    {
        #region Protected Declarations

        /// <inheritdoc cref="IConsoleLogger"/>
        protected IConsoleLogger Logger { get; } = NotNullOrThrow(logger);

        /// <summary>
        /// Выполняет регистрацию зависимостей в контейнере Unity.
        /// </summary>
        /// <param name="container">Контейнер Unity.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <remarks>
        /// Переопределите метод, чтобы зарегистрировать зависимости, применимые для вашего решения.
        /// </remarks>
        protected virtual ValueTask RegisterContainerAsync(IUnityContainer container, CancellationToken cancellationToken = default)
        {
            container
                .RegisterPlatformSharedDependencies()
                .RegisterNotesAPI()
                .RegisterNotesDefaultComparers()
                .RegisterNotesDefaultWriters();

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Генерирует текст с результатом в соответствии с запросом и зарегистрированным в контейнере API.
        /// </summary>
        /// <param name="request"><inheritdoc cref="INoteProcessorRequest" path="/summary"/></param>
        /// <param name="container">Контейнер Unity.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Текст с результатом.</returns>
        /// <remarks>
        /// По умолчанию вызывается метод <see cref="INoteProcessor.GenerateTextAsync"/>.
        /// </remarks>
        protected virtual ValueTask<(string Text, ValidationResult Result)> GenerateTextAsync(
            INoteProcessorRequest request,
            IUnityContainer container,
            CancellationToken cancellationToken = default)
        {
            var generator = container.Resolve<INoteProcessor>();
            return generator.GenerateTextAsync(request, cancellationToken);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Выполняет консольную команду.
        /// </summary>
        /// <param name="request">Запрос на генерацию заметок, свойства которого проверяются. В случае ошибок, они выводятся на консоль.</param>
        /// <param name="resultsFilePath">Путь, по которому записывается результат генерации, или <c>null</c>/пустая строка, если результат выводится на консоль.</param>
        /// <param name="cultureName">Имя культуры, используемой для языка, на котором выполняется генерация.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Код возврата команды.</returns>
        public async ValueTask<int> ExecuteAsync(
            INoteProcessorRequest request,
            string? resultsFilePath = null,
            string? cultureName = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            var comparerRequest = request.GetComparerRequest();
            if (string.IsNullOrEmpty(comparerRequest.OldFolder))
            {
                await this.Logger.ErrorAsync("Pass the path to old Configuration folder.");
                return -1;
            }

            if (!Directory.Exists(comparerRequest.OldFolder))
            {
                await this.Logger.ErrorAsync("Old Configuration folder doesn't exist: {0}", comparerRequest.OldFolder);
                return -2;
            }

            if (string.IsNullOrEmpty(comparerRequest.NewFolder))
            {
                await this.Logger.ErrorAsync("Pass the path new old Configuration folder.");
                return -1;
            }

            if (!Directory.Exists(comparerRequest.NewFolder))
            {
                await this.Logger.ErrorAsync("New Configuration folder doesn't exist: {0}", comparerRequest.NewFolder);
                return -2;
            }

            if (resultsFilePath?.Contains("{0}", StringComparison.Ordinal) is true)
            {
                resultsFilePath = string.Format(resultsFilePath, Guid.NewGuid().ToString("N")[..7]);
            }

            await this.Logger.InfoAsync("Comparing new Configuration folder \"{0}\" to the old one \"{1}\"", comparerRequest.NewFolder, comparerRequest.OldFolder);
            if (!string.IsNullOrEmpty(request.ExistentNotesPath))
            {
                await this.Logger.InfoAsync("Using existent notes from a file: {0}", request.ExistentNotesPath);
            }

            cultureName = cultureName?.Trim();
            var culture = string.IsNullOrEmpty(cultureName) ? null : CultureInfo.GetCultureInfo(cultureName);

            string text;
            ValidationResult result;
            try
            {
                await using var companion = new UnityContainerCompanion { UseConfiguration = true };
                (text, result) = await companion.ProcessAndGetAsync(
                    (c, ct) => this.RegisterContainerAsync(c.Container, ct),
                    async (c, ct) =>
                    {
                        using var _ = culture is null ? null : LocalizationManager.CreateScope(culture);
                        return await this.GenerateTextAsync(request, c.Container, ct);
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error comparing configuration folders", ex);
                return -3;
            }

            await this.Logger.LogResultAsync(result);

            if (!result.IsSuccessful)
            {
                return -4;
            }

            if (string.IsNullOrEmpty(text))
            {
                await this.Logger.InfoAsync("Configuration folders are identical.");
            }

            if (string.IsNullOrEmpty(resultsFilePath))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    if (!this.Logger.Quiet)
                    {
                        await this.Logger.WriteLineAsync();
                    }

                    await this.Logger.WriteLineAsync(text);
                }
            }
            else
            {
                await this.Logger.InfoAsync("Writing results into file: {0}", resultsFilePath);
                if (Path.GetDirectoryName(resultsFilePath) is { Length: not 0 } resultsFolder)
                {
                    FileHelper.CreateDirectoryIfNotExists(resultsFolder);
                }

                await File.WriteAllTextAsync(resultsFilePath, text, Encoding.UTF8, cancellationToken);
            }

            return 0;
        }

        #endregion
    }
}
