#nullable enable

namespace Tessa.Test.Default.Shared.Views
{
    /// <summary>
    /// Объект, уведомляющий клиентов об изменении в репозитории.
    /// </summary>
    public interface IMediatorServer
    {
        /// <summary>
        /// Уведомление клиентов о наличии изменений в репозитории.
        /// </summary>
        void Notify();
    }
}
