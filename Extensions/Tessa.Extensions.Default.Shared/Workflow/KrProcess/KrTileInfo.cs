#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Workflow;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    [StorageObjectGenerator(GenerateDefaultConstructor = false)]
    public sealed partial class KrTileInfo : StorageObject
    {
        private IReadOnlyList<KrTileInfo>? readonlyTileInfo;

        public KrTileInfo(
            Guid id,
            string name,
            string? caption,
            string? icon,
            string? tooltip,
            bool isGlobal,
            bool askConfirmation,
            string? confirmationMessage,
            bool actionGrouping,
            string? buttonHotkey,
            int order,
            IEnumerable<KrTileInfo>? nestedTiles,
            Guid? handlerID,
            ButtonToolbarVisibilityMode toolbarVisibilityMode = ButtonToolbarVisibilityMode.DoNotShow,
            string? alias = null,
            bool hidden = false)
            : base(new Dictionary<string, object?>(13, StringComparer.Ordinal))
        {
            this.Set(nameof(this.ID), id);
            this.Set(nameof(this.Name), name);
            this.Set(nameof(this.Caption), caption);
            this.Set(nameof(this.Icon), icon);
            this.Set(nameof(this.Tooltip), tooltip);
            this.Set(nameof(this.IsGlobal), isGlobal);
            this.Set(nameof(this.AskConfirmation), askConfirmation);
            this.Set(nameof(this.ConfirmationMessage), confirmationMessage);
            this.Set(nameof(this.ActionGrouping), actionGrouping);
            this.Set(nameof(this.ButtonHotkey), buttonHotkey);
            this.Set(nameof(this.Order), order);
            this.Set(nameof(this.NestedTiles),
                nestedTiles?.Select(x => x.GetStorage()).ToList() ?? []);
            this.Set(nameof(this.HandlerID), handlerID);
            this.Set(nameof(this.ToolbarVisibilityMode), Int32Boxes.Box((int) toolbarVisibilityMode));
            this.Set(nameof(this.Alias), alias);
            this.Set(nameof(this.Hidden), hidden);
        }

        /// <inheritdoc />
        public KrTileInfo(
            Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        public Guid ID => this.Get<Guid>(nameof(this.ID));

        public string? Name => this.Get<string>(nameof(this.Name));

        public string? Caption => this.Get<string>(nameof(this.Caption));

        public string? Icon => this.Get<string>(nameof(this.Icon));

        public string? Tooltip => this.Get<string>(nameof(this.Tooltip));

        public bool IsGlobal => this.Get<bool>(nameof(this.IsGlobal));

        public bool AskConfirmation => this.Get<bool>(nameof(this.AskConfirmation));

        public string? ConfirmationMessage => this.Get<string>(nameof(this.ConfirmationMessage));

        public bool ActionGrouping => this.Get<bool>(nameof(this.ActionGrouping));

        public string? ButtonHotkey => this.Get<string>(nameof(this.ButtonHotkey));

        public int Order => this.Get<int>(nameof(this.Order));

        public IReadOnlyList<KrTileInfo> NestedTiles =>
            this.readonlyTileInfo ??= this.ReadOnlyListFromStorage();

        public Guid? HandlerID => this.TryGet<Guid?>(nameof(this.HandlerID));

        /// <inheritdoc cref="ButtonToolbarVisibilityMode"/>
        public ButtonToolbarVisibilityMode ToolbarVisibilityMode =>
            (ButtonToolbarVisibilityMode) this.Get<int>(nameof(this.ToolbarVisibilityMode));

        /// <summary>
        /// Алиас кнопки вторичного процесса.
        /// </summary>
        public string Alias => this.Get<string>(nameof(this.Alias)) ?? this.ID.ToString();
        
        /// <summary>
        /// Скрывать из интерфейса.
        /// </summary>
        public bool Hidden => this.Get<bool>(nameof(this.Hidden));

        private ImmutableList<KrTileInfo> ReadOnlyListFromStorage() =>
            this
                .Get<IList>(nameof(this.NestedTiles))!
                .Cast<Dictionary<string, object?>>()
                .Select(x => new KrTileInfo(x))
                .ToImmutableList();
    }
}
