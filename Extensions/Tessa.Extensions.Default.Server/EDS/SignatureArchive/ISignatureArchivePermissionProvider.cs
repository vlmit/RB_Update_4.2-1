using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    /// <summary>
    /// Предоставляет методы для проверки доступа к функциональности архивирования подписей.
    /// </summary>
    public interface ISignatureArchivePermissionProvider
    {
        /// <summary>
        /// Проверяет, является ли пользователь администратором архивного подписания.
        /// </summary>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Признак, является ли пользователь администратором архивного подписания.</returns>
        ValueTask<bool> IsAdministratorAsync(CancellationToken cancellationToken = default);
    }
}
