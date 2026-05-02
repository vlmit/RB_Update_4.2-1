using Tessa.Platform;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Console.ImportUsers
{
    public sealed class UserInfo :
        TimeZoneObject
    {
        #region Properties

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? MiddleName { get; init; }

        public string? ShortName { get; set; }

        public string? Position { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public bool Hide { get; init; }

        public UserLoginType? LoginType { get; init; }

        public string? Login { get; init; }

        public string? Password { get; init; }

        public string? ExternalID { get; init; }

        #endregion

        #region Methods

        /// <summary>
        /// Returns full name as following: <c>{LastName} {FirstName} {MiddleName}</c>
        /// </summary>
        /// <returns>Full name.</returns>
        public string GetFullName()
        {
            var lastName = this.LastName?.Trim() ?? string.Empty;
            var firstName = this.FirstName?.Trim() ?? string.Empty;
            var middleName = this.MiddleName?.Trim() ?? string.Empty;

            var totalLength = lastName.Length + firstName.Length + middleName.Length;
            if (totalLength == 0)
            {
                return string.Empty;
            }

            var fullName = StringBuilderHelper.Acquire(totalLength + 2).Append(lastName);
            if (firstName.Length > 0)
            {
                if (fullName.Length > 0)
                {
                    fullName.Append(' ');
                }

                fullName.Append(firstName);
            }

            if (middleName.Length > 0)
            {
                if (fullName.Length > 0)
                {
                    fullName.Append(' ');
                }

                fullName.Append(middleName);
            }

            return fullName.ToStringAndRelease();
        }

        #endregion
    }
}
