using System;
using System.Diagnostics.CodeAnalysis;

namespace Tessa.Extensions.Default.Console.ManageRoles
{
    public class OperationContext
    {
        /// <summary>
        /// Список вызываемых команд. Не равен <c>null</c>. Пустой массив приводит к ошибке - не указана команда.
        /// </summary>
        [DisallowNull]
        public CommandType[]? Commands { get; set; }
        
        /// <summary>
        /// Список идентификаторов, для которых выполняются команды из списка <see cref="Commands"/>.
        /// Не равен <c>null</c>. Пустой массив для команды, которая требует идентификаторов, приводит к ошибке.
        /// </summary>
        [DisallowNull]
        public Guid[]? Identifiers { get; set; }

        /// <summary>
        /// Количество записей замещения, загружаемых при каждой итерации синхронизации заместителей.
        /// Указание значения равного или меньше нуля снимает все ограничения.
        /// Важно, данное значение не должны быть меньше числа сотрудников в системе, т.к. это может привести к неработоспособности функционала синзронизации заместителей.  
        /// </summary>
        public int BulkSize { get; set; }

        /// <summary>
        /// Определяет, что при перерасчёте заместителей система будет выполнять перерасчёт только для тех ролей, у которым требуется перерасчёт замещения.
        /// </summary>
        public bool SyncChangedOnly { get; set; }
    }
}