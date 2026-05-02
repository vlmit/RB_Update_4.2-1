#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using OtpNet;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <summary>
    /// Обработчик двухфакторной аутентификации с использованием одноразового пароля на основе времени.
    /// </summary>
    public sealed class TwoFactorAuthTotpHandler :
        ITwoFactorAuthHandler
    {
        #region Fields

        private readonly IDbScope dbScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="TwoFactorAuthEmailHandler"/>.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        public TwoFactorAuthTotpHandler(IDbScope dbScope) => this.dbScope = NotNullOrThrow(dbScope);

        #endregion

        #region ITwoFactorAuthHandler Implementation

        /// <inheritdoc/>
        public TimeSpan AttemptTimeout { get; } = TimeSpan.Zero;

        /// <inheritdoc/>
        public async Task HandleStartAsync(TwoFactorAuthContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var key = context.Settings.TryGet<string?>("Key");
            var uri = context.Settings.TryGet<string?>("Uri");

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(uri))
            {
                var error = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_TotpKeyNotFound");
                context.ValidationResult.AddError(this, error);
                return;
            }

            var modifiedByID = await this.GetTypeSettingsModifiedUserAsync(context.Data.Token.UserID, context.Data.TypeID, cancellationToken);
            if (modifiedByID != context.Data.Token.UserID)
            {
                await this.UpdateTypeSettingsModifiedUserAsync(context.Data.Token.UserID, context.Data.TypeID, cancellationToken);

                context.Response["Key"] = key;
                context.Response["Uri"] = uri;
                context.Response["Message"] = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_TotpInstructionFull");
            }
            else
            {
                context.Response["Message"] = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_TotpInstructionShort");
            }
        }

        /// <inheritdoc/>
        public Task HandleCommandAsync(TwoFactorAuthContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task HandleCheckAsync(TwoFactorAuthContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            var key = context.Settings.TryGet<string?>("Key");

            if (string.IsNullOrEmpty(key))
            {
                var error = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_TotpKeyNotFound");
                context.ValidationResult.AddError(this, error);
                return;
            }

            var totp = new Totp(Base32Encoding.ToBytes(key));
            if (!totp.VerifyTotp(context.Request.Get<string>("Code"), out _))
            {
                var error = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_CodeInvalid");
                context.ValidationResult.AddError(this, error);
                return;
            }

            await this.UpdateTypeSettingsModifiedUserAsync(context.Data.Token.UserID, context.Data.TypeID, cancellationToken);
        }

        #endregion

        #region Private Methods

        private async Task<Guid?> GetTypeSettingsModifiedUserAsync(Guid userID, Guid typeID, CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();

            var userParameter = DataParameter.Guid(nameof(userID), userID);
            var typeParameter = DataParameter.Guid(nameof(typeID), typeID);

            var query = this.dbScope.BuilderFactory
                .Select().C("ModifiedByID")
                .From("TwoFactorAuthUserTypes").NoLock()
                .Where().C("ID").Equals().P(userParameter.Name!)
                .And().C("TypeID").Equals().P(typeParameter.Name!)
                .Build();

            return await this.dbScope.Db
                .SetCommand(query, userParameter, typeParameter)
                .LogCommand()
                .ExecuteAsync<Guid?>(cancellationToken);
        }

        private async Task UpdateTypeSettingsModifiedUserAsync(Guid userID, Guid typeID, CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();

            var userParameter = DataParameter.Guid(nameof(userID), userID);
            var typeParameter = DataParameter.Guid(nameof(typeID), typeID);

            var query = this.dbScope.BuilderFactory
                .Update("TwoFactorAuthUserTypes")
                .C("ModifiedByID").Assign().P(userParameter.Name!)
                .Where().C("ID").Equals().P(userParameter.Name!)
                .And().C("TypeID").Equals().P(typeParameter.Name!)
                .Build();

            await this.dbScope.Db
                .SetCommand(query, userParameter, typeParameter)
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken);
        }

        #endregion
    }
}
