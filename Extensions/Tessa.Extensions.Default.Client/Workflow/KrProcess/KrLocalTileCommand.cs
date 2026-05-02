#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Tiles;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    /// <summary>
    /// Команда выполняющаяся при клике по локальному тайлу подсистемы маршрутов.
    /// </summary>
    /// <param name="launcher"><inheritdoc cref="IKrProcessLauncher" path="/summary"/></param>
    /// <param name="krSecondaryProcessTileUIHandlerResolver"><inheritdoc cref="IKrSecondaryProcessTileUIHandlerResolver" path="/summary"/></param>
    public sealed class KrLocalTileCommand(
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
            if (tileInfo.ID == Guid.Empty
                || context.CardEditor is not { } cardEditor)
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
            using (cardEditor.SetOperationInProgress(blocking: true))
            {
                // Двойное сохранение, если в карточке есть изменения. Сперва делается основное сохранение, затем запуск процесса.
                if (await cardEditor.CardModel.HasChangesAsync())
                {
                    if (!await cardEditor.SaveCardAsync(
                            context,
                            request: new CardSavingRequest(CardSavingMode.RefreshOnSuccess)))
                    {
                        return;
                    }
                }

                var process = KrProcessBuilder
                    .CreateProcess()
                    .SetProcess(tileInfo.ID)
                    .SetCard(context.CardEditor.CardModel.Card.ID)
                    .SetProcessInfo(additionalInfo)
                    .Build();

                await this.launcher.LaunchWithCardEditorAsync(
                    process,
                    cardEditor,
                    true);
            }
        }

        #endregion
    }
}
