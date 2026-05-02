#nullable enable

using System.Collections.Generic;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Описывает параметры запуска процесса используемые <see cref="KrProcessClientLauncherBase"/>.
    /// </summary>
    public class KrProcessClientLauncherBaseSpecificParameters :
        KrProcessLauncherSpecificParametersBase
    {
        #region Properties

        /// <summary>
        /// Дополнительная информация, передаваемую в запросе на сохранение карточки для расширений. Данные должны быть сериализуемых типов.
        /// </summary>
        public IDictionary<string, object?>? RequestInfo { get; set; }

        #endregion
    }
}
