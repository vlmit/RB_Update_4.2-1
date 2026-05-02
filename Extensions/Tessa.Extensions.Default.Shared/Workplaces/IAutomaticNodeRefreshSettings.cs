#nullable enable

using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workplaces
{
    /// <summary>
    /// Описание интерфейса настроек автоматического обновления узлов рабочего места.
    /// </summary>
    public interface IAutomaticNodeRefreshSettings :
        IStorageSerializable
    {
        /// <summary>
        /// Интервал автоматического обновления в секундах.
        /// </summary>
        int RefreshInterval { get; set; }

        /// <summary>
        /// Признак необходимости обновления табличных данных.
        /// </summary>
        bool WithContentDataRefreshing { get; set; }

        /// <summary>
        /// Признак постоянного обновления узла, вне зависимости от того, выбран он или нет.
        /// </summary>
        bool AlwaysRefresh { get; set; }
    }
}
