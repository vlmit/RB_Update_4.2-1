#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class PDFRef : StorageSerializable
    {
        #region Properties

        public double ObjectNumber { get; set; }
        public double? GenerationNumber { get; set; }

        #endregion

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.ObjectNumber)] = DoubleBoxes.Box(this.ObjectNumber);
            storage[nameof(this.GenerationNumber)] = DoubleBoxes.Box(this.GenerationNumber);

        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.ObjectNumber = storage.TryConvertDouble(nameof(this.ObjectNumber)) ?? 0;
            this.GenerationNumber = storage.TryConvertDouble(nameof(this.GenerationNumber));
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            var other = obj as PDFRef;
            return other is not null && this.ObjectNumber == other.ObjectNumber && this.GenerationNumber == other.GenerationNumber;
        }

        public override int GetHashCode() => HashCode.Combine(this.ObjectNumber, this.GenerationNumber);

        public override string ToString() => $"{ObjectNumber} {GenerationNumber} R";
    }
}
