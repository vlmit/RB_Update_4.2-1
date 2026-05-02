using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.RefGroups;
using Unity;

namespace Tessa.Extensions.Default.Console.RecalcGroups
{
    public static class Command
    {
        [Verb("RecalcAllRefGroups")]
        [LocalizableDescription("Common_CLI_RecalcAllRefGroups")]
        public static async Task RecalcAllGroups(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            int result = await ProcessAsync(stdOut, stdErr, RefGroupsRecalcMode.AllGroups, null, address, userName, password, quiet, nologo);
            ConsoleAppHelper.EnvironmentExit(result);
        }

        [Verb("RecalcRefGroups")]
        [LocalizableDescription("Common_CLI_RecalcRefGroups")]
        public static async Task RecalcGroups(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_RefGroupIDList")] IEnumerable<Guid> ids,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            int result = await ProcessAsync(stdOut, stdErr, RefGroupsRecalcMode.ByGroupIDs, ids, address, userName, password, quiet, nologo);
            ConsoleAppHelper.EnvironmentExit(result);
        }

        [Verb("RecalcRefGroupTypes")]
        [LocalizableDescription("Common_CLI_RecalcRefGroupTypes")]
        public static async Task RecalcTypes(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_RefGroupTypeIDList")] IEnumerable<Guid> ids,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            int result = await ProcessAsync(stdOut, stdErr, RefGroupsRecalcMode.ByGroupTypeIDs, ids, address, userName, password, quiet, nologo);
            ConsoleAppHelper.EnvironmentExit(result);
        }

        #region Private Methods

        private static async ValueTask<int> ProcessAsync(
            TextWriter stdOut,
            TextWriter stdErr,
            RefGroupsRecalcMode recalcMode,
            IEnumerable<Guid>? ids,
            string? address,
            string? userName,
            string? password,
            bool quiet,
            bool nologo)
        {
            var idsArray = ids?.AsArray();
            if (recalcMode is RefGroupsRecalcMode.ByGroupIDs or RefGroupsRecalcMode.ByGroupTypeIDs
                && idsArray is not { Length: > 0 })
            {
                throw new ArgumentException("IDs was null or empty.");
            }

            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                (c, ct) => c.Container.ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, address, cancellationToken: ct),
                async (c, ct) =>
                {
                    await using var operation = c.Container.Resolve<Operation>();
                    if (!await operation.LoginAsync(userName, password, ct))
                    {
                        return ConsoleAppHelper.FailedLoginExitCode;
                    }

                    return await operation.ExecuteAsync(
                        new()
                        {
                            Mode = recalcMode,
                            Ids = idsArray
                        }, ct);
                });
        }

        #endregion
    }
}
