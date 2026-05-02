#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.UI;
using Tessa.UI.Tiles;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    /// <summary>
    /// Команда выполняющаяся при клике по глобальному тайлу подсистемы маршрутов.
    /// </summary>
    /// <param name="launcher"><inheritdoc cref="IKrProcessLauncher" path="/summary"/></param>
    /// <param name="krSecondaryProcessTileUIHandlerResolver"><inheritdoc cref="IKrSecondaryProcessTileUIHandlerResolver" path="/summary"/></param>
    public sealed class KrGlobalTileCommand(
        IKrProcessLauncher launcher,
        IKrSecondaryProcessTileUIHandlerResolver krSecondaryProcessTileUIHandlerResolver) :
        IKrTileCommand
    {
        #region Fields

        private readonly IKrProcessLauncher launcher = NotNullOrThrow(launcher);
        private readonly IKrSecondaryProcessTileUIHandlerResolver krSecondaryProcessTileUIHandlerResolver = NotNullOrThrow(krSecondaryProcessTileUIHandlerResolver);

        #endregion

        #region IKrTileCommand Members

        /// <inheritdoc />
        public async Task OnClickAsync(
            IUIContext context,
            ITile tile,
            KrTileInfo tileInfo)
        {
            if (tileInfo.ID == Guid.Empty)
            {
                return;
            }

            Dictionary<string, object?>? additionalInfo = null;

            if (tileInfo.HandlerID is not null
                && this.krSecondaryProcessTileUIHandlerResolver.TryResolve(tileInfo.HandlerID.Value) is { } handler
                && !await handler.HandleTileAsync(
                        context,
                        tileInfo,
                        additionalInfo = [],
                        CancellationToken.None))
            {
                return;
            }

            if (tileInfo.AskConfirmation
                && !TessaDialog.Confirm(LocalizeFormat(tileInfo.ConfirmationMessage)))
            {
                return;
            }

            using (TessaSplash.Create("$KrButton_DefaultTileSplash"))
            {
                var process = KrProcessBuilder
                    .CreateProcess()
                    .SetProcess(tileInfo.ID)
                    .SetProcessInfo(additionalInfo)
                    .Build();

                var specificParameters = new KrProcessUILauncher.SpecificParameters
                {
                    RaiseErrorWhenExecutionIsForbidden = true
                };

                var result = await this.launcher.LaunchAsync(
                    process,
                    specificParameters: specificParameters);

                TessaDialog.ShowNotEmpty(result.ValidationResult);
            }
        }

        #endregion
    }
}
