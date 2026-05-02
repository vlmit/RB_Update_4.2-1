using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.Compile
{
    public static class Command
    {
        #region Public Methods

        [Verb("Compile")]
        [LocalizableDescription("Common_CLI_Compile")]
        public static async Task Compile(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_CompileCategories")] IEnumerable<string>? categories = null,
            [Argument("id")] [LocalizableDescription("Common_CLI_CompileObject")] IEnumerable<Guid>? identifiers = null,
            [Argument("showCategories")] [LocalizableDescription("Common_CLI_ShowCategories")] bool showCategories = false,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("q")] [LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            IReadOnlySet<string>? categorySet = null;
            IReadOnlySet<Guid>? identifierSet = null;

            if (!showCategories)
            {
                categorySet = GetReadOnlySetOrNullIfEmpty(categories);
                identifierSet = GetReadOnlySetOrNullIfEmpty(identifiers);

                if (categorySet is null
                    && identifierSet?.Count > 0)
                {
                    throw new ArgumentException(
                        "Category(s) is required.",
                        nameof(categories));
                }
            }

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
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
                                Categories = categorySet,
                                Identifiers = identifierSet,
                                ShowCategories = showCategories
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }

        #endregion

        #region Private Methods

        private static IReadOnlySet<T>? GetReadOnlySetOrNullIfEmpty<T>(
            IEnumerable<T>? items)
        {
            var result = items?.ToImmutableHashSet();

            if (result?.Count == 0)
            {
                result = null;
            }

            return result;
        }

        #endregion
    }
}
