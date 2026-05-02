using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Views;
using Tessa.Views.Json;
using Tessa.Views.Json.Converters;

namespace Tessa.Test.Default.Shared.Views
{
    /// <summary>
    /// Вспомогательные методы для работы с представлениями используемые в тестах.
    /// </summary>
    public static class TestViewHelper
    {
        #region Public Methods

        /// <summary>
        /// Выполняет чтение моделей представлений из встроенных ресурсов расположенных в указанной сборке по заданному пути.
        /// </summary>
        /// <param name="assembly">Сборка в которой выполняется поиск представлений.</param>
        /// <param name="jsonViewModelImporter">Объект для импорта представлений.</param>
        /// <param name="jsonViewModelAdapter">Адаптер представлений.</param>
        /// <param name="directory">Путь, относительный к <see cref="ResourcesPaths.Views"/>, по которому выполнятся загрузка представлений. Если задано значение <see langword="null"/> или <see cref="string.Empty"/>, тогда загрузка будет выполнена из <see cref="ResourcesPaths.Views"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Список моделей представлений.</returns>
        public static async ValueTask<List<TessaViewModel>> ReadViewsAsync(
            Assembly assembly,
            IJsonViewModelImporter jsonViewModelImporter,
            IJsonViewModelConverter jsonViewModelAdapter,
            string directory = default,
            CancellationToken cancellationToken = default)
        {
            const string jsonViewExtension = "jview";
            const string jsonViewExtensionWithDot = "." + jsonViewExtension;

            ThrowIfNull(assembly);
            ThrowIfNull(jsonViewModelImporter);
            ThrowIfNull(jsonViewModelAdapter);

            var result = new List<TessaViewModel>();

            var basePath = Path.Join(ResourcesPaths.Resources, ResourcesPaths.Views, directory);
            var filePaths = AssemblyHelper.GetFileNameEnumerableFromEmbeddedResources(
                assembly,
                basePath,
                jsonViewExtension);

            foreach (var filePath in filePaths)
            {
                await using var stream = assembly.GetResourceStream(filePath.FullName);
                var fileExtension = Path.GetExtension(filePath.Name);

                if (string.Equals(fileExtension, jsonViewExtensionWithDot, StringComparison.OrdinalIgnoreCase))
                {
                    var jsonViewModel = await jsonViewModelImporter.ImportAsync(stream, cancellationToken);
                    result.Add(jsonViewModelAdapter.ConvertToTessaViewModel(jsonViewModel));
                }
            }

            return result;
        }

        #endregion
    }
}
