#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.UI;
using Tessa.UI.Cards;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    /// <summary>
    /// Объект, запускающий процесс на клиенте, имеющим доступ к зависимостям определённым в <see cref="Tessa.UI"/>.
    /// </summary>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    public sealed class KrProcessUILauncher(
        ICardRepository cardRepository) :
        KrProcessClientLauncherBase
    {
        #region Nested Types

        /// <summary>
        /// Предоставляет параметры запуска процесса с клиента.
        /// </summary>
        public sealed class SpecificParameters :
            KrProcessClientLauncherBaseSpecificParameters
        {
            #region Properties

            /// <summary>
            /// Возвращает или задаёт значение, показывающее, следует ли использовать текущий <see cref="ICardEditorModel"/> или нет. Приоритет выше, чем у свойства <see cref="CardEditor"/>.
            /// </summary>
            public bool UseCurrentCardEditor { get; set; }

            /// <summary>
            /// Возвращает или задаёт указанный <see cref="ICardEditorModel"/> который следует использовать. Приоритет ниже, чем у <see cref="UseCurrentCardEditor"/>.
            /// </summary>
            public ICardEditorModel? CardEditor { get; set; }

            #endregion
        }

        #endregion

        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override async Task<IKrProcessLaunchResult> LaunchCoreAsync(
            Dictionary<string, object?> requestInfo,
            ICardExtensionContext? cardContext = null,
            IKrProcessLauncherSpecificParameters? specificParameters = null,
            CancellationToken cancellationToken = default)
        {
            bool withinCardEditor;
            ICardEditorModel? specificCardEditor;

            if (specificParameters is SpecificParameters sp)
            {
                withinCardEditor = sp.UseCurrentCardEditor;
                specificCardEditor = sp.CardEditor;
            }
            else
            {
                withinCardEditor = false;
                specificCardEditor = null;
            }

            ICardEditorModel? cardEditor = null;
            IUIContext? uiContext = null;
            if (withinCardEditor)
            {
                var context = UIContext.Current;
                var currentEditor = context?.CardEditor;
                if (currentEditor is null)
                {
                    throw new InvalidOperationException("Can't use current card editor because it's null.");
                }

                cardEditor = currentEditor;
                uiContext = context;
            }
            else if (specificCardEditor is not null)
            {
                cardEditor = specificCardEditor;
                uiContext = cardEditor.Context;
            }

            if (cardEditor is not null)
            {
                await cardEditor.SaveCardAsync(
                    uiContext,
                    requestInfo,
                    cancellationToken: cancellationToken);

                return cardEditor.LastData.StoreResponse.GetKrProcessLaunchFullResult();
            }

            var request = new CardRequest
            {
                RequestType = KrConstants.LaunchProcessRequestType,
                Info = requestInfo,
            };

            var response = await this.cardRepository.RequestAsync(request, cancellationToken);
            var result = response.GetKrProcessLaunchFullResult();

            await TessaDialog.ShowNotEmptyAsync(result.ValidationResult);

            return result;
        }

        #endregion
    }
}
