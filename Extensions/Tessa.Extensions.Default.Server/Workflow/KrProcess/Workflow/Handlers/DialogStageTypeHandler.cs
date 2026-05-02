#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SourceBuilders;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess.ClientCommandInterpreter;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.DialogDescriptor"/>.
    /// </summary>
    public class DialogStageTypeHandler :
        StageTypeHandlerBase
    {
        #region Nested Types

        /// <summary>
        /// Базовый класс, контекста скрипта этапа "Диалог".
        /// </summary>
        public abstract class ScriptContextBase
        {
            #region Fields

            /// <summary>
            /// Стратегия доступа к карточке диалога.
            /// </summary>
            protected readonly IMainCardAccessStrategy dialogCardAccessStrategy;

            /// <inheritdoc cref="ISession" path="/summary"/>
            protected readonly ISession session;

            #endregion

            #region Constructors

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="ScriptContextBase"/>.
            /// </summary>
            /// <param name="dialogCardAccessStrategy">Стратегия доступ к карточке диалога.</param>
            /// <param name="buttonName"><inheritdoc cref="ButtonName" path="/summary"/></param>
            /// <param name="storeMode"><inheritdoc cref="StoreMode" path="/summary"/></param>
            /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
            protected ScriptContextBase(
                IMainCardAccessStrategy dialogCardAccessStrategy,
                string? buttonName,
                CardTaskDialogStoreMode storeMode,
                ISession session)
            {
                this.dialogCardAccessStrategy = NotNullOrThrow(dialogCardAccessStrategy);
                this.ButtonName = buttonName;
                this.StoreMode = storeMode;
                this.session = NotNullOrThrow(session);
            }

            #endregion

            #region Properties

            /// <summary>
            /// Алиас нажатой кнопки.
            /// </summary>
            public string? ButtonName { get; }

            /// <inheritdoc cref="CardTaskDialogStoreMode" path="/summary"/>
            public CardTaskDialogStoreMode StoreMode { get; }

            #endregion

            #region Public Methods

            /// <summary>
            /// Возвращает карточку диалога.
            /// </summary>
            /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
            /// <remarks>Карточка диалога или значение <see langword="null"/>, если произошла ошибка.</remarks>
            public ValueTask<Card?> GetDialogCardAsync(CancellationToken cancellationToken = default) => this.dialogCardAccessStrategy.GetCardAsync(cancellationToken: cancellationToken);

            /// <summary>
            /// Возвращает контент файла карточки диалога с временем жизни: запрос или задание.
            /// </summary>
            /// <param name="file">Информация о файле, контент которого требуется получить.</param>
            /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
            /// <returns>Массив байт, являющийся контентом указанного файла.</returns>
            public async ValueTask<byte[]> GetFileContentAsync(
                CardFile file,
                CancellationToken cancellationToken = default)
            {
                ThrowIfNull(file);

                if (this.StoreMode is not CardTaskDialogStoreMode.Info
                    and not CardTaskDialogStoreMode.Settings)
                {
                    throw new InvalidOperationException(
                        $"Method \"{nameof(this.GetFileContentAsync)}\" not allowed for dialogs with \"{this.StoreMode}\" store mode.{Environment.NewLine}" +
                        $"Use \"{nameof(this.GetFileContainerAsync)}\" instead.");
                }

                if (CardTaskDialogHelper.GetFileContentFromInfo(file) is { } fileContent)
                {
                    return await CardTaskDialogHelper.GetFileContentFromBase64Async(
                        fileContent,
                        cancellationToken);
                }

                return Array.Empty<byte>();
            }

            /// <summary>
            /// Задаёт контент файла карточки диалога с временем жизни запрос.
            /// </summary>
            /// <param name="file">Информация о файле, контент которого требуется задать.</param>
            /// <param name="content">Задаваемый контент файла.</param>
            public void SetFileContent(
                CardFile file,
                byte[] content)
            {
                ThrowIfNull(file);
                ThrowIfNull(content);

                if (this.StoreMode != CardTaskDialogStoreMode.Info)
                {
                    throw new InvalidOperationException(
                        $"Method \"{nameof(this.SetFileContent)}\" not allowed for dialogs with \"{this.StoreMode}\" store mode.{Environment.NewLine}" +
                        $"Use \"{nameof(this.GetFileContainerAsync)}\" instead.");
                }

                CardTaskDialogHelper.SetFileContentToInfo(
                    file,
                    content,
                    this.session.User);
            }

            /// <summary>
            /// Возвращает файловый контейнер для карточки диалога с временем жизни карточка и задание.
            /// </summary>
            /// <param name="validationResult">Результат валидации.</param>
            /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
            /// <returns>Файловый контейнер или значение <see langword="null"/>, если произошла ошибка.</returns>
            public ValueTask<ICardFileContainer?> GetFileContainerAsync(
                IValidationResultBuilder? validationResult = null,
                CancellationToken cancellationToken = default)
            {
                if (this.StoreMode is not CardTaskDialogStoreMode.Card
                    and not CardTaskDialogStoreMode.Settings)
                {
                    throw new InvalidOperationException(
                        $"Method \"{nameof(this.GetFileContainerAsync)}\" not allowed for dialogs with \"{this.StoreMode}\" store mode.{Environment.NewLine}" +
                        $"Use \"{nameof(this.GetFileContentAsync)}\" or \"{nameof(this.SetFileContent)}\" instead.");
                }

                return this.dialogCardAccessStrategy.GetFileContainerAsync(
                    validationResult,
                    cancellationToken);
            }

            #endregion
        }

        /// <summary>
        /// Контекст скрипта валидации.
        /// </summary>
        public sealed class ScriptContext :
            ScriptContextBase
        {
            #region Constructors

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="ScriptContext"/>.
            /// </summary>
            /// <param name="dialogCardAccessStrategy">Стратегия доступ к карточке диалога.</param>
            /// <param name="buttonName"><inheritdoc cref="ScriptContextBase.ButtonName" path="/summary"/></param>
            /// <param name="storeMode"><inheritdoc cref="ScriptContextBase.StoreMode" path="/summary"/></param>
            /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
            public ScriptContext(
                IMainCardAccessStrategy dialogCardAccessStrategy,
                string? buttonName,
                CardTaskDialogStoreMode storeMode,
                ISession session)
                : base(
                      dialogCardAccessStrategy,
                      buttonName,
                      storeMode,
                      session)
            {
            }

            #endregion

            #region Properties

            /// <summary>
            /// Возвращает или задаёт флаг, позволяющий прервать обработку диалога без закрытия окна диалога.
            /// </summary>
            public bool Cancel { get; set; }

            /// <summary>
            /// Возвращает или задаёт флаг, позволяющий отменить завершение диалога с закрытием окна диалога.
            /// </summary>
            public bool CompleteDialog { get; set; }

            #endregion
        }

        /// <summary>
        /// Контекст скрипта сохранения.
        /// </summary>
        public sealed class SavingScriptContext :
            ScriptContextBase
        {
            #region Constructors

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="SavingScriptContext"/>.
            /// </summary>
            /// <param name="dialogCard">Стратегия доступ к карточке диалога.</param>
            /// <param name="buttonName"><inheritdoc cref="ScriptContextBase.ButtonName" path="/summary"/></param>
            /// <param name="storeMode"><inheritdoc cref="ScriptContextBase.StoreMode" path="/summary"/></param>
            /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
            public SavingScriptContext(
                IMainCardAccessStrategy dialogCard,
                string? buttonName,
                CardTaskDialogStoreMode storeMode,
                ISession session)
                : base(
                      dialogCard,
                      buttonName,
                      storeMode,
                      session)
            {
            }

            #endregion
        }

        #endregion

        #region Constants And Static Fields

        /// <summary>
        /// Ключ, по которому в <see cref="WorkflowProcess.InfoStorage"/> хранится соответствие алиас диалога - идентификатор карточки диалога. Тип значения: <see cref="IDictionary{TKey, TValue}"/>, где TKey - <see cref="string"/>, TValue - <see cref="object"/>.
        /// </summary>
        public const string DialogsProcessInfoKey = StorageHelper.SystemKeyPrefix + "Dialogs";

        /// <summary>
        /// Дескриптор метода "Сценарий валидации".
        /// </summary>
        public static readonly KrExtraSourceDescriptor ValidationMethodDescriptor = new KrExtraSourceDescriptor("DialogActionScript")
        {
            DisplayName = "$UI_KrDialog_Script",
            ParameterName = "Dialog",
            ParameterType = $"global::{typeof(DialogStageTypeHandler).FullName}.{nameof(ScriptContext)}",
            ScriptField = KrConstants.KrDialogStageTypeSettingsVirtual.DialogActionScript
        };

        /// <summary>
        /// Дескриптор метода "Сценарий сохранения".
        /// </summary>
        public static readonly KrExtraSourceDescriptor SavingMethodDescriptor = new KrExtraSourceDescriptor("SavingDialogScript")
        {
            DisplayName = "$UI_KrDialog_SavingScript",
            ParameterName = "Dialog",
            ParameterType = $"global::{typeof(DialogStageTypeHandler).FullName}.{nameof(SavingScriptContext)}",
            ScriptField = KrConstants.KrDialogStageTypeSettingsVirtual.DialogCardSavingScript
        };

        #endregion

        #region Constructors

        public DialogStageTypeHandler(
            IKrScope krScope,
            IKrStageTemplateCompilationCache compilationCache,
            IUnityContainer unityContainer,
            [Dependency(CardRepositoryNames.Default)] ICardRepository cardRepositoryDefault,
            IDbScope dbScope,
            IKrProcessCache processCache,
            ISignatureProvider signatureProvider,
            IStageTasksRevoker tasksRevoker,
            IKrTypesCache typesCache,
            ICardFileManager cardFileManager,
            ICardRepository cardRepository,
            Func<ICardTaskCompletionOptionSettingsBuilder> ctcBuilderFactory,
            ISession session)
        {
            this.KrScope = NotNullOrThrow(krScope);
            this.CompilationCache = NotNullOrThrow(compilationCache);
            this.UnityContainer = NotNullOrThrow(unityContainer);
            this.CardRepositoryDefault = NotNullOrThrow(cardRepositoryDefault);
            this.DbScope = NotNullOrThrow(dbScope);
            this.ProcessCache = NotNullOrThrow(processCache);
            this.SignatureProvider = NotNullOrThrow(signatureProvider);
            this.TasksRevoker = NotNullOrThrow(tasksRevoker);
            this.TypesCache = NotNullOrThrow(typesCache);
            this.CardFileManager = NotNullOrThrow(cardFileManager);
            this.CardRepository = NotNullOrThrow(cardRepository);
            this.CtcBuilderFactory = NotNullOrThrow(ctcBuilderFactory);
            this.Session = NotNullOrThrow(session);
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; }

        /// <inheritdoc cref="IKrStageTemplateCompilationCache" path="/summary"/>
        protected IKrStageTemplateCompilationCache CompilationCache { get; }

        /// <inheritdoc cref="IUnityContainer" path="/summary"/>
        protected IUnityContainer UnityContainer { get; }

        /// <inheritdoc cref="ICardRepository" path="/summary"/>
        protected ICardRepository CardRepositoryDefault { get; }

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        protected IDbScope DbScope { get; }

        /// <inheritdoc cref="IKrProcessCache" path="/summary"/>
        protected IKrProcessCache ProcessCache { get; }

        /// <inheritdoc cref="ISignatureProvider" path="/summary"/>
        protected ISignatureProvider SignatureProvider { get; }

        /// <inheritdoc cref="ISignatureProvider" path="/summary"/>
        protected IStageTasksRevoker TasksRevoker { get; }

        /// <inheritdoc cref="IKrTypesCache" path="/summary"/>
        protected IKrTypesCache TypesCache { get; }

        /// <inheritdoc cref="ICardFileManager" path="/summary"/>
        protected ICardFileManager CardFileManager { get; }

        /// <inheritdoc cref="ICardRepository" path="/summary"/>
        protected ICardRepository CardRepository { get; }

        /// <inheritdoc cref="ICardTaskCompletionOptionSettingsBuilder" path="/summary"/>
        protected Func<ICardTaskCompletionOptionSettingsBuilder> CtcBuilderFactory { get; }

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task BeforeInitializationAsync(
            IStageTypeHandlerContext context)
        {
            await base.BeforeInitializationAsync(context);

            if (!(await this.ProcessCache.GetAllRuntimeStagesAsync(context.CancellationToken))
                .TryGetValue(context.Stage.ID, out var runtimeStage)
                || string.IsNullOrWhiteSpace(runtimeStage.RuntimeSourceBefore))
            {
                // Если нет скрипта, то загружать карточку нет смысла.
                return;
            }

            // Тут возможны варианты:
            // Диалог неперсистентный и квазиперсистентный: создаем новую, без вариантов.
            // Диалог персистентный: либо берем готовую карточку по алиасу, либо создаем новую.

            var stage = context.Stage;
            var settingsStorage = stage.SettingsStorage;
            var storeMode = (CardTaskDialogStoreMode) settingsStorage.TryGet<int>(KrConstants.KrDialogStageTypeSettingsVirtual.CardStoreModeID);
            var alias = settingsStorage.TryGet<string>(KrConstants.KrDialogStageTypeSettingsVirtual.DialogAlias);

            IMainCardAccessStrategy newCardAccessStrategy;
            Guid persistentCardID;

            if (storeMode == CardTaskDialogStoreMode.Card
                && !string.IsNullOrEmpty(alias)
                && (persistentCardID = GetAliasedDialogID(context, alias)) != Guid.Empty
                && await KrProcessHelper.CardExistsAsync(
                    persistentCardID,
                    this.DbScope,
                    context.CancellationToken))
            {
                newCardAccessStrategy = new KrScopeMainCardAccessStrategy(
                    persistentCardID,
                    this.KrScope,
                    context.ValidationResult);
            }
            else
            {
                newCardAccessStrategy = new ObviousMainCardAccessStrategy(
                    async (validationResult, ct) =>
                    {
                        var cardNewRequest = new CardNewRequest();

                        var typeID = settingsStorage.TryGet<Guid?>(KrConstants.KrDialogStageTypeSettingsVirtual.DialogTypeID);
                        if (typeID.HasValue)
                        {
                            var docType = (await this.TypesCache.GetDocTypesAsync(ct))
                                .FirstOrDefault(x => x.ID == typeID.Value);

                            if (docType is not null)
                            {
                                cardNewRequest.Info[KrConstants.Keys.DocTypeID] = typeID;
                                cardNewRequest.Info[KrConstants.Keys.DocTypeTitle] = docType.Caption;

                                typeID = docType.CardTypeID;
                            }
                        }
                        else
                        {
                            var templateID = settingsStorage.TryGet<Guid?>(KrConstants.KrDialogStageTypeSettingsVirtual.TemplateID);

                            if (templateID.HasValue)
                            {
                                typeID = await KrProcessHelper.GetTemplateCardTypeAsync(
                                    templateID.Value,
                                    this.DbScope,
                                    ct);

                                if (!typeID.HasValue)
                                {
                                    validationResult.AddError(
                                        this,
                                        await LocalizeFormatAsync(
                                            "$KrProcess_ErrorMessage_ErrorFormat2",
                                            KrErrorHelper.GetTraceTextFromStage(stage),
                                            "$KrStages_CreateCard_TemplateNotFound"));
                                    return null;
                                }
                            }
                            else
                            {
                                validationResult.AddError(
                                    this,
                                    await LocalizeFormatAsync(
                                        "$KrProcess_ErrorMessage_ErrorFormat2",
                                        KrErrorHelper.GetTraceTextFromStage(stage),
                                        "$KrStages_Dialog_TemplateAndTypeNotSpecified"));
                                return null;
                            }
                        }

                        cardNewRequest.CardTypeID = typeID;

                        var newResponse = await this.CardRepositoryDefault.NewAsync(
                            cardNewRequest,
                            ct);

                        var validationResultResponse = newResponse.TryGetValidationResult();

                        if (validationResultResponse is not null)
                        {
                            validationResult.Add(validationResultResponse);
                        }

                        var card = newResponse.TryGetCard();

                        if (card is not null
                            && card.ID == Guid.Empty)
                        {
                            card.ID = Guid.NewGuid();
                        }

                        return card;
                    },
                    this.CardFileManager,
                    context.ValidationResult);
            }

            this.KrScope.AddDisposableObject(newCardAccessStrategy);
            context.Stage.InfoStorage[KrConstants.Keys.NewCard] = newCardAccessStrategy;
        }

        /// <inheritdoc />
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            return context.RunnerMode switch
            {
                KrProcessRunnerMode.Sync => await this.StartSyncDialogAsync(context),
                KrProcessRunnerMode.Async => await this.StartAsyncDialogAsync(context),
                _ => throw ArgumentOutOfRange(context.RunnerMode),
            };
        }

        /// <inheritdoc />
        public override async Task<StageHandlerResult> HandleResurrectionAsync(
            IStageTypeHandlerContext context)
        {
            CardTaskDialogActionResult? actionResult;
            string sourceName;
            switch (context.CardExtensionContext)
            {
                case ICardStoreExtensionContext storeContext:
                    var card = storeContext.Request.Card;
                    actionResult = CardTaskDialogHelper.GetCardTaskDialogActionResult(card.Info);
                    sourceName = $"{nameof(card)}.{nameof(card.Info)}";
                    break;
                case ICardRequestExtensionContext requestContext:
                    actionResult = CardTaskDialogHelper.GetCardTaskDialogActionResult(requestContext.Request);
                    sourceName = $"{nameof(requestContext)}.{nameof(requestContext.Request)}";
                    break;
                default:
                    return StageHandlerResult.CompleteResult;
            }

            if (actionResult is null)
            {
                context.ValidationResult.AddError(this, $"No parameter of type {nameof(CardTaskDialogActionResult)} is not set in the {sourceName}.");
                return StageHandlerResult.EmptyResult;
            }

            var dialogCardAccessStrategy = this.GetCard(
                actionResult,
                context);
            this.KrScope.AddDisposableObject(dialogCardAccessStrategy);

            if (context.Stage.TemplateID.HasValue)
            {
                var compilationObject = await this.CompilationCache.GetAsync(
                    context.Stage.TemplateID.Value,
                    cancellationToken: context.CancellationToken);

                var inst = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrRuntimeClass,
                        SourceIdentifiers.StageAlias,
                        context.Stage.ID),
                    context.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return StageHandlerResult.EmptyResult;
                }

                if (inst is not null)
                {
                    await HandlerHelper.InitScriptContextAsync(
                        this.UnityContainer,
                        inst,
                        context);

                    var scriptContext = new ScriptContext(
                        dialogCardAccessStrategy,
                        actionResult.PressedButtonName,
                        actionResult.StoreMode,
                        this.Session)
                    {
                        Cancel = false,
                        CompleteDialog = true,
                    };

                    await inst.InvokeExtraAsync(
                        ValidationMethodDescriptor.MethodName,
                        scriptContext);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    if (scriptContext.Cancel)
                    {
                        ValidationSequence
                            .Begin(context.ValidationResult)
                            .Error(DefaultValidationKeys.CancelDialog)
                            .End();
                        return StageHandlerResult.CancelProcessResult;
                    }
                }
            }

            if (actionResult.StoreMode == CardTaskDialogStoreMode.Settings)
            {
                await UserAPIHelper.PrepareFilesInSettingsDialogCardForStoreAsync(
                    this.DbScope,
                    this.CardRepository,
                    context.MainCardAccessStrategy,
                    dialogCardAccessStrategy,
                    actionResult.TaskID,
                    actionResult.KeepFiles,
                    this.Session,
                    context.ValidationResult,
                    context.CancellationToken);
            }

            return StageHandlerResult.CompleteResult;
        }

        /// <inheritdoc />
        public override async Task<StageHandlerResult> HandleTaskCompletionAsync(
            IStageTypeHandlerContext context)
        {
            var task = NotNullOrThrow(context.TaskInfo).Task;
            if (task.OptionID == DefaultCompletionOptions.Complete)
            {
                return StageHandlerResult.CompleteResult;
            }

            if (task.OptionID != DefaultCompletionOptions.ShowDialog)
            {
                context.ValidationResult.AddError(this, $"Unsupported completion option (ID = \"{task.OptionID:B}\").");
                return StageHandlerResult.EmptyResult;
            }

            var actionInfo = CardTaskDialogHelper.GetCardTaskDialogActionResult(task);
            if (actionInfo is null)
            {
                context.ValidationResult.AddError(this, $"No parameter of type {nameof(CardTaskDialogActionResult)} is not set in the dialog task (ID = \"{task.RowID:B}\").");
                return StageHandlerResult.EmptyResult;
            }

            var coInfo = CardTaskDialogHelper.GetCompletionOptionSettings(task, DefaultCompletionOptions.ShowDialog);
            if (coInfo is null)
            {
                context.ValidationResult.AddError(this, $"No parameter of type {nameof(CardTaskCompletionOptionSettings)} is not set in the dialog task (ID = \"{task.RowID:B}\").");
                return StageHandlerResult.EmptyResult;
            }

            if (!string.IsNullOrEmpty(coInfo.DialogAlias)
                && coInfo.StoreMode == CardTaskDialogStoreMode.Card
                && coInfo.PersistentDialogCardID != Guid.Empty)
            {
                AddAliasedDialog(context, coInfo.DialogAlias, coInfo.PersistentDialogCardID);
            }

            var dialogCardAccessStrategy = this.GetCard(
                actionInfo,
                context);
            this.KrScope.AddDisposableObject(dialogCardAccessStrategy);

            Card? updatedSettingsCard = null;

            if (actionInfo.StoreMode is CardTaskDialogStoreMode.Settings
                or CardTaskDialogStoreMode.Card
                && context.Stage.TemplateID.HasValue)
            {
                var compilationObject = await this.CompilationCache.GetAsync(
                    context.Stage.TemplateID.Value,
                    cancellationToken: context.CancellationToken);

                var savingScriptInstance = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrRuntimeClass,
                        SourceIdentifiers.StageAlias,
                        context.Stage.ID),
                    context.ValidationResult);

                if (savingScriptInstance is not null)
                {
                    await HandlerHelper.InitScriptContextAsync(
                        this.UnityContainer,
                        savingScriptInstance,
                        context);

                    var savingScriptContext = new SavingScriptContext(
                        dialogCardAccessStrategy,
                        actionInfo.PressedButtonName,
                        actionInfo.StoreMode,
                        this.Session);

                    await savingScriptInstance.InvokeExtraAsync(
                        SavingMethodDescriptor.MethodName,
                        savingScriptContext);
                }

                if (!context.ValidationResult.IsSuccessful())
                {
                    return StageHandlerResult.EmptyResult;
                }

                if (actionInfo.StoreMode == CardTaskDialogStoreMode.Settings
                    && dialogCardAccessStrategy.WasUsed)
                {
                    updatedSettingsCard = await dialogCardAccessStrategy.GetCardAsync(
                        cancellationToken: context.CancellationToken);
                }
            }

            var scriptContext = new ScriptContext(
                dialogCardAccessStrategy,
                actionInfo.PressedButtonName,
                actionInfo.StoreMode,
                this.Session)
            {
                Cancel = false,
                CompleteDialog = actionInfo.CompleteDialog,
            };

            if (context.Stage.TemplateID.HasValue)
            {
                var compilationObject = await this.CompilationCache.GetAsync(
                    context.Stage.TemplateID.Value,
                    cancellationToken: context.CancellationToken);

                var validationScriptInstance = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrRuntimeClass,
                        SourceIdentifiers.StageAlias,
                        context.Stage.ID),
                    context.ValidationResult);

                if (validationScriptInstance is not null)
                {
                    await HandlerHelper.InitScriptContextAsync(
                        this.UnityContainer,
                        validationScriptInstance,
                        context);

                    await validationScriptInstance.InvokeExtraAsync(
                        ValidationMethodDescriptor.MethodName,
                        scriptContext);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    if (scriptContext.Cancel)
                    {
                        ValidationSequence
                            .Begin(context.ValidationResult)
                            .Error(DefaultValidationKeys.CancelDialog)
                            .End();
                        return StageHandlerResult.InProgressResult;
                    }

                    if (actionInfo.StoreMode == CardTaskDialogStoreMode.Settings
                        && dialogCardAccessStrategy.WasUsed)
                    {
                        updatedSettingsCard = await dialogCardAccessStrategy.GetCardAsync(
                            cancellationToken: context.CancellationToken);
                    }
                }

                if (!context.ValidationResult.IsSuccessful())
                {
                    return StageHandlerResult.EmptyResult;
                }
            }

            if (actionInfo.StoreMode == CardTaskDialogStoreMode.Settings)
            {
                await UserAPIHelper.PrepareFilesInSettingsDialogCardForStoreAsync(
                    this.DbScope,
                    this.CardRepository,
                    context.MainCardAccessStrategy,
                    dialogCardAccessStrategy,
                    actionInfo.TaskID,
                    actionInfo.KeepFiles,
                    this.Session,
                    context.ValidationResult,
                    context.CancellationToken);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return StageHandlerResult.EmptyResult;
                }
            }

            if (this.KrScope.Exists
                && context.MainCardID.HasValue)
            {
                if (scriptContext.CompleteDialog)
                {
                    var taskCopy = new CardTask(StorageHelper.Clone(task.GetStorage()));
                    taskCopy.RemoveChanges();
                    taskCopy.Action = CardTaskAction.Complete;
                    taskCopy.State = CardRowState.Deleted;
                    taskCopy.OptionID = DefaultCompletionOptions.Complete;

                    var co = NotNullOrThrow(CardTaskDialogHelper.GetCompletionOptionSettings(
                        taskCopy,
                        DefaultCompletionOptions.ShowDialog));

                    CardTaskDialogButtonInfo? pressedButton;
                    if (!string.IsNullOrEmpty(actionInfo.PressedButtonName)
                        && (pressedButton = co.Buttons.FirstOrDefault(
                            i => string.Equals(i.Name, actionInfo.PressedButtonName, StringComparison.Ordinal))) is not null
                        && !string.IsNullOrEmpty(pressedButton.Caption))
                    {
                        taskCopy.Result = pressedButton.Caption;
                    }

                    if (updatedSettingsCard is not null)
                    {
                        updatedSettingsCard.RemoveChanges();
                        co.DialogCard = updatedSettingsCard;
                    }

                    var mainCard = await this.KrScope.GetMainCardAsync(
                        context.MainCardID.Value,
                        cancellationToken: context.CancellationToken);

                    if (mainCard is null)
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    mainCard.Tasks.Add(taskCopy);
                }
                else if (updatedSettingsCard is not null)
                {
                    var taskCopy = new CardTask(StorageHelper.Clone(task.GetStorage()));
                    taskCopy.RemoveChanges();
                    taskCopy.Action = CardTaskAction.None;
                    taskCopy.OptionID = null;
                    taskCopy.State = CardRowState.Modified;
                    taskCopy.Flags |= CardTaskFlags.HistoryItemCreated;

                    var co = NotNullOrThrow(CardTaskDialogHelper.GetCompletionOptionSettings(
                        taskCopy,
                        DefaultCompletionOptions.ShowDialog));

                    updatedSettingsCard.RemoveChanges();
                    co.DialogCard = updatedSettingsCard;

                    var mainCard = await this.KrScope.GetMainCardAsync(
                        context.MainCardID.Value,
                        cancellationToken: context.CancellationToken);

                    if (mainCard is null)
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    mainCard.Tasks.Add(taskCopy);
                }
            }

            return StageHandlerResult.InProgressResult;
        }

        /// <inheritdoc />
        public override Task<bool> HandleStageInterruptAsync(IStageTypeHandlerContext context) =>
            this.TasksRevoker.RevokeAllStageTasksAsync(new StageTaskRevokerContext(
                context,
                context.ValidationResult,
                context.CancellationToken));

        /// <inheritdoc />
        public override async Task AfterPostprocessingAsync(IStageTypeHandlerContext context)
        {
            await base.AfterPostprocessingAsync(context);

            if (context.Stage.InfoStorage.TryGetValue(KrConstants.Keys.NewCard, out var newCardObj))
            {
                context.Stage.InfoStorage.Remove(KrConstants.Keys.NewCard);
                if (newCardObj is IMainCardAccessStrategy cardAccessStrategy)
                {
                    await cardAccessStrategy.DisposeAsync();
                }
            }
        }

        #endregion

        #region protected

        /// <summary>
        /// Запускает диалог в синхронном режиме.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected async Task<StageHandlerResult> StartSyncDialogAsync(
            IStageTypeHandlerContext context)
        {
            var stage = context.Stage;
            var storeMode = (CardTaskDialogStoreMode?) stage
                .SettingsStorage
                .TryGet<int?>(KrConstants.KrDialogStageTypeSettingsVirtual.CardStoreModeID)
                ?? CardTaskDialogStoreMode.Info;

            if (storeMode != CardTaskDialogStoreMode.Info)
            {
                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(stage),
                        "$KrStages_Dialog_StartingSyncDialogWithNotInfoStoreMode"));
                return StageHandlerResult.EmptyResult;
            }

            var coSettings = await this.CreateCompletionOptionSettingsAsync(context);
            if (coSettings is null
                || context.SecondaryProcess is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var cardID = context.MainCardID ?? Guid.Empty;
            var processID = context.SecondaryProcess.ID;

            var serializedProcess = KrProcessHelper.SerializeWorkflowProcess(
                context.WorkflowProcess);

            var signature = KrProcessHelper.SignWorkflowProcess(
                serializedProcess,
                cardID,
                processID,
                this.SignatureProvider);

            var processInstance = new KrProcessInstance(
                processID,
                context.MainCardID,
                serializedProcess,
                signature);

            await this.PrepareNewDialogCardAsync(
                stage,
                coSettings,
                null,
                null,
                context.ValidationResult,
                context.CancellationToken);

            this.KrScope.TryAddClientCommand(
                new KrProcessClientCommand(
                    DefaultCommandTypes.ShowAdvancedDialog,
                    new Dictionary<string, object?>
                    {
                        [KrConstants.Keys.ProcessInstance] = processInstance.GetStorage(),
                        [KrConstants.Keys.CompletionOptionSettings] = coSettings.GetStorage(),
                    }));

            return StageHandlerResult.CancelProcessResult;
        }

        /// <summary>
        /// Запускает диалог в асинхронном режиме.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected async Task<StageHandlerResult> StartAsyncDialogAsync(
            IStageTypeHandlerContext context)
        {
            var performer = context.Stage.Performer;

            if (performer is null)
            {
                return StageHandlerResult.SkipResult;
            }

            var coSettings = await this.CreateCompletionOptionSettingsAsync(context);
            if (coSettings is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var taskID = Guid.NewGuid();
            await this.PrepareNewDialogCardAsync(
                context.Stage,
                coSettings,
                context.MainCardAccessStrategy,
                taskID,
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return StageHandlerResult.EmptyResult;
            }

            var taskGroupRowID = await HandlerHelper.GetTaskHistoryGroupAsync(
                context,
                this.KrScope,
                context.ValidationResult,
                context.CancellationToken);

            var (kindID, kindCaption) = HandlerHelper.GetTaskKind(context);
            await context.WorkflowAPI!.SendTaskAsync(
                DefaultTaskTypes.KrShowDialogTypeID,
                context.Stage.SettingsStorage.TryGet<string>(KrConstants.KrDialogStageTypeSettingsVirtual.TaskDigest),
                performer.PerformerID,
                performer.PerformerName,
                context.ValidationResult,
                taskRowID: taskID,
                modifyTaskAction: (task, _) =>
                {
                    task.GroupRowID = taskGroupRowID;
                    task.Planned = context.Stage.Planned;
                    task.PlannedWorkingDays = context.Stage.Planned.HasValue ? null : context.Stage.TimeLimitOrDefault;
                    task.Flags |= CardTaskFlags.CreateHistoryItem;
                    WorkflowCommonHelper.SetTaskKind(task, kindID, kindCaption, context);
                    CardTaskDialogHelper.SetCompletionOptionSettings(task, coSettings);

                    return ValueTask.CompletedTask;
                },
                cancellationToken: context.CancellationToken);

            return StageHandlerResult.InProgressResult;
        }

        /// <summary>
        /// Возвращает стратегию доступа карточки диалога.
        /// </summary>
        /// <param name="actionInfo"><inheritdoc cref="CardTaskDialogActionResult" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Стратегия доступа к карточке диалога.</returns>
        protected IMainCardAccessStrategy GetCard(
            CardTaskDialogActionResult actionInfo,
            IStageTypeHandlerContext context)
        {
            return actionInfo.StoreMode switch
            {
                CardTaskDialogStoreMode.Info => new ObviousMainCardAccessStrategy(actionInfo.DialogCard, this.CardFileManager, context.ValidationResult),
                CardTaskDialogStoreMode.Settings => new ObviousMainCardAccessStrategy(actionInfo.DialogCard, this.CardFileManager, context.ValidationResult),
                CardTaskDialogStoreMode.Card => new KrScopeMainCardAccessStrategy(actionInfo.DialogCardID, this.KrScope, context.ValidationResult),
                _ => throw ArgumentOutOfRange(actionInfo.StoreMode),
            };
        }

        /// <summary>
        /// Создаёт параметры диалога.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Параметры диалога или значение <see langword="null"/>, если произошла ошибка.</returns>
        /// <remarks>Ошибки записываются в <see cref="IStageTypeHandlerContext.ValidationResult"/>.</remarks>
        protected async ValueTask<CardTaskCompletionOptionSettings?> CreateCompletionOptionSettingsAsync(
            IStageTypeHandlerContext context)
        {
            var stage = context.Stage;
            var settingsStorage = stage.SettingsStorage;

            var storeModeInt = settingsStorage.TryGet<int?>(KrConstants.KrDialogStageTypeSettingsVirtual.CardStoreModeID);
            if (!storeModeInt.HasValue)
            {
                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(stage),
                        "$KrStages_Dialog_CardStoreModeNotSpecified"));
            }

            var openModeInt = settingsStorage.TryGet<int?>(KrConstants.KrDialogStageTypeSettingsVirtual.OpenModeID);
            if (!openModeInt.HasValue)
            {
                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(stage),
                        "$KrStages_Dialog_CardOpenModeNotSpecified"));
            }

            var dialogTypeID = settingsStorage.TryGet<Guid?>(KrConstants.KrDialogStageTypeSettingsVirtual.DialogTypeID);
            var cardNewMethod = CardTaskDialogNewMethod.CardType;

            if (!dialogTypeID.HasValue)
            {
                var templateID = settingsStorage.TryGet<Guid?>(KrConstants.KrDialogStageTypeSettingsVirtual.TemplateID);
                if (templateID.HasValue)
                {
                    dialogTypeID = templateID;
                    cardNewMethod = CardTaskDialogNewMethod.Template;
                }
                else
                {
                    context.ValidationResult.AddError(
                        this,
                        await LocalizeFormatAsync(
                            "$KrProcess_ErrorMessage_ErrorFormat2",
                            KrErrorHelper.GetTraceTextFromStage(stage),
                            "$KrStages_Dialog_TemplateAndTypeNotSpecified"));
                }
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var ctcBuilder = this.CtcBuilderFactory();

            var buttonsSettings = settingsStorage.TryGet<IList>(KrConstants.KrDialogButtonSettingsVirtual.Synthetic);
            if (buttonsSettings is not null)
            {
                foreach (var buttonStorage in buttonsSettings.Cast<Dictionary<string, object?>>())
                {
                    var button = new CardTaskDialogButtonInfo
                    {
                        Name = buttonStorage.TryGet<string>(KrConstants.KrDialogButtonSettingsVirtual.Name),
                        CardButtonType = (CardButtonType) buttonStorage.TryGet<int>(KrConstants.KrDialogButtonSettingsVirtual.TypeID),
                        Caption = buttonStorage.TryGet<string>(KrConstants.KrDialogButtonSettingsVirtual.Caption),
                        Icon = buttonStorage.TryGet<string>(KrConstants.KrDialogButtonSettingsVirtual.Icon),
                        Cancel = buttonStorage.TryGet<bool>(KrConstants.KrDialogButtonSettingsVirtual.Cancel),
                        Order = buttonStorage.TryGet<int>(KrConstants.KrDialogButtonSettingsVirtual.Order),
                    };
                    ctcBuilder.AddButton(button);
                }
            }

            var coSettings = await ctcBuilder
                .SetCompletionOption(DefaultCompletionOptions.ShowDialog)
                .SetDialogType(dialogTypeID!.Value)
                .SetTaskButtonCaption(settingsStorage.TryGet<string>(KrConstants.KrDialogStageTypeSettingsVirtual.ButtonName))
                .SetDialogName(settingsStorage.TryGet<string>(KrConstants.KrDialogStageTypeSettingsVirtual.DialogName))
                .SetDialogAlias(settingsStorage.TryGet<string>(KrConstants.KrDialogStageTypeSettingsVirtual.DialogAlias))
                .SetDialogCaption(settingsStorage.TryGet<string>(KrConstants.KrDialogStageTypeSettingsVirtual.DisplayValue))
                .SetStoreMode((CardTaskDialogStoreMode) storeModeInt!.Value)
                .SetOpenMode((CardTaskDialogOpenMode) openModeInt!.Value)
                .SetKeepFiles(settingsStorage.TryGet<bool>(KrConstants.KrDialogStageTypeSettingsVirtual.KeepFiles))
                .SetCardNewMethod(cardNewMethod)
                .SetIsCloseWithoutConfirmation(settingsStorage.TryGet<bool>(KrConstants.KrDialogStageTypeSettingsVirtual.IsCloseWithoutConfirmation))
                .SetNotDisplayTabs(settingsStorage.TryGet<bool>(KrConstants.KrDialogStageTypeSettingsVirtual.NotDisplayTabs))
                .BuildAsync(context.ValidationResult, context.CancellationToken);

            if (!string.IsNullOrEmpty(coSettings.DialogAlias)
                && coSettings.StoreMode == CardTaskDialogStoreMode.Card)
            {
                var persistentCardID = GetAliasedDialogID(context, coSettings.DialogAlias);
                coSettings.PersistentDialogCardID = persistentCardID;
            }

            return coSettings;
        }

        /// <summary>
        /// Сохраняет идентификатор карточки диалога по его алиасу в персистентном хранилище параметров процесса (<see cref="WorkflowProcess.InfoStorage"/>).
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="alias">Алиас диалога.</param>
        /// <param name="dialogCardID">Идентификатор карточки диалога.</param>
        protected static void AddAliasedDialog(
            IStageTypeHandlerContext context,
            string alias,
            Guid dialogCardID)
        {
            var processInfo = context.WorkflowProcess.InfoStorage;

            if (!processInfo.TryGetValue(DialogsProcessInfoKey, out var dialogsStorageObj)
                || dialogsStorageObj is not IDictionary<string, object?> dialogsStorage)
            {
                dialogsStorage = new Dictionary<string, object?>(StringComparer.Ordinal);
                processInfo[DialogsProcessInfoKey] = dialogsStorage;
            }

            dialogsStorage[alias] = dialogCardID.ToString("N");
        }

        /// <summary>
        /// Возвращает идентификатор карточки диалога по его алиасу.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="alias">Алиас диалога.</param>
        /// <returns>Идентификатор карточки диалога или значение <see cref="Guid.Empty"/>, если его не удалось получить.</returns>
        protected static Guid GetAliasedDialogID(
            IStageTypeHandlerContext context,
            string alias)
        {
            var processInfo = context.WorkflowProcess.InfoStorage;

            if (processInfo.TryGetValue(DialogsProcessInfoKey, out var dialogsStorageObj)
                && dialogsStorageObj is IDictionary<string, object?> dialogsStorage
                && dialogsStorage.TryGetValue(alias, out var cardIDObj)
                && cardIDObj is string cardIDStr
                && Guid.TryParse(cardIDStr, out var cardID))
            {
                return cardID;
            }

            return Guid.Empty;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Инициализирует параметры диалога информацией о подготовленной карточке хранящейся в <see cref="Stage.InfoStorage"/> этапа по ключу <see cref="KrConstants.Keys.NewCard"/>.
        /// </summary>
        /// <param name="stage">Этап из которого загружается информация по подготовленной карточке.</param>
        /// <param name="coSettings"><inheritdoc cref="CardTaskCompletionOptionSettings" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить выполнение асинхронной задачи.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task PrepareNewDialogCardAsync(
            Stage stage,
            CardTaskCompletionOptionSettings coSettings,
            IMainCardAccessStrategy? mainCardAccessStrategy,
            Guid? taskID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            if (!stage.InfoStorage.TryGetValue(KrConstants.Keys.NewCard, out var newCardObj))
            {
                return;
            }

            stage.InfoStorage.Remove(KrConstants.Keys.NewCard);

            if (newCardObj is not IMainCardAccessStrategy { WasUsed: true } dialogCardAccessStrategy)
            {
                return;
            }

            var card = await dialogCardAccessStrategy.GetCardAsync(
                cancellationToken: cancellationToken);

            if (card is null)
            {
                return;
            }

            if (coSettings.StoreMode == CardTaskDialogStoreMode.Card
                && dialogCardAccessStrategy.WasFileContainerUsed)
            {
                coSettings.PersistentDialogCardID = card.ID;

                this.KrScope.AddCard(card);
                this.KrScope.AddCardFileContainer(
                    await dialogCardAccessStrategy.GetFileContainerAsync(
                        cancellationToken: cancellationToken));

                // Для диалога с временем жизни "Карточка", содержащим файлы,
                // не надо создавать карточку-заготовку.
                return;
            }
            else if (coSettings.StoreMode == CardTaskDialogStoreMode.Settings
                && mainCardAccessStrategy is not null
                && taskID.HasValue)
            {
                await UserAPIHelper.PrepareFilesInSettingsDialogCardForStoreAsync(
                    this.DbScope,
                    this.CardRepository,
                    mainCardAccessStrategy,
                    dialogCardAccessStrategy,
                    taskID.Value,
                    coSettings.KeepFiles,
                    this.Session,
                    validationResult,
                    cancellationToken);

                if (!validationResult.IsSuccessful())
                {
                    return;
                }
            }

            var files = card.TryGetFiles()?.Clone();
            card.RemoveAllButChanged();
            card.Files = files;

            var cardBytes = Encoding.UTF8.GetBytes(card.ToTypedJson());
            var cardSignature = this.SignatureProvider.Sign(cardBytes);

            coSettings.PreparedNewCard = cardBytes;
            coSettings.PreparedNewCardSignature = cardSignature;
        }

        #endregion
    }
}
