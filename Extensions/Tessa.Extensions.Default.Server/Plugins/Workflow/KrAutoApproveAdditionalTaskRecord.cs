#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Запись об автоматическом согласовании для заданий доп. согласования.
    /// </summary>
    /// <param name="TaskID">Идентификатор задания.</param>
    /// <param name="UserName">Имя исполнителя.</param>
    /// <param name="UserPosition">Должность исполнителя.</param>
    /// <param name="IsResponsible">Признак того, что задание для ответственного исполниля.</param>
    /// <param name="Сomment">Комментарий исполниля.</param>
    /// <param name="IsCompleted">Признак того, что задание завершено.</param>
    /// <param name="OptionID">Идентификатор варианта завершения задания.</param>
    public sealed record KrAutoApproveAdditionalTaskRecord(
        Guid TaskID,
        string? UserName,
        string? UserPosition,
        bool IsResponsible,
        string? Сomment,
        bool IsCompleted,
        Guid? OptionID);
}
