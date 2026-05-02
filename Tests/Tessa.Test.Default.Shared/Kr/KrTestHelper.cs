#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using NUnit.Framework;
using Tessa.Cards;
using Tessa.Cards.Extensions.Templates;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess.ClientCommandInterpreter;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.Helpful;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Предоставляет вспомогательные методы для тестирования процессов.
    /// </summary>
    public static class KrTestHelper
    {
        #region Public Methods

        /// <summary>
        /// Запускает глобальный вторичный процесс аналогично запуску через тайл.
        /// </summary>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="krProcessInstance">Информация о запускаемом процессе.</param>
        /// <param name="raiseErrorWhenExecutionIsForbidden">Значение <see langword="true"/>, если должна быть создана ошибка при невозможности запуска процесса из-за нарушения ограничений (Сообщение при невозможности выполнения процесса), иначе - <see langword="false"/>.</param>
        /// <param name="info">Дополнительная пользовательская информация, которая должна быть передана в запросе на запуск процесса.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат запуска процесса.</returns>
        public static async Task<KrProcessLaunchResult> LaunchGlobalKrProcessAsync(
            ICardRepository cardRepository,
            KrProcessInstance krProcessInstance,
            bool raiseErrorWhenExecutionIsForbidden = false,
            IDictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardRepository);
            ThrowIfNull(krProcessInstance);

            var request = new CardRequest
            {
                RequestType = KrConstants.LaunchProcessRequestType,
            };

            request.SetKrProcessInstance(krProcessInstance);

            if (raiseErrorWhenExecutionIsForbidden)
            {
                request.Info[KrConstants.RaiseErrorWhenExecutionIsForbidden] = BooleanBoxes.True;
            }

            if (info is not null)
            {
                StorageHelper.Merge(info, request.Info);
            }

            var resp = await cardRepository.RequestAsync(request, cancellationToken);
            return resp.GetKrProcessLaunchFullResult();
        }

        /// <summary>
        /// Имитирует цикл: Завершение этапа/действия с отрицательным вариантом завершения, например, задание этапа/действия "Согласование" с вариантом завершения <see cref="DefaultCompletionOptions.Disapprove"/> -&gt; Доработка -&gt; Завершение этапа/действия с положительным вариантом завершения.
        /// </summary>
        /// <typeparam name="TClc">Тип объекта, управляющего жизненным циклом карточки.</typeparam>
        /// <param name="clc">Объект, управляющий жизненным циклом карточки, в которой запущен процесс.</param>
        /// <param name="actionAsync">Асинхронное действие, содержащее завершение этапа/действия, отрицательный вариант завершения которого приводит к переходу к "Доработке".</param>
        /// <param name="beforeNegativeAction">Действие, выполняющееся перед завершением задания типа "Доработка".</param>
        /// <param name="amendingTaskTypeID">Идентификатор типа задания доработки. Если значение не задано, то используется <see cref="DefaultTaskTypes.KrEditTypeID"/>.</param>
        /// <param name="amendingTaskCompletionOptionID">Идентификатор варианта завершения задания доработки, по которому процесс переходит к следующей итерации согласования. Если значение не задано, то используется <see cref="DefaultCompletionOptions.NewApprovalCycle"/>.</param>
        /// <param name="negativeCompleteOptionRepeatCount">Число повторений отрицательного варианта завершения. Значение по умолчанию: 1.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static ValueTask EditingCycleAsync<TClc>(
            TClc clc,
            Func<EditingCycleContext<TClc>, ValueTask> actionAsync,
            Action<TClc>? beforeNegativeAction = null,
            Guid? amendingTaskTypeID = null,
            Guid? amendingTaskCompletionOptionID = null,
            int negativeCompleteOptionRepeatCount = 1,
            CancellationToken cancellationToken = default)
            where TClc : ICardLifecycleCompanion<TClc> =>
            EditingCycleAsync(
                clc,
                actionAsync,
                async (clc2, ct2) =>
                {
                    beforeNegativeAction?.Invoke(clc2);

                    await clc2
                        .GetTaskOrThrow(amendingTaskTypeID ?? DefaultTaskTypes.KrEditTypeID, out var task)
                        .CompleteTask(
                            task,
                            amendingTaskCompletionOptionID ?? DefaultCompletionOptions.NewApprovalCycle)
                        .GoAsync(cancellationToken: ct2);
                },
                negativeCompleteOptionRepeatCount,
                cancellationToken);

        /// <summary>
        /// Имитирует цикл: Завершение этапа/действия с отрицательным вариантом завершения, например, задание этапа "Согласование" с вариантом завершения <see cref="DefaultCompletionOptions.Disapprove"/> -&gt; Обработка отрицательного завершения, например, завершение этапа/действия "Доработка" -&gt; Завершение этапа/действия с положительным вариантом завершения.
        /// </summary>
        /// <typeparam name="TClc">Тип объекта, управляющего жизненным циклом карточки, в которой запущен процесс.</typeparam>
        /// <param name="clc">Объект, управляющий жизненным циклом карточки, в которой запущен процесс.</param>
        /// <param name="actionAsync">Асинхронное действие, содержащее завершение этапа/действия, отрицательный вариант завершения которого приводит к переходу к "Доработке".</param>
        /// <param name="negativeAction">Асинхронное действие, выполняющееся после завершения <paramref name="actionAsync"/> отрицательным вариантом завершения.</param>
        /// <param name="negativeCompleteOptionRepeatCount">Число повторений отрицательного варианта завершения. Значение по умолчанию: 1.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async ValueTask EditingCycleAsync<TClc>(
            TClc clc,
            Func<EditingCycleContext<TClc>, ValueTask> actionAsync,
            Func<TClc, CancellationToken, ValueTask> negativeAction,
            int negativeCompleteOptionRepeatCount = 1,
            CancellationToken cancellationToken = default)
            where TClc : ICardLifecycleCompanion<TClc>
        {
            ThrowIfNull(clc);
            ThrowIfNull(actionAsync);
            ThrowIfNull(negativeAction);

            var currentNegativeCoRepeatCount = 0;
            bool isNegativeCompletionOption;

            do
            {
                isNegativeCompletionOption = currentNegativeCoRepeatCount < negativeCompleteOptionRepeatCount;

                if (!isNegativeCompletionOption)
                {
                    currentNegativeCoRepeatCount = 0;
                }

                await actionAsync(
                    new EditingCycleContext<TClc>(
                        clc,
                        !isNegativeCompletionOption,
                        currentNegativeCoRepeatCount,
                        cancellationToken));

                if (isNegativeCompletionOption)
                {
                    await negativeAction(clc, cancellationToken);

                    currentNegativeCoRepeatCount++;
                }
            } while (isNegativeCompletionOption);
        }

        /// <summary>
        /// Инициализирует объект, управляющий жизненным циклом карточки, диалога.
        /// </summary>
        /// <param name="completionOptionSettings"><inheritdoc cref="CardTaskCompletionOptionSettings" path="/summary"/></param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <returns>Объект, управляющий жизненным циклом карточки, диалога.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public static CardLifecycleCompanion InitializeDialogCard(
            CardTaskCompletionOptionSettings completionOptionSettings,
            ICardLifecycleCompanionDependencies deps)
        {
            ThrowIfNull(completionOptionSettings);
            ThrowIfNull(deps);

            return completionOptionSettings.StoreMode switch
            {
                CardTaskDialogStoreMode.Info or CardTaskDialogStoreMode.Settings => completionOptionSettings.DialogCard is null
                    ? CreateDialogCard(completionOptionSettings, deps)
                    : new CardLifecycleCompanion(
                        completionOptionSettings.DialogCard,
                        deps),
                CardTaskDialogStoreMode.Card => completionOptionSettings.PersistentDialogCardID == Guid.Empty
                    ? CreateDialogCard(completionOptionSettings, deps)
                    : new CardLifecycleCompanion(
                            completionOptionSettings.PersistentDialogCardID,
                            null,
                            null,
                            deps)
                        .Load(),
                _ => throw ArgumentOutOfRange(completionOptionSettings.StoreMode),
            };
        }

        /// <summary>
        /// Планирует создание карточки диалога.
        /// </summary>
        /// <param name="completionOptionSettings"><inheritdoc cref="CardTaskCompletionOptionSettings" path="/summary"/></param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <returns>Объект, управляющий жизненным циклом карточки, диалога.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.<para/>
        /// Действия аналогичны выполняемым обработчиком клиентской команды <see cref="DefaultCommandTypes.ShowAdvancedDialog"/> с учётом выполнения на сервере.
        /// </remarks>
        public static CardLifecycleCompanion CreateDialogCard(
            CardTaskCompletionOptionSettings completionOptionSettings,
            ICardLifecycleCompanionDependencies deps)
        {
            ThrowIfNull(completionOptionSettings);
            ThrowIfNull(deps);

            var info = completionOptionSettings.Info;

            if (completionOptionSettings.PreparedNewCard is not null
                && completionOptionSettings.PreparedNewCardSignature is not null)
            {
                info[CardHelper.NewCardBilletKey] = completionOptionSettings.PreparedNewCard;
                info[CardHelper.NewCardBilletSignatureKey] = completionOptionSettings.PreparedNewCardSignature;
            }

            info[CardTaskDialogHelper.StoreMode] = Int32Boxes.Box((int) completionOptionSettings.StoreMode);

            CardLifecycleCompanion clc;
            var dialogTypeID = completionOptionSettings.DialogTypeID;

            switch (completionOptionSettings.CardNewMethod)
            {
                case CardTaskDialogNewMethod.CardType:
                    clc = new CardLifecycleCompanion(
                            dialogTypeID,
                            null,
                            deps)
                        .Create()
                        .WithInfo(info);
                    break;

                case CardTaskDialogNewMethod.Template:
                    clc = new CardLifecycleCompanion(
                            Guid.Empty,
                            CardHelper.TemplateTypeID,
                            CardHelper.TemplateTypeName,
                            deps)
                        .Create()
                        .WithInfo(info);

                    clc.GetLastPendingAction().Info.SetTemplateCardID(dialogTypeID);
                    break;

                case CardTaskDialogNewMethod.DialogType:
                    Card dialogCard;
                    if (completionOptionSettings.PreparedDialogCard is not null)
                    {
                        dialogCard = completionOptionSettings.PreparedDialogCard;
                        dialogCard.ID = Guid.NewGuid();
                        clc = new CardLifecycleCompanion(dialogCard, deps);
                    }
                    else
                    {
                        clc = new CardLifecycleCompanion(
                            dialogTypeID,
                            null,
                            deps)
                        .Create()
                        .WithInfo(info);
                    }
                    break;

                case CardTaskDialogNewMethod.Form:
                    clc = new CardLifecycleCompanion(
                            WorkflowEngineHelper.WorkflowFormDialogTypeID,
                            null,
                            deps)
                        .Create()
                        .WithInfo(info);
                    break;

                default:
                    throw ArgumentOutOfRange(completionOptionSettings);
            }

            return clc;
        }

        //private async Task<Card?> CreateDialogTypeCardAsync(
        //    CardTaskCompletionOptionSettings completionOptionSettings,
        //    CancellationToken cancellationToken = default)
        //{
        //    var dialogTypeMetadata = await this.cardMetadata.GetMetadataForTypeAsync(completionOptionSettings.DialogTypeID, cancellationToken);
        //    var cardTypes = await dialogTypeMetadata.GetCardTypesAsync(cancellationToken);

        //    var dialogMetadata = await this.cardMetadataBuilder.BuildAsync(
        //        [.. cardTypes],
        //        this.schemeService,
        //        cancellationToken: cancellationToken);

        //    var newRequest = new CardNewRequest { CardTypeID = completionOptionSettings.DialogTypeID };

        //    // дефолтный компонент не вызывает веб-сервис и расширения, он просто конструирует секции по метаинфе
        //    var newResponse = await CardNewComponent.Default.NewAsync(newRequest, dialogMetadata, this.session, cancellationToken);

        //    Card card = newResponse.Card;
        //    CardHelper.GrantAllPermissions(card);

        //    return card;
        //}

        /// <summary>
        /// Создаёт объект, содержащий информацию о завершении глобального диалога.
        /// </summary>
        /// <param name="dialogFileContainer">Контейнер, содержащий карточку диалога и информацию о её файлах.</param>
        /// <param name="coSettings">Параметры диалога.</param>
        /// <param name="buttonAlias">Алиас кнопки - варианта завершения диалога.</param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Кортеж: &lt;Информация о завершении диалога или значение <see langword="null"/>, если произошла ошибка; Результат выполнения операции&gt;.</returns>
        /// <remarks>
        /// Выполняемые действия соответствуют методу
        /// <see cref="M:Tessa.Extensions.Default.Client.Workflow.KrProcess.CommandInterpreter.AdvancedDialogCommandHandler.CompleteDialogAsync"/>.<para/>
        ///
        /// Для завершения диалога связанного с заданием предназначены методы
        /// <see cref="CompleteDialogAsync{T}(T,Func{ICardFileContainer,CancellationToken,ValueTask},string,CardTask,ISession,Guid,CancellationToken)"/>.
        /// </remarks>
        public static async ValueTask<(CardTaskDialogActionResult?, ValidationResult)> CreateRequestInfoForCompleteDialogAsync(
            ICardFileContainer dialogFileContainer,
            CardTaskCompletionOptionSettings coSettings,
            string buttonAlias,
            ISession session,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dialogFileContainer);
            ThrowIfNull(coSettings);
            ThrowIfNull(buttonAlias);
            ThrowIfNull(session);

            var dialogCard = dialogFileContainer.Card;
            var dialogCardFiles = dialogFileContainer.FileContainer.Files;
            var validationResult = new ValidationResultBuilder
            {
                await dialogCardFiles.EnsureAllContentModifiedAsync(cancellationToken),
            };

            if (!validationResult.IsSuccessful())
            {
                return (null, validationResult.Build());
            }

            await CardTaskDialogHelper.SetFileContentToInfoAsync(
                dialogCard,
                new ReadOnlyCollection<IFile>(dialogCardFiles),
                session,
                validationResult,
                cancellationToken);

            var completeDialog = coSettings
                .Buttons
                .Single(i => i.Name == buttonAlias)
                .CompleteDialog;

            var actionResult = new CardTaskDialogActionResult
            {
                TaskID = Guid.Empty,
                PressedButtonName = buttonAlias,
                StoreMode = coSettings.StoreMode,
                KeepFiles = coSettings.KeepFiles,
                CompleteDialog = completeDialog,
            };
            actionResult.SetDialogCard(dialogCard);

            return (actionResult, validationResult.Build());
        }

        /// <summary>
        /// Завершает диалог указанным вариантом завершения.
        /// </summary>
        /// <typeparam name="T">Тип объекта управляющего жизненным циклом основной карточки.</typeparam>
        /// <param name="mainClc">Объект, управляющий жизненным циклом основной карточки.</param>
        /// <param name="dialogCardFileContainerModifyAsync">Метод для настройки контейнера, содержащего информацию по карточке диалога и её файлам, или значение <see langword="null"/>, если настройка не требуется.</param>
        /// <param name="buttonAlias">Алиас кнопки - варианта завершения диалога.</param>
        /// <param name="dialogTask">Завершаемое задание.</param>
        /// <param name="session">Сессия пользователя.</param>
        /// <param name="completionOptionID">Идентификатор варианта завершения задания диалога.<para/>
        /// Стандартные варианты завершения:
        /// <list type="table">
        /// <listheader>
        ///     <description>Подсистема</description>
        ///     <description>Объект</description>
        ///     <description>Значение</description>
        /// </listheader>
        /// <item>
        ///     <description>Маршруты документов</description>
        ///     <description>Этап "Диалог"</description>
        ///     <description><see cref="DefaultCompletionOptions.ShowDialog"/></description>
        /// </item>
        /// <item>
        ///     <description>Редактор бизнес-процессов (Workflow Engine)</description>
        ///     <description>Действие "Диалог"</description>
        ///     <description><see cref="CardTaskDialogHelper.ShowDialogOption"/></description>
        /// </item>
        /// <item>
        ///     <description>Редактор бизнес-процессов (Workflow Engine)</description>
        ///     <description>Действие "Задание" или "Группа заданий"</description>
        ///     <description>Идентификатор варианта завершения для которого настроено открытие диалога.</description>
        /// </item>
        /// </list>
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат выполнения.</returns>
        public static async Task<ValidationResult> CompleteDialogAsync<T>(
            T mainClc,
            Func<ICardFileContainer, CancellationToken, ValueTask>? dialogCardFileContainerModifyAsync,
            string buttonAlias,
            CardTask dialogTask,
            ISession session,
            Guid completionOptionID,
            CancellationToken cancellationToken = default)
            where T : ICardLifecycleCompanion<T>
        {
            ThrowIfNull(mainClc);
            ThrowIfNull(dialogTask);
            ThrowIfNull(session);

            var coSettings = GetCompletionOptionSettings(
                dialogTask,
                completionOptionID);

            // Создание карточки диалога.
            var dialogClc = await InitializeDialogCard(
                    coSettings,
                    mainClc.Dependencies)
                .GoAsync(cancellationToken: cancellationToken);
            var dialogCardFileContainer = await dialogClc.GetCardFileContainerAsync(
                cancellationToken: cancellationToken);

            if (dialogCardFileContainerModifyAsync is not null)
            {
                await dialogCardFileContainerModifyAsync(dialogCardFileContainer, cancellationToken);
            }

            return await CompleteDialogAsync(
                mainClc,
                dialogCardFileContainer,
                buttonAlias,
                coSettings,
                dialogTask,
                session,
                completionOptionID,
                cancellationToken);
        }

        /// <summary>
        /// Завершает диалог указанным вариантом завершения.
        /// </summary>
        /// <param name="mainClc">Объект, управляющий жизненным циклом основной карточки.</param>
        /// <param name="dialogCardFileContainer">Контейнер, содержащий информацию по карточке диалога и её файлам. После выполнения этого метода объект необходимо получить заново, т.к. карточка диалога и её файлы могли быть изменены.</param>
        /// <param name="buttonAlias">Алиас кнопки - варианта завершения диалога.</param>
        /// <param name="coSettings"><inheritdoc cref="CardTaskCompletionOptionSettings" path="/summary"/></param>
        /// <param name="dialogTask">Завершаемое задание.</param>
        /// <param name="session">Сессия пользователя.</param>
        /// <param name="completionOptionID">Идентификатор варианта завершения задания диалога.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Результат выполнения операции.</returns>
        public static async Task<ValidationResult> CompleteDialogAsync<T>(
            T mainClc,
            ICardFileContainer dialogCardFileContainer,
            string buttonAlias,
            CardTaskCompletionOptionSettings coSettings,
            CardTask dialogTask,
            ISession session,
            Guid completionOptionID,
            CancellationToken cancellationToken = default)
            where T : ICardLifecycleCompanion<T>
        {
            ThrowIfNull(mainClc);
            ThrowIfNull(dialogCardFileContainer);
            ThrowIfNull(coSettings);
            ThrowIfNull(dialogTask);
            ThrowIfNull(session);

            var dialogTaskRowID = dialogTask.RowID;
            var dialogCard = dialogCardFileContainer.Card;
            var files = dialogCardFileContainer.FileContainer.Files;
            var validationResult = new ValidationResultBuilder
            {
                await files.EnsureAllContentModifiedAsync(cancellationToken),
            };

            if (!validationResult.IsSuccessful())
            {
                return validationResult.Build();
            }

            try
            {
                var dialogCardClone = dialogCard.Clone();
                var dialogCardID = dialogCard.ID; // Значение необходимо передавать отдельно, т.к. не во всех случаях есть dialogCardClone.

                var button = coSettings
                    .Buttons
                    .Single(i => i.Name == buttonAlias);

                // Кнопка не отправляет запрос на сервер?
                if (button.Cancel)
                {
                    return ValidationResult.Empty;
                }

                switch (coSettings.StoreMode)
                {
                    case CardTaskDialogStoreMode.Info:
                        await CardTaskDialogHelper.SetFileContentToInfoAsync(
                            dialogCardClone,
                            new ReadOnlyCollection<IFile>(files),
                            session,
                            validationResult,
                            cancellationToken);

                        if (!validationResult.IsSuccessful())
                        {
                            return validationResult.Build();
                        }

                        CardTaskDialogHelper.PrepareDialogCardToSave(dialogCardClone);

                        break;
                    case CardTaskDialogStoreMode.Settings:
                        CardTaskDialogHelper.SetFileContentToMainCard(
                            dialogCardClone,
                            dialogTaskRowID,
                            new ReadOnlyCollection<IFile>(files),
                            mainClc.GetCardOrThrow(),
                            (await mainClc.GetCardFileContainerAsync(cancellationToken: cancellationToken))
                            .FileContainer,
                            session);

                        if (CardHelper.HasFilesContentsToSave(dialogCardClone))
                        {
                            CardTaskDialogHelper.PrepareDialogCardToSave(dialogCardClone);

                            var actionResult = new CardTaskDialogActionResult
                            {
                                MainCardID = mainClc.CardID,
                                PressedButtonName = buttonAlias,
                                StoreMode = coSettings.StoreMode,
                                CompleteDialog = button.CompleteDialog,
                                KeepFiles = coSettings.KeepFiles,
                            };

                            actionResult.SetDialogCard(dialogCardClone);

                            await mainClc
                                .ActionTask(
                                    dialogTask,
                                    CardTaskAction.None,
                                    (_, task) =>
                                    {
                                        task.OptionID = completionOptionID;

                                        CardTaskDialogHelper.SetCardTaskDialogActionResult(
                                            task,
                                            actionResult);
                                        return ValueTask.CompletedTask;
                                    })
                                .GoAsync(
                                    i => validationResult.Add(i),
                                    cancellationToken: cancellationToken);

                            if (!validationResult.IsSuccessful())
                            {
                                return validationResult.Build();
                            }

                            // Загрузка карточки диалога после сохранения.
                            // Это необходимо, например, для актуализации ExternalSource у файлов в карточке диалога.
                            // Если этого не сделать, то они перетрутся при повторном сохранении.

                            // Только для серверных тестов: Если в клиенте есть ошибка, но в тестах её нет, то необходимо проверить передаётся ли по ссылке сохраняемый объект и изменяется ли он на сервере. Если да, то необходимо добавить клонирование объекта перед сохранением. Это позволит имитировать сериализацию при выполнении запроса клиент-сервер.

                            // В отличии от клиента в тестах не надо явно загружать карточку.

                            mainClc.GetTaskByRowIDOrThrow(dialogTask.RowID, out dialogTask);

                            coSettings = GetCompletionOptionSettings(
                                dialogTask,
                                completionOptionID);

                            dialogCardClone = NotNullOrThrow(coSettings.DialogCard);
                            CardTaskDialogHelper.PrepareDialogCardToSave(dialogCardClone);

                            // При успешном сохранении подчищаем список файлов и секций для повторного сохранения.
                            var mainCard = mainClc.GetCardOrThrow();
                            foreach (var section in mainCard.Sections)
                            {
                                section.Value.RemoveChanges(
                                    CardRemoveChangesDeletedHandling.Remove);
                            }

                            foreach (var file in mainCard.Files.ToArray())
                            {
                                if (file.State == CardFileState.Deleted)
                                {
                                    mainCard.Files.Remove(file);
                                }
                                else
                                {
                                    file.RemoveChanges(
                                        CardRemoveChangesDeletedHandling.Remove);
                                }
                            }
                        }
                        else
                        {
                            CardTaskDialogHelper.PrepareDialogCardToSave(dialogCardClone);
                        }

                        break;
                    case CardTaskDialogStoreMode.Card:
                        if (CardHelper.HasFilesContentsToSave(dialogCardClone))
                        {
                            var actionResult = new CardTaskDialogActionResult
                            {
                                MainCardID = mainClc.CardID,
                                StoreMode = CardTaskDialogStoreMode.Card,
                                PressedButtonName = buttonAlias,
                                TaskID = dialogTaskRowID,
                                CompleteDialog = button.CompleteDialog,
                            };
                            actionResult.SetDialogCardID(dialogCardID);

                            var storeResult = await dialogCardFileContainer.StoreAsync(
                                (_, storeRequest, _) =>
                                {
                                    CardTaskDialogHelper.SetCardTaskDialogActionResult(
                                        storeRequest,
                                        actionResult);
                                    return ValueTask.CompletedTask;
                                },
                                cancellationToken: cancellationToken);
                            validationResult.Add(storeResult.ValidationResult);

                            if (!validationResult.IsSuccessful())
                            {
                                return validationResult.Build();
                            }

                            dialogCardClone.Version = storeResult.CardVersion;
                            dialogCardClone.RemoveChanges();
                            dialogCardClone = null;
                        }
                        else if (!dialogCardClone.HasChanges()
                                 && dialogCardClone.StoreMode != CardStoreMode.Insert)
                        {
                            dialogCardClone = null;
                        }
                        else
                        {
                            var storeModeCard = dialogCardClone.StoreMode;
                            if (storeModeCard == CardStoreMode.Update)
                            {
                                dialogCardClone.UpdateStates();
                            }

                            dialogCardClone.RemoveAllButChanged(storeModeCard);
                        }

                        break;
                    default:
                        throw ArgumentOutOfRange(coSettings.StoreMode);
                }

                var actionResultFinally = new CardTaskDialogActionResult
                {
                    MainCardID = mainClc.CardID,
                    PressedButtonName = buttonAlias,
                    StoreMode = coSettings.StoreMode,
                    CompleteDialog = button.CompleteDialog,
                    KeepFiles = coSettings.KeepFiles,
                };

                if (dialogCardClone is null)
                {
                    actionResultFinally.SetDialogCardID(dialogCardID);
                }
                else
                {
                    actionResultFinally.SetDialogCard(dialogCardClone);
                }

                await mainClc
                    .GetTaskByRowIDOrThrow(dialogTaskRowID, out dialogTask)
                    .CompleteTask(
                        dialogTask,
                        completionOptionID,
                        (task, _) =>
                        {
                            CardTaskDialogHelper.SetCardTaskDialogActionResult(
                                task,
                                actionResultFinally);
                            return ValueTask.CompletedTask;
                        })
                    .GoAsync(
                        i => validationResult.Add(i),
                        cancellationToken: cancellationToken);
            }
            finally
            {
                if (coSettings.StoreMode == CardTaskDialogStoreMode.Settings)
                {
                    CardTaskDialogHelper.ClearDialogFiles(
                        dialogCard,
                        dialogCardFileContainer.FileContainer);
                }
            }

            return validationResult.Build();
        }

        /// <summary>
        /// Запускает глобальный вторичный процесс при выполнении которого открывается диалог.
        /// </summary>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="processID">Идентификатор вторичного процесса.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Кортеж &lt;Информация об экземпляре процесса; Параметры диалога&gt;.</returns>
        public static Task<(KrProcessInstance, CardTaskCompletionOptionSettings)> LaunchGlobalKrProcessWithDialogAsync(
            ICardRepository cardRepository,
            Guid processID,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardRepository);

            var process = KrProcessBuilder
                .CreateProcess()
                .SetProcess(processID)
                .Build();

            return LaunchGlobalKrProcessWithDialogAsync(
                cardRepository,
                process,
                cancellationToken);
        }

        /// <summary>
        /// Запускает глобальный вторичный процесс, при выполнении которого, открывается диалог.
        /// </summary>
        /// <param name="cardRepository">Репозиторий для управления карточками.</param>
        /// <param name="krProcessInstance">Информация о запускаемом процессе.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Кортеж &lt;Информация об экземпляре процесса; Параметры диалога&gt;.</returns>
        public static async Task<(KrProcessInstance, CardTaskCompletionOptionSettings)> LaunchGlobalKrProcessWithDialogAsync(
            ICardRepository cardRepository,
            KrProcessInstance krProcessInstance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardRepository);
            ThrowIfNull(krProcessInstance);

            var result = await LaunchGlobalKrProcessAsync(
                cardRepository,
                krProcessInstance,
                cancellationToken: cancellationToken);
            ValidationAssert.HasEmpty(result.ValidationResult);

            // Получение из ответа на запрос процесса и параметров диалога.
            // Действия аналогичны выполняемым в Tessa.Extensions.Default.Client.Workflow.KrProcess.CommandInterpreter.KrAdvancedDialogCommandHandler с учётом выполнения на сервере.
            var clientCommands = result.CardResponse.GetKrProcessClientCommands();
            ThrowIfNull(clientCommands);

            var command = clientCommands.Single(i => i.CommandType == DefaultCommandTypes.ShowAdvancedDialog);

            var processInstanceStorage = command.Parameters.Get<Dictionary<string, object?>>(KrConstants.Keys.ProcessInstance);
            ThrowIfNull(processInstanceStorage);

            var coSettingsStorage = command.Parameters.Get<Dictionary<string, object?>>(KrConstants.Keys.CompletionOptionSettings);
            ThrowIfNull(coSettingsStorage);

            var processInstance = new KrProcessInstance(processInstanceStorage);
            var coSettings = new CardTaskCompletionOptionSettings(coSettingsStorage);

            return (processInstance, coSettings);
        }

        /// <summary>
        /// Возвращает информацию о процессах, запущенных по карточке с указанным идентификатором.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="mainCardID">Идентификатор карточки, для которой необходимо получить информацию по запущенным процессам.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Информация по процессам.</returns>
        public static async Task<List<WorkflowProcessInfoForTest>> GetWorkflowProcessAsync(
            IDbScope dbScope,
            Guid mainCardID,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using (dbScope.Create())
            {
                var db = dbScope.Db;

                var queryBuilder = dbScope.BuilderFactory
                    .Select()
                    .C("wp", "RowID")
                    .C("wp", "TypeName")
                    .C("wp", "Params")
                    .From("WorkflowProcesses", "wp").NoLock()
                    .InnerJoin(CardSatelliteHelper.SatellitesSectionName, "s").NoLock()
                    .On().C("s", "ID").Equals().C("wp", "ID")
                    .Where()
                    .C("s", CardSatelliteHelper.MainCardIDColumn).Equals().P(CardSatelliteHelper.MainCardIDColumn);

                db.SetCommand(
                        queryBuilder.Build(),
                        db.Parameter(CardSatelliteHelper.MainCardIDColumn, mainCardID, DataType.Guid))
                    .LogCommand();

                var results = new List<WorkflowProcessInfoForTest>();
                await using (var reader = await db.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var id = reader.GetGuid(0);
                        var typeName = reader.GetString(1);
                        var processParamsJson = await reader.GetSequentialNullableStringAsync(2, db.Dbms, cancellationToken);

                        results.Add(new WorkflowProcessInfoForTest(id, typeName, processParamsJson));
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// Проверяет контент файла, находящегося в диалоге.
        /// </summary>
        /// <param name="storeMode"><inheritdoc cref="CardTaskDialogStoreMode" path="/summary"/></param>
        /// <param name="cardFile">Файл, контент которого требуется проверить.</param>
        /// <param name="getDialogCardFileContainerAsync">Функция, возвращающая файловый контейнер диалога.</param>
        /// <param name="expectedContent">Ожидаемый контент.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async ValueTask CheckDialogFileContentAsync(
            CardTaskDialogStoreMode storeMode,
            CardFile cardFile,
            Func<ValueTask<ICardFileContainer>> getDialogCardFileContainerAsync,
            byte[] expectedContent,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardFile);
            ThrowIfNull(getDialogCardFileContainerAsync);
            ThrowIfNull(expectedContent);

            byte[] fileContent;

            switch (storeMode)
            {
                case CardTaskDialogStoreMode.Info:
                    fileContent = await CardTaskDialogHelper.GetFileContentFromBase64Async(
                        NotNullOrThrow(CardTaskDialogHelper.GetFileContentFromInfo(cardFile)),
                        cancellationToken);
                    break;
                case CardTaskDialogStoreMode.Settings:
                case CardTaskDialogStoreMode.Card:
                    var dialogCardFileContainer = NotNullOrThrow(await getDialogCardFileContainerAsync());
                    fileContent = await GetFileContentFromFileContainer(cardFile, dialogCardFileContainer, cancellationToken);
                    break;
                default:
                    throw ArgumentOutOfRange(storeMode);
            }

            TestHelper.AssertThatBytesAreEqual(fileContent, expectedContent);
        }

        /// <summary>
        /// Получает контент файла из файлового контейнера.
        /// </summary>
        /// <param name="cardFile">Файл, контент которого требуется получить.</param>
        /// <param name="dialogCardFileContainer">Файловый контейнер диалога.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Контент файла.</returns>
        public static async Task<byte[]> GetFileContentFromFileContainer(
            CardFile cardFile,
            ICardFileContainer dialogCardFileContainer,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardFile);
            ThrowIfNull(dialogCardFileContainer);

            var version = TestHelper.GetFileVersion(
                dialogCardFileContainer.FileContainer.Files,
                cardFile.RowID,
                cardFile.VersionRowID);

            return await TestHelper.GetFileVersionContentAsync(
                version,
                cancellationToken);
        }

        /// <summary>
        /// Возвращает параметры диалога имеющие указанный идентификатор. Создаёт исключение, если параметры не найдены.
        /// </summary>
        /// <param name="task">Задание, содержащее параметры.</param>
        /// <param name="completionOptionID">Идентификатор варианта завершения.</param>
        /// <returns>Параметры варианта завершения диалога.</returns>
        public static CardTaskCompletionOptionSettings GetCompletionOptionSettings(
            CardTask task,
            Guid completionOptionID) =>
            CardTaskDialogHelper.GetCompletionOptionSettings(
                task,
                completionOptionID) ??
            throw new InvalidOperationException($"Not {nameof(CardTaskCompletionOptionSettings)} found for completion option with ID={completionOptionID:B}.");

        /// <summary>
        /// Добавляет в <paramref name="sb"/> информацию по этапам.
        /// </summary>
        /// <param name="sb"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="rows">Строки, содержащие информацию по этапам.</param>
        /// <param name="isDetailed">Значение <see langword="true"/>, если необходимо добавить подробную информацию по этапам, иначе только основную.</param>
        public static void AddStageRowsInfo(
            StringBuilder sb,
            IReadOnlyCollection<CardRow> rows,
            bool isDetailed = false)
        {
            ThrowIfNull(sb);
            ThrowIfNull(rows);

            if (rows.Count == 0)
            {
                return;
            }

            var tableRows = rows
                .Select(static (i, j) => new string?[]
                {
                    FormatNullable(i.TryGetRowID(), "D"),
                    i.State.ToString(),
                    FormatNullable(i.Get<string>(KrConstants.KrStages.NameField)),
                    i.Get<string>(KrConstants.KrStages.StageTypeCaption),
                    i.Get<Guid>(KrConstants.KrStages.StageTypeID).ToString("D"),
                    i.Get<string>(KrConstants.KrStages.StateName),
                    i.Get<int>(KrConstants.KrStages.StateID).ToString(),
                    i.Get<int>(KrConstants.KrStages.Order).ToString(),
                    FormatNullable(i.TryGet<string>(KrConstants.KrStages.BasedOnStageTemplateName)),
                    FormatNullable(i.TryGet<int?>(KrConstants.KrStages.BasedOnStageTemplateOrder)),
                    GroupPosition.GetByID(i.TryGet<int?>(KrConstants.KrStages.BasedOnStageTemplateGroupPositionID)).ToString(),
                })
                .ToArray();

            TestTextHelper.PrintTable(
                sb,
                tableRows,
                [
                    "RowID",
                    "RowState",
                    "Name",
                    "StageTypeCaption",
                    "StageTypeID",
                    "StageStateName",
                    "StageStateID",
                    "Order",
                    "TemplateName",
                    "TemplateOrder",
                    "GroupPosition",
                ]);

            if (isDetailed
                && tableRows.Length > 0)
            {
                sb
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("Detailed stage rows:");

                sb.AppendElements(
                    rows,
                    Environment.NewLine,
                    static (sb, value) =>
                        StorageHelper.Print(sb, value));
            }
        }

        /// <summary>
        /// Возвращает идентификатор карточки сателлита вторичного процесса по его идентификатору.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="secondaryProcessID">Идентификатор вторичного процесса.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Идентификатор карточки сателлита вторичного процесса или значение <see langword="null"/>, если процесс с заданным идентификатором не найден.</returns>
        public static async Task<Guid?> GetKrSecondarySatelliteIDByProcessIDAsync(
            IDbScope dbScope,
            Guid secondaryProcessID,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);

            await using var _ = dbScope.Create();
            var db = dbScope.Db;
            return await db
                .SetCommand(
                    dbScope.BuilderFactory
                        .Select()
                        .C("ID")
                        .From("WorkflowProcesses").NoLock()
                        .Where().C("RowID").Equals().P("ProcessID")
                        .Build(),
                    db.Parameter("ProcessID", secondaryProcessID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<Guid?>(cancellationToken);
        }

        /// <summary>
        /// Возвращает карточку сателлита процесса.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="secondaryProcessID">Идентификатор процесса.</param>
        /// <param name="satelliteTypeID">Идентификатор типа сателлита.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Карточка сателлита процесса.</returns>
        public static async Task<Card> GetKrSatelliteAsync(
            IDbScope dbScope,
            ICardRepository cardRepository,
            Guid secondaryProcessID,
            Guid satelliteTypeID,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);
            ThrowIfNull(cardRepository);

            var krSecondarySatelliteID = await GetKrSecondarySatelliteIDByProcessIDAsync(
                dbScope,
                secondaryProcessID,
                cancellationToken);

            if (!krSecondarySatelliteID.HasValue)
            {
                Assert.Fail($"The secondary process with ID={secondaryProcessID} is not running.");
            }

            var response = await cardRepository.GetAsync(
                new CardGetRequest
                {
                    CardID = krSecondarySatelliteID.Value,
                    CardTypeID = satelliteTypeID,
                    GetMode = CardGetMode.ReadOnly,
                    RestrictionFlags = CardGetRestrictionValues.Satellite,
                },
                cancellationToken);

            ValidationAssert.IsSuccessful(response.ValidationResult);

            return response.Card;
        }

        /// <summary>
        /// Возвращает состояние документа.
        /// </summary>
        /// <param name="clc"><inheritdoc cref="ICardLifecycleCompanion" path="/summary"/></param>
        /// <returns><inheritdoc cref="KrState" path="/summary"/></returns>
        public static KrState GetState(ICardLifecycleCompanion clc) =>
            GetState(NotNullOrThrow(clc).GetCardOrThrow());

        /// <summary>
        /// Возвращает состояние документа.
        /// </summary>
        /// <param name="card">Карточка.</param>
        /// <returns><inheritdoc cref="KrState" path="/summary"/></returns>
        public static KrState GetState(Card card)
        {
            var aci = NotNullOrThrow(card).GetApprovalInfoSection();
            return (KrState) aci.RawFields.Get<int>(KrConstants.KrApprovalCommonInfo.StateID);
        }

        #endregion
    }
}
