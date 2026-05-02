#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Информация по токену безопасности, используемая на клиенте и на сервере для проверки прав.
    /// </summary>
    [StorageObjectGenerator]
    public sealed partial class KrToken :
        CardStorageObject,
        ICloneable
    {
        #region Fields

        private HashSet<KrPermissionFlagDescriptor>? permissions;

        #endregion

        #region Constructors

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storage"]'/>
        public KrToken(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.PermissionsVersion), Int64Boxes.Zero);
            this.Init(nameof(this.Permissions), new List<Guid>());
            this.Init(nameof(this.ExtendedCardSettings), null);
            this.Init(nameof(this.ServerOnly), BooleanBoxes.False);
            this.Init(nameof(this.Signature), null);
            this.Init(nameof(this.Info), new Dictionary<string, object>(StringComparer.Ordinal));
            this.Init(nameof(this.SubmittedRules), null);
            this.Init(nameof(this.RejectedRules), null);

            // Восстанавливаем тип данных списка к List<Guid>, т.к. это важно при проверке подписи
            if (this.TryGet<object>(nameof(this.Permissions)) is IList<object> permissionsObject)
            {
                this.Set(nameof(this.Permissions), permissionsObject.Cast<Guid>().ToList());
            }
        }

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storageProvider"]'/>
        public KrToken(IStorageObjectProvider storageProvider)
            : this(StorageHelper.GetObjectStorage(storageProvider))
        {
        }

        #endregion

        #region Storage Properties

        /// <summary>
        /// Версия правил доступа.
        /// </summary>
        public long PermissionsVersion
        {
            get => this.Get<long>(nameof(this.PermissionsVersion));
            set => this.Set(nameof(this.PermissionsVersion), Int64Boxes.Box(value));
        }

        /// <summary>
        /// Идентификатор карточки. Если равен <see cref="Guid.Empty"/>, то считается, что токен подписан для новой карточки,
        /// у которой номер версии равен нулю.
        /// </summary>
        public Guid CardID
        {
            get => this.Get<Guid>(nameof(this.CardID));
            set => this.Set(nameof(this.CardID), value);
        }

        /// <summary>
        /// Номер версии карточки. Если равен <see cref="CardComponentHelper.DoNotCheckVersion"/>,
        /// то считается, что токен подписан для любой версии карточки.
        /// </summary>
        public int CardVersion
        {
            get => this.Get<int>(nameof(this.CardVersion));
            set => this.Set(nameof(this.CardVersion), Int32Boxes.Box(value));
        }

        /// <summary>
        /// Права на карточку типового решения. Хранит список идентификаторов объектов <see cref="KrPermissions.KrPermissionFlagDescriptor"/>
        /// </summary>
        public ICollection<KrPermissionFlagDescriptor> Permissions
        {
            get => this.permissions ??= this.InitPermissions();
            set
            {
                this.Set(nameof(this.Permissions), value.Select(x => x.ID).ToList());
                if (this.permissions is null)
                {
                    this.permissions ??= [..value];
                }
                else
                {
                    this.permissions.Clear();
                    this.permissions.AddRange(value);
                }
            }
        }

        /// <summary>
        /// Настройки доступа к карточке по секциям
        /// </summary>
        public KrPermissionExtendedCardSettingsStorage ExtendedCardSettings
        {
            get => this.GetDictionary(nameof(this.ExtendedCardSettings), static storage => new KrPermissionExtendedCardSettingsStorage(storage));
            set => this.SetStorageValue(nameof(this.ExtendedCardSettings), value);
        }

        /// <summary>
        /// Дата и время истечения токена.
        /// </summary>
        /// <remarks>
        /// Рекомендуется устанавливать дату истечения на два дня большую, чем текущая дата.
        /// </remarks>
        public DateTime ExpiryDate
        {
            get => this.Get<DateTime>(nameof(this.ExpiryDate));
            set => this.Set(nameof(this.ExpiryDate), value);
        }

        /// <summary>
        /// Определяет, что данный токен должен быть учтён только при обработки запроса со стороны сервера
        /// и игнорироваться при обработке запроса с клиента.
        /// </summary>
        public bool ServerOnly
        {
            get => this.Get<bool>(nameof(this.ServerOnly));
            set => this.Set(nameof(this.ServerOnly), BooleanBoxes.Box(value));
        }

        /// <summary>
        /// Определяет, что данный токен выдаёт все права доступа на карточку.
        /// Флаг учитывается только при наличии флага <see cref="ServerOnly"/>.
        /// </summary>
        public bool FullAccess
        {
            get => this.TryGet<bool>(nameof(this.FullAccess));
            set => this.Set(nameof(this.FullAccess), BooleanBoxes.Box(value));
        }

        /// <summary>
        /// Подпись токена, которая гарантирует его валидность. Подписываются все другие поля, кроме собственно подписи.
        /// </summary>
        public string? Signature
        {
            get => this.Get<string>(nameof(this.Signature));
            set => this.Set(nameof(this.Signature), value);
        }

        /// <summary>
        /// Дополнительная информация в токене безопасности.
        /// Должна быть записана до подписи токена, иначе он будет считаться не валидным.
        /// </summary>
        public Dictionary<string, object?>? Info
        {
            get => this.Get<Dictionary<string, object?>>(nameof(this.Info));
            set => this.Set(nameof(this.Info), value);
        }

        /// <summary>
        /// Список правил доступа, которые были применены при расчёте прав доступа.
        /// </summary>
        public List<Guid> SubmittedRules
        {
            get => this.Get<List<Guid>>(nameof(this.SubmittedRules), () => new List<Guid>());
            set => this.Set(nameof(this.SubmittedRules), value);
        }

        /// <summary>
        /// Список правил, которые были отклонены при расчёте прав доступа.
        /// </summary>
        public List<Guid> RejectedRules
        {
            get => this.Get<List<Guid>>(nameof(this.RejectedRules), () => new List<Guid>());
            set => this.Set(nameof(this.RejectedRules), value);
        }

        #endregion

        #region Base Overrides

        /// <doc path='info[@type="IValidationObject" and @item="Validate"]'/>
        protected override void ValidateInternal(IValidationResultBuilder validationResult)
        {
            ValidationSequence
                .Begin(validationResult)
                .SetObjectName(this, this.TryGetString(nameof(this.CardID)))
                .SetMessage(PropertyNotExists, ValidationResultType.Error)
                .Validate(nameof(this.CardID), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.CardVersion), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.Permissions), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.ExpiryDate), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.ServerOnly), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.Signature), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.Info), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.SubmittedRules), this.ObjectExistsInStorageByKey)
                .Validate(nameof(this.RejectedRules), this.ObjectExistsInStorageByKey)
                .End();
        }

        /// <inheritdoc/>
        public override void Clean()
        {
            base.Clean();

            this.TryGetExtendedCardSettings()?.Clean();
        }

        #endregion

        #region Public Methods

        /// <doc path='info[@type="StorageObject" and @item="Clone"]'/>
        public KrToken Clone() => new(StorageHelper.Clone(this.GetStorage()));

        /// <summary>
        /// Устанавливает для карточки информацию по токену безопасности <see cref="KrToken"/>.
        /// </summary>
        /// <param name="cardInfo">Дополнительная информация для карточки.</param>
        public void Set(IDictionary<string, object?> cardInfo)
        {
            ThrowIfNull(cardInfo);

            cardInfo[nameof(KrToken)] = this.GetStorage();
        }

        /// <summary>
        /// Метод для проверки наличия заданного доступа к токене.
        /// </summary>
        /// <param name="krPermission">Проверяемая настройка доступа</param>
        /// <returns>Возвращает true, если в токене есть данная настройка доступа, иначе false</returns>
        public bool HasPermission(KrPermissionFlagDescriptor krPermission)
        {
            return krPermission.IsVirtual
                ? krPermission.IncludedPermissions.All(this.Permissions.Contains)
                : this.Permissions.Contains(krPermission);
        }

        /// <summary>
        /// Метод для добавления настройки доступа в токен. 
        /// </summary>
        /// <param name="krPermission">Добавляемая настройка доступа.</param>
        public void AddPermission(KrPermissionFlagDescriptor krPermission)
        {
            if (this.permissions is null
                || this.permissions.Add(krPermission))
            {
                this.Get<List<Guid>>(nameof(this.Permissions))?.Add(krPermission.ID);
            }
        }

        /// <summary>
        /// Метод для удаления настройки доступа из токена.
        /// </summary>
        /// <param name="krPermission">Удаляемая настройка доступа.</param>
        public void RemovePermission(KrPermissionFlagDescriptor krPermission)
        {
            if (this.permissions is null
                || this.permissions.Remove(krPermission))
            {
                this.Get<List<Guid>>(nameof(this.Permissions))?.Remove(krPermission.ID);
            }
        }

        /// <summary>
        /// Возвращает расширенные настройки доступа к карточке или <c>null</c>, если настройки не были заданы.
        /// </summary>
        /// <returns>Расширенные настройки доступа к карточке или <c>null</c>, если настройки не были заданы.</returns>
        public KrPermissionExtendedCardSettingsStorage? TryGetExtendedCardSettings()
        {
            return this.TryGetDictionary(nameof(this.ExtendedCardSettings), static storage => new KrPermissionExtendedCardSettingsStorage(storage));
        }

        /// <summary>
        /// Возвращает список правил доступа, которые были применены при расчёте прав доступа, или <c>null</c>, если список не был задан.
        /// </summary>
        public List<Guid>? TryGetSubmittedRules()
        {
            return this.TryGet<List<Guid>>(nameof(this.SubmittedRules));
        }

        /// <summary>
        /// Возвращает список правил, которые были отклонены при расчёте прав доступа, или <c>null</c>, если список не был задан.
        /// </summary>
        public List<Guid>? TryGetRejectedRules()
        {
            return this.TryGet<List<Guid>>(nameof(this.RejectedRules));
        }

        #endregion

        #region Private Methods

        private HashSet<KrPermissionFlagDescriptor> InitPermissions()
        {
            var list = this.Get<List<Guid>>(nameof(this.Permissions));
            var result = new HashSet<KrPermissionFlagDescriptor>();

            if (list is null)
            {
                return result;
            }

            foreach (var perm in KrPermissionFlagDescriptors.Full.IncludedPermissions)
            {
                if (list.Contains(perm.ID))
                {
                    result.Add(perm);
                }
            }

            return result;
        }

        #endregion

        #region ICloneable Members

        /// <doc path='info[@type="StorageObject" and @item="Clone"]'/>
        object ICloneable.Clone() => this.Clone();

        #endregion

        #region Static Methods

        /// <summary>
        /// Возвращает информацию по токену безопасности <see cref="KrToken"/>
        /// или <c>null</c>, если такая информация не была установлена.
        /// </summary>
        /// <param name="cardInfo">
        /// Дополнительная информация для карточки.
        /// Это либо <c>card.Info</c> для загруженной карточки (например, в <see cref="CardGetResponse"/>)
        /// или карточки, отправляемой на сохранение в <see cref="CardStoreRequest"/>.
        /// Либо <c>request.Info</c> для всех других запросов к <see cref="ICardRepository"/>, в которых нет карточки.
        /// </param>
        /// <returns>Запрошенная информация или <c>null</c>, если требуемая информация ещё не была установлена.</returns>
        public static KrToken? TryGet(IDictionary<string, object?> cardInfo)
        {
            ThrowIfNull(cardInfo);

            return !cardInfo.TryGetValue(nameof(KrToken), out var value)
                ? null
                : value is Dictionary<string, object?> dictionary
                    ? new KrToken(dictionary)
                    : null;
        }


        /// <summary>
        /// Возвращает признак того, что в заданной хеш-таблице <paramref name="cardInfo"/>
        /// содержится информация по токену безопасности.
        /// </summary>
        /// <param name="cardInfo">Дополнительная информация для карточки.</param>
        /// <returns>
        /// <c>true</c>, если в заданной хеш-таблице <paramref name="cardInfo"/>
        /// содержится информация по токену безопасности;
        /// <c>false</c> в противном случае.
        /// </returns>
        public static bool Contains(IDictionary<string, object> cardInfo)
        {
            ThrowIfNull(cardInfo);

            return cardInfo.ContainsKey(nameof(KrToken));
        }


        /// <summary>
        /// Удаляет информацию по токену безопасности <see cref="KrToken"/>
        /// для заданной хеш-таблицы <paramref name="cardInfo"/>.
        /// Возвращает признак того, что токен присутствовал и был удалён.
        /// </summary>
        /// <param name="cardInfo">Дополнительная информация для карточки.</param>
        /// <returns>
        /// <c>true</c>, если токен присутствовал в объекте <paramref name="cardInfo"/> и был удалён;
        /// <c>false</c>, если токен отсутствовал и не был удалён.
        /// </returns>
        public static bool Remove(IDictionary<string, object?> cardInfo)
        {
            ThrowIfNull(cardInfo);

            return cardInfo.Remove(nameof(KrToken));
        }

        #endregion
    }
}
