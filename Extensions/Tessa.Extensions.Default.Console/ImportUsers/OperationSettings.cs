using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Console.ImportUsers
{
    public sealed class OperationSettings :
        IAsyncInitializable
    {
        #region Fields

        private HashSet<string>? yes;

        private Dictionary<string, UserLoginType>? loginTypesByNames;

        #endregion

        #region Methods

        public bool GetBool([NotNullWhen(true)] string? value) =>
            !string.IsNullOrEmpty(value)
            && this.yes?.Contains(value) == true;

        public UserLoginType GetLoginType(string? value) =>
            !string.IsNullOrEmpty(value)
            && this.loginTypesByNames?.TryGetValue(value, out var loginType) == true
                ? loginType
                : UserLoginTypes.Forbidden;

        #endregion

        #region IAsyncInitializable Members

        public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            this.yes = new(await LocalizationManager.GetAllStringsAsync("UI_Common_Yes", cancellationToken), StringComparer.OrdinalIgnoreCase);
            this.loginTypesByNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (UserLoginType loginType in UserLoginTypes.All)
            {
                if (loginType != UserLoginTypes.Forbidden)
                {
                    foreach (string value in await LocalizationManager.GetAllStringsAsync($"Enum_LoginTypes_{loginType}", cancellationToken))
                    {
                        this.loginTypesByNames[value] = loginType;
                    }
                }
            }
        }

        #endregion
    }
}
