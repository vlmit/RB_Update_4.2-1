#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <summary>
    /// Обработчик двухфакторной аутентификации с использованием кода из электронной почты.
    /// </summary>
    public sealed class TwoFactorAuthEmailHandler :
        ITwoFactorAuthHandler
    {
        #region Fields

        private readonly INotificationManager notificationManager;
        private readonly ITessaServerSettings serverSettings;
        private readonly IDbScope dbScope;

        private const int CodeLength = 6;
        private const int RetryTimeoutAtSeconds = 60;
        private static readonly Guid notificationID = new(0x56dfb946, 0x7efe, 0x4dee, 0xa8, 0xb4, 0x86, 0x41, 0xac, 0x01, 0xff, 0xa4);

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="TwoFactorAuthEmailHandler"/>.
        /// </summary>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        /// <param name="serverSettings"><inheritdoc cref="ITessaServerSettings" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        public TwoFactorAuthEmailHandler(
            INotificationManager notificationManager,
            ITessaServerSettings serverSettings,
            IDbScope dbScope)
        {
            this.notificationManager = NotNullOrThrow(notificationManager);
            this.serverSettings = NotNullOrThrow(serverSettings);
            this.dbScope = NotNullOrThrow(dbScope);
        }

        #endregion

        #region ITwoFactorAuthHandler Implementation

        /// <inheritdoc/>
        public TimeSpan AttemptTimeout { get; } = TimeSpan.FromSeconds(RetryTimeoutAtSeconds);

        /// <inheritdoc/>
        public async Task HandleStartAsync(TwoFactorAuthContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            if (context.ExistentAttemptID.HasValue)
            {
                var error = await LocalizeAsync("$TwoFactorAuth_Messages_AuthenticationInProgress", context.Culture) ?? string.Empty;
                context.ValidationResult.AddError(this, error);
                return;
            }

            var token = context.Data.Token;

            var email = await this.GetUserEmailAsync(token.UserID, cancellationToken);
            if (!IsEmailValid(email))
            {
                var error = await LocalizeAsync("$TwoFactorAuth_Messages_EmailInvalid", context.Culture) ?? string.Empty;
                context.ValidationResult.AddError(this, error);
                return;
            }

            var code = GenerateConfirmationCode();
            ValidationResult validationResult;

            await using (SessionContext.Create(Session.CreateSystemToken(this.serverSettings)))
            {
                validationResult = await this.notificationManager.SendAsync(
                    notificationID,
                    [token.UserID],
                    new NotificationSendContext
                    {
                        MainCardID = token.UserID,
                        ExcludeDeputies = true,
                        DisableSubscribers = true,
                        IgnoreUserSessions = true,
                        ModifyEmailActionAsync = (email2, _) =>
                        {
                            email2.PlaceholderAliases.SetReplacement("code", $"text:{code}");
                            email2.PlaceholderAliases.SetReplacement("login", $"text:{token.UserLogin}");
                            email2.PlaceholderAliases.SetReplacement("host", $"text:{token.HostIP}");
                            return Task.CompletedTask;
                        }
                    },
                    cancellationToken);

                context.ValidationResult.Add(validationResult);
            }

            if (validationResult.IsSuccessful)
            {
                context.Data.Info["Code"] = code;
                context.Data.Info["RetryDate"] = DateTime.UtcNow;
                context.Response["RetryTimeout"] = Int32Boxes.Box(RetryTimeoutAtSeconds);
                context.Response["Message"] = await GetMessageAsync(email, context.Culture, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task HandleCommandAsync(TwoFactorAuthContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            if (DateTime.UtcNow < context.Data.Info.Get<DateTime>("RetryDate").AddSeconds(RetryTimeoutAtSeconds))
            {
                var email = await this.GetUserEmailAsync(context.Data.Token.UserID, cancellationToken);
                if (IsEmailValid(email))
                {
                    context.Response["Message"] = await GetMessageAsync(email, context.Culture, cancellationToken);
                }

                var error = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_CodeRetryTimeout");
                context.ValidationResult.AddError(this, error);
            }
            else
            {
                await this.HandleStartAsync(context, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task HandleCheckAsync(TwoFactorAuthContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);

            if (context.Request.Get<string>("Code") != context.Data.Info.Get<string>("Code"))
            {
                var error = await LocalizeFormatAsync(context.Culture, "$TwoFactorAuth_Messages_CodeInvalid");
                context.ValidationResult.AddError(this, error);
            }
        }

        #endregion

        #region Private Methods

        private async Task<string?> GetUserEmailAsync(Guid userID, CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();

            var parameter = DataParameter.Guid(Names.Table_ID, userID);

            var query = this.dbScope.BuilderFactory
                .Select().C("Email")
                .From("PersonalRoles").NoLock()
                .Where().C("ID").Equals().P(parameter.Name!)
                .Build();

            return await this.dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<string>(cancellationToken);
        }

        private static bool IsEmailValid([NotNullWhen(true)] string? email) =>
            !string.IsNullOrWhiteSpace(email) && FormattingHelper.EmailRegex.IsMatch(email);

        private static ValueTask<string> GetMessageAsync(string email, CultureInfo culture, CancellationToken cancellationToken)
        {
            if (email is { Length: > 0 })
            {
                var index = email.IndexOf('@', StringComparison.OrdinalIgnoreCase);
                var tail = email[(index > 0 ? index - 1 : 0)..];
                email = $"{email[0]}******{tail}";
            }

            return LocalizeFormatAsync(culture, "$TwoFactorAuth_Messages_CodeSent", email);
        }

        private static string GenerateConfirmationCode()
        {
            var numberGenerator = new Random();
            var codeBuilder = new StringBuilder(CodeLength);
            for (int i = 0; i < CodeLength; i++)
            {
                var digit = numberGenerator.Next(minValue: 0, maxValue: 9);
                codeBuilder.Append(digit);
            }

            return codeBuilder.ToString();
        }

        #endregion
    }
}
