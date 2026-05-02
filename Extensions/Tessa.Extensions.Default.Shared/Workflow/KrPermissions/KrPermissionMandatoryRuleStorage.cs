#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Настройки обязательности полей.
    /// </summary>
    [StorageObjectGenerator(GenerateDefaultConstructor = false)]
    public sealed partial class KrPermissionMandatoryRuleStorage : StorageObject
    {
        #region Constructors

        public KrPermissionMandatoryRuleStorage(
            Guid sectionID,
            IEnumerable<Guid>? columnIDs = null)
            : base(new Dictionary<string, object?>(DefaultCapacity, StringComparer.Ordinal))
        {
            this.Set(nameof(this.SectionID), sectionID);
            this.Set(nameof(this.ColumnIDs), columnIDs?.Cast<object>() ?? ImmutableList<object>.Empty);
        }

        public KrPermissionMandatoryRuleStorage(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.SectionID), GuidBoxes.Empty);
            this.Init(nameof(this.ColumnIDs), ImmutableList<object>.Empty);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Секция, для которой применяется данная настройка.
        /// </summary>
        public Guid SectionID => this.Get<Guid>(nameof(this.SectionID));

        /// <summary>
        /// Список полей, для которых применяется данная настройка.
        /// </summary>
        public IReadOnlyCollection<object>? ColumnIDs => this.Get<IReadOnlyCollection<object>>(nameof(this.ColumnIDs));

        #endregion
    }
}
