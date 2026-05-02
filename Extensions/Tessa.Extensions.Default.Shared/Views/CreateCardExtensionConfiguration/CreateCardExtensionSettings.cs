using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Views
{
    /// <summary>
    /// The create card extension settings.
    /// </summary>
    public sealed class CreateCardExtensionSettings :
        StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Режим создания карточки.
        /// </summary>
        public CardCreationKind CardCreationKind { get; set; }

        /// <summary>
        /// Режим открытия созданной карточки карточки. Игнорируется, если расширение используется для выбора ссылки по троеточию.
        /// </summary>
        public CardOpeningKind CardOpeningKind { get; set; }

        /// <summary>
        /// Алиас типа карточки, если режим создания карточки <see cref="CardCreationKind"/>
        /// равен <see cref="Views.CardCreationKind.ByTypeAlias"/>.
        /// </summary>
        public string TypeAlias { get; set; }

        /// <summary>
        /// Идентификатор типа документа (но не типа карточки), если режим создания карточки <see cref="CardCreationKind"/>
        /// равен <see cref="Views.CardCreationKind.ByDocTypeIdentifier"/>.
        /// </summary>
        public string DocTypeIdentifier { get; set; }

        /// <summary>
        /// Название параметра, по которому можно получить запись по первичному ключу.
        /// Необходимо для поведения "Создать новую карточку и выбрать" при выборе ссылки по троеточию.
        /// </summary>
        public string IDParam { get; set; }

        /// <summary>
        /// Открыть окно на полный экран (актуально для <see cref="CardOpeningKind.ModalDialog"/>).
        /// </summary>
        public bool OpenInFullscreen { get; set; }

        /// <summary>
        /// Открыть только первую вкладку без заголовка (актуально для <see cref="CardOpeningKind.ModalDialog"/>).
        /// </summary>
        public bool OpenOnlyFirstTab { get; set; }

        /// <summary>
        /// Заголовок вкладки/окна.
        /// </summary>
        public string DisplayValue { get; set; }

        #endregion

        #region IStorageSerializable Members

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object> storage)
        {
            storage[nameof(this.CardCreationKind)] = this.CardCreationKind.ToString();
            storage[nameof(this.CardOpeningKind)] = this.CardOpeningKind.ToString();
            storage[nameof(this.TypeAlias)] = this.TypeAlias;
            storage[nameof(this.DocTypeIdentifier)] = this.DocTypeIdentifier;
            storage[nameof(this.IDParam)] = this.IDParam;
            storage[nameof(this.OpenInFullscreen)] = BooleanBoxes.Box(this.OpenInFullscreen);
            storage[nameof(this.OpenOnlyFirstTab)] = BooleanBoxes.Box(this.OpenOnlyFirstTab);
            storage[nameof(this.DisplayValue)] = this.DisplayValue;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object> storage)
        {
            this.CardCreationKind = storage.TryConvertEnum<CardCreationKind>(nameof(this.CardCreationKind)) ?? CardCreationKind.ByTypeFromSelection;
            this.CardOpeningKind = storage.TryConvertEnum<CardOpeningKind>(nameof(this.CardOpeningKind)) ?? CardOpeningKind.ApplicationTab;
            this.TypeAlias = storage.TryGet<string>(nameof(this.TypeAlias));
            this.DocTypeIdentifier = storage.TryGet<string>(nameof(this.DocTypeIdentifier));
            this.IDParam = storage.TryGet<string>(nameof(this.IDParam));
            this.OpenInFullscreen = storage.TryGet<bool>(nameof(this.OpenInFullscreen));
            this.OpenOnlyFirstTab = storage.TryGet<bool>(nameof(this.OpenOnlyFirstTab));
            this.DisplayValue = storage.TryGet<string>(nameof(this.DisplayValue));
        }

        #endregion
    }
}
