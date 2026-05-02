#nullable enable

using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <summary>
    /// Предоставляет типовые способы двухфакторной аутентификации.
    /// </summary>
    public static class TwoFactorAuthDefaultTypes
    {
        /// <summary>
        /// Тип 2FA "Email".
        /// </summary>
        public static readonly TwoFactorAuthType Email = new(
            new(0x5dde924c, 0x61e0, 0x4b3e, 0x80, 0x58, 0x21, 0x7d, 0x1c, 0xa7, 0x07, 0xff),
            "$Enum_TwoFactorAuthTypes_Email");

        /// <summary>
        /// Тип 2FA "TOTP".
        /// </summary>
        public static readonly TwoFactorAuthType TOTP = new(
            new(0xfef9bdb2, 0xc3b3, 0x4e18, 0x9b, 0x85, 0xd9, 0x7f, 0x4e, 0xb4, 0xa3, 0x65),
            "$Enum_TwoFactorAuthTypes_TOTP");
    }
}
