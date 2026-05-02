#nullable enable

using System;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет информацию о роли дополнительного исполнителя.
    /// </summary>
    /// <param name="rowID"><inheritdoc cref="RowID" path="/summary"/></param>
    /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
    /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
    /// <param name="isResponsible"><inheritdoc cref="IsResponsible" path="/summary"/></param>
    /// <param name="mainApproverRowID"><inheritdoc cref="MainApproverRowID" path="/summary"/></param>
    public readonly struct AdditionalRoleEntryStorage(
        Guid? rowID,
        Guid id,
        string name,
        bool isResponsible,
        Guid? mainApproverRowID = null)
        : IRoleUser
    {
        #region IRoleUser Members

        /// <inheritdoc/>
        public Guid ID { get; } = id;

        /// <inheritdoc/>
        public string Name { get; } = name;

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор строки.
        /// </summary>
        public Guid? RowID { get; } = rowID;

        /// <summary>
        /// Значение, показывающее, что исполнитель является ответственным.
        /// </summary>
        public bool IsResponsible { get; } = isResponsible;

        /// <summary>
        /// Идентификатор строки роли основного согласующего.
        /// </summary>
        public Guid? MainApproverRowID { get; } = mainApproverRowID;

        #endregion
    }
}
