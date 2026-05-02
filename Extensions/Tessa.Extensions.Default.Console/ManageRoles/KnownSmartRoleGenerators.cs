using System;
using Tessa.FileVault;
using Tessa.Groups;
using Tessa.Localization;

namespace Tessa.Extensions.Default.Console.ManageRoles
{
    /// <summary>
    /// Известные алиасы генераторов умных ролей, которые могут использоваться вместо идентификаторов.
    /// </summary>
    public enum KnownSmartRoleGenerators
    {
        /// <summary>
        /// Программный генератор умных ролей для групп.
        /// </summary>
        [LocalizableDescription("Groups_SmartRoleGenerator_Name")]
        Groups
    }

    public static class KnownSmartRoleGeneratorsExtensions
    {
        public static Guid ToGuid(this KnownSmartRoleGenerators value) =>
            value switch
            {
                KnownSmartRoleGenerators.Groups => GroupHelper.GroupSmartRoleGeneratorID,
                _ => throw ArgumentOutOfRange(value)
            };
    }
}
