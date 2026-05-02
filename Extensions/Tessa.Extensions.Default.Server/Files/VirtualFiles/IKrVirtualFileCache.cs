#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Files.VirtualFiles
{
    /// <summary>
    /// Кэш виртуальных файлов.
    /// </summary>
    public interface IKrVirtualFileCache
    {
        /// <summary>
        /// Получает все виртуальные файлов из кэша. Загружает данные из базы при первом вызове.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Все виртуальные файлы из кэша.</returns>
        ValueTask<IKrVirtualFile[]> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Получает виртуальный файл по его ID. Загружает данные из базы при первом вызове.
        /// Если файл отсутствует по идентификатору, то возвращает <see langword="null"/>.
        /// </summary>
        /// <param name="virtualFileID">Идентификатор виртуального файла.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Виртуальный файл из кэша.</returns>
        ValueTask<IKrVirtualFile?> TryGetAsync(Guid virtualFileID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получает список идентификаторов типов карточек, которые могут иметь виртуальные файлы.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Список идентификаторов типов карточек, которые могут иметь виртуальные файлы.</returns>
        ValueTask<IList<Guid>> GetAllowedTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Сбрасывает кэш виртуальных файлов.
        /// </summary>
        /// <returns>Асинхронная задача.</returns>
        Task InvalidateAsync();
    }
}
