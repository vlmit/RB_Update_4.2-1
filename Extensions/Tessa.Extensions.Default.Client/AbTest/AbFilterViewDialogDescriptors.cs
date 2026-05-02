using Tessa.Extensions.Default.Client.Views;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Предоставляет объекты типа <see cref="FilterViewDialogDescriptor"/>.
    /// </summary>
    public static class AbFilterViewDialogDescriptors
    {
        /// <summary>
        /// Дескриптор, описывающий специальный диалог с параметрами фильтрации представления РМ AbTest/Автомобили.
        /// </summary>
        public static readonly FilterViewDialogDescriptor Cars =
            new("AbCarViewParameters",
            [
                new("CarName", "Parameters", "Name"),
                new("CarMaxSpeed", "Parameters", "MaxSpeed")
                {
                    CriteriaOperator = EqualsToCriteriaOperator.Instance
                },
                new("Driver", "Parameters", "DriverID")
                {
                    DisplayValueSectionName = "Parameters",
                    DisplayValueFieldName = "DriverName"
                },
                new("CarReleaseDateFrom", "Parameters", "ReleaseDateFrom"),
                new("CarReleaseDateTo", "Parameters", "ReleaseDateTo"),
            ]);
    }
}
