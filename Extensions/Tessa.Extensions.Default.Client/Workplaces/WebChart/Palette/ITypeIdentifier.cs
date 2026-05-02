using System;

namespace Tessa.Extensions.Default.Client.Workplaces.WebChart.Palette
{
    /// <summary>
    /// Описание интерфейса позволяющего идентифицировать тип элемента
    /// </summary>
    public interface ITypeIdentifier
    {
        /// <summary>
        /// Идентификатор типа
        /// </summary>
        Guid TypeId { get; }
    }
}
