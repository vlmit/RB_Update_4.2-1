#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Запись об автоматическом согласовании.
    /// </summary>
    /// <param name="CardID">Идентификатор карточки.</param>
    /// <param name="CardTypeName">Имя типа карточки.</param>
    /// <param name="TaskID">Идентификатор задания.</param>
    /// <param name="ApprovalComment">Комментарий согласующего.</param>
    public sealed record KrAutoApproveTaskRecord(Guid CardID, string CardTypeName, Guid TaskID, string? ApprovalComment)
    {
        public string? ApprovalComment { get; set; } = ApprovalComment;
    }
}
