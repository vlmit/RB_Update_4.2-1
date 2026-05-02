using System.Collections.Generic;
using Tessa.Platform.Storage;
using Tessa.Properties.Resharper;

namespace Tessa.Extensions.Default.Shared.Workplaces
{
    /// <summary>
    /// Описание интерфейса поддерживаемых настроек фильтрации
    /// </summary>
    public interface ITreeItemFilteringSettings :
        IStorageSerializable
    {
        #region Public properties

        /// <summary>
        /// Список параметров
        /// </summary>
        [NotNull]
        List<string> Parameters { get; set; }

        /// <summary>
        /// Список секций
        /// </summary>
        [NotNull]
        List<string> RefSections { get; set; }

        #endregion
    }
}
