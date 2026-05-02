#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.Wf
{
    /// <summary>
    /// Расширение на модификацию метаданных для типов заданий "Задача".
    /// Переносит метаданные типа задания WfResolution в типы WfResolutionProject, WfResolutionControl и WfResolutionChild.
    /// </summary>
    public sealed class WfCardMetadataExtension :
        CardTypeMetadataExtension
    {
        #region Constructors

        /// <inheritdoc cref="CardTypeMetadataExtension(ICardMetadata)"/>
        public WfCardMetadataExtension(ICardMetadata clientCardMetadata)
            : base(clientCardMetadata)
        {
        }

        /// <inheritdoc cref="CardTypeMetadataExtension()"/>
        public WfCardMetadataExtension()
            : base()
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task ModifyTypes(ICardMetadataExtensionContext context)
        {
            var resolutionType = await this.TryGetCardTypeAsync(context, DefaultTaskTypes.WfResolutionTypeID).ConfigureAwait(false);
            if (resolutionType is null)
            {
                return;
            }
            // сделать необходимые части объекта глобальными.
            MakeGlobal(resolutionType, context);

            if (!resolutionType.IsSealed)
            {
                // тип получен с сервера, скорее всего для предпросмотра в редакторе типов карточек
                await CopyMainFormToOtherFormsAsync(resolutionType, context.CancellationToken);
            }

            foreach (Guid taskTypeID in WfHelper.MetadataResolutionTaskTypeIDList)
            {
                var taskType = await this.TryGetCardTypeAsync(context, taskTypeID, useServerMetadataOnClient: false).ConfigureAwait(false);
                if (taskType is null)
                {
                    continue;
                }

                if (taskType.ID == DefaultTaskTypes.WfResolutionProjectTypeID)
                {
                    await CopyResolutionTaskTypeToProjectAsync(resolutionType, taskType, context.CancellationToken);
                }
                else
                {
                    await CopyResolutionTaskTypeAsync(resolutionType, taskType, context.CancellationToken);
                }
            }
        }

        #endregion

        #region Private Methods

        private static void MakeGlobal(CardType cardType, ICardMetadataExtensionContext context)
        {
            using var ctx = new CardGlobalReferencesContext(context, cardType);
            var mainForm = cardType.TryGetMainFormForTask();
            if (mainForm is not null)
            {
                // регистрация глобальных объектов.
                // все блоки главной формы.
                mainForm.Blocks.MakeGlobal(ctx, mainForm);
                // все формы, кроме главной.
                cardType.Forms.Where(p => !p.Equals(mainForm)).MakeGlobal(ctx);
            }
            else
            {
                cardType.Forms.MakeGlobal(ctx);
            }
            // все варианты завершения.
            cardType.CompletionOptions.MakeGlobal(ctx);
            // все валидаторы.
            cardType.Validators.MakeGlobal(ctx);
            // все расширения типа.
            cardType.Extensions.MakeGlobal(ctx);
        }

        private static async ValueTask CopyMainFormToOtherFormsAsync(CardType sourceType, CancellationToken cancellationToken = default)
        {
            var mainForm = sourceType.TryGetMainFormForTask();
            if (mainForm is null)
            {
                return;
            }

            // т.к. формы уже глобальные, то копировать их не нужно.
            foreach (CardTypeNamedForm namedForm in sourceType.Forms.Where(p => !p.Equals(mainForm)))
            {
                await mainForm.Blocks.InsertNonOrderableAsync(namedForm.Blocks, cancellationToken: cancellationToken).ConfigureAwait(false);
                StorageHelper.Merge(mainForm.FormSettings, namedForm.FormSettings);
            }
        }

        private static async ValueTask CopyResolutionTaskTypeAsync(CardType sourceType, CardType targetType, CancellationToken cancellationToken = default)
        {
            var sourceMainForm = sourceType.TryGetMainFormForTask();
            var targetMainForm = targetType.TryGetMainFormForTask();

            if (sourceMainForm is not null
                && targetMainForm is not null)
            {
                await sourceMainForm.Blocks.InsertNonOrderableAsync(targetMainForm.Blocks, cancellationToken: cancellationToken).ConfigureAwait(false);
                StorageHelper.Merge(sourceMainForm.FormSettings, targetMainForm.FormSettings);
            }

            await sourceType.Forms.InsertNonOrderableAsync(targetType.Forms, targetType.Forms.Count, 1, cancellationToken: cancellationToken).ConfigureAwait(false);

            await sourceType.SchemeItems.CopyToTheBeginningOfAsync(targetType.SchemeItems, cancellationToken).ConfigureAwait(false);
            await sourceType.CompletionOptions.InsertNonOrderableAsync(targetType.CompletionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            await sourceType.Validators.InsertNonOrderableAsync(targetType.Validators, cancellationToken: cancellationToken).ConfigureAwait(false);
            await sourceType.Extensions.InsertNonOrderableAsync(targetType.Extensions, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask CopyResolutionTaskTypeToProjectAsync(CardType sourceType, CardType targetType, CancellationToken cancellationToken = default)
        {
            var sendForm = sourceType.Forms.FirstOrDefault(x => x.Name.Equals(WfHelper.SendToPerformerFormName, StringComparison.Ordinal));
            if (sendForm is not null)
            {
                targetType.Forms.Add(sendForm);
            }

            var sendCompletionOption = sourceType.CompletionOptions.FirstOrDefault(x => x.TypeID == DefaultCompletionOptions.SendToPerformer);
            if (sendCompletionOption is not null)
            {
                targetType.CompletionOptions.Add(sendCompletionOption);
            }

            await sourceType.SchemeItems.CopyToTheBeginningOfAsync(targetType.SchemeItems, cancellationToken).ConfigureAwait(false);
            await sourceType.Validators.InsertNonOrderableAsync(targetType.Validators, cancellationToken: cancellationToken).ConfigureAwait(false);
            await sourceType.Extensions.InsertNonOrderableAsync(targetType.Extensions, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
