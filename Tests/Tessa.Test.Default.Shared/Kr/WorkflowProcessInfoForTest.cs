#nullable enable

using System;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Объект, содержащий информацию по процессу.
    /// </summary>
    /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
    /// <param name="typeName"><inheritdoc cref="TypeName" path="/summary"/></param>
    /// <param name="parameters"><inheritdoc cref="Parameters" path="/summary"/></param>
    public sealed class WorkflowProcessInfoForTest(
        Guid id,
        string typeName,
        string? parameters) :
        IEquatable<string>, IEquatable<WorkflowProcessInfoForTest>
    {
        #region Properties

        /// <summary>
        /// Идентификатор процесса.
        /// </summary>
        public Guid ID { get; } = id;

        /// <summary>
        /// Название типа процесса.
        /// </summary>
        public string TypeName { get; } = NotEmptyOrThrow(typeName);

        /// <summary>
        /// Параметры процесса.
        /// </summary>
        public string? Parameters { get; } = parameters;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override string ToString() =>
            $"{Environment.NewLine}" +
            $"{this.GetType().Name}: " +
            $"{nameof(this.ID)} = {this.ID:B}, " +
            $"{nameof(this.TypeName)} = {this.TypeName}, " +
            $"{nameof(this.Parameters)} = {FormatNullable(this.Parameters)}";

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            this.Equals(obj as WorkflowProcessInfoForTest);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(this.ID, this.TypeName, this.Parameters);

        #endregion

        #region Operators

        /// <summary>
        /// Определяет равны ли два указанных объекта.
        /// </summary>
        /// <param name="x">Первый сравниваемый объект.</param>
        /// <param name="y">Второй сравниваемый объект.</param>
        /// <returns>Значение <see langword="true"/>, если объекты равны, иначе - <see langword="false"/>.</returns>
        public static bool operator ==(WorkflowProcessInfoForTest x, WorkflowProcessInfoForTest y) =>
            ReferenceEquals(x, y)
            && x is not null
            && x.Equals(y);

        /// <summary>
        /// Определяет не равны ли два указанных объекта.
        /// </summary>
        /// <param name="x">Первый сравниваемый объект.</param>
        /// <param name="y">Второй сравниваемый объект.</param>
        /// <returns>Значение <see langword="true"/>, если объекты не равны, иначе - <see langword="false"/>.</returns>
        public static bool operator !=(WorkflowProcessInfoForTest x, WorkflowProcessInfoForTest y) => !(x == y);

        #endregion

        #region IEquatable<string> Members

        /// <inheritdoc/>
        public bool Equals(string? other) =>
            string.Equals(other, this.TypeName, StringComparison.Ordinal);

        #endregion

        #region IEquatable<WorkflowProcessInfoForTest> Members

        /// <inheritdoc/>
        public bool Equals(WorkflowProcessInfoForTest? other) =>
            other is not null
                && this.ID == other.ID
                && string.Equals(this.TypeName, other.TypeName, StringComparison.Ordinal)
                && string.Equals(this.Parameters, other.Parameters, StringComparison.Ordinal);

        #endregion
    }
}
