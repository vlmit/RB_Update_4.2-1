using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile.Models
{
    /// <summary>
    /// Объект, содержит информацию о JSON Web Token при работе с TESSA Assistant.
    /// </summary>
    public sealed class TokenInfo : StorageSerializable
    {
        #region Constructors

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="claims">Полезная нагрузка JWT токена.</param>
        public TokenInfo(IEnumerable<Claim> claims)
        {
            var dict = NotNullOrThrow(claims).ToDictionary(static i => i.Type, static j => (object?) j.Value);
            this.DeserializeCore(dict);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор пользователя для которого был выдан JSON Web Token.
        /// </summary>
        public Guid UserID { get; set; }

        /// <summary>
        /// Имя пользователя для которого был выдан JSON Web Token.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор операции для которого был выдан JSON Web Token.
        /// </summary>
        public Guid OperationID { get; set; }

        /// <summary>
        /// Разрешённые операции для токена для которого был выдан JSON Web Token.
        /// </summary>
        public DeskiMobileTokenPermissionFlags Access { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[DeskiMobileClaimNames.UserID] = this.UserID;
            storage[DeskiMobileClaimNames.UserName] = this.UserName;
            storage[DeskiMobileClaimNames.OperationID] = this.OperationID;
            storage[DeskiMobileClaimNames.Access] = this.Access;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.UserID = storage.TryConvertGuid(DeskiMobileClaimNames.UserID) ??
                throw new InvalidOperationException("Claim have not '" + DeskiMobileClaimNames.UserID + "' param.");
            this.UserName = storage.TryGet<string>(DeskiMobileClaimNames.UserName) ??
                throw new InvalidOperationException("Claim have not '" + DeskiMobileClaimNames.UserName + "' param.");
            this.OperationID = storage.TryConvertGuid(DeskiMobileClaimNames.OperationID) ??
                throw new InvalidOperationException("Claim have not '" + DeskiMobileClaimNames.OperationID + "' param.");
            this.Access = Enum.TryParse<DeskiMobileTokenPermissionFlags>(storage.TryGet<string>(DeskiMobileClaimNames.Access), out var access)
                ? access
                : DeskiMobileTokenPermissionFlags.None;
        }

        #endregion
    }
}
