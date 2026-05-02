#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.Prompts;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для создания исходящего письма из карточки входящего документа (файловый инструмент).
    /// </summary>
    /// <param name="cardTransactionStrategy"><inheritdoc cref="ICardTransactionStrategy" path="/summary"/></param>
    /// <param name="cardStreamServerRepository"><inheritdoc cref="ICardStreamServerRepository" path="/summary"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="cardFileManager"><inheritdoc cref="ICardFileManager" path="/summary"/></param>
    /// <param name="cardServerPermissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public sealed class OutgoingWriterAiAgentPlugin(
        ICardTransactionStrategy cardTransactionStrategy,
        ICardStreamServerRepository cardStreamServerRepository,
        ICardRepository cardRepository,
        ICardFileManager cardFileManager,
        ICardServerPermissionsProvider cardServerPermissionsProvider,
        IKrPermissionsManager krPermissionsManager) : IAiAgentPlugin
    {
        #region Static Fields And Constants

        /// <summary>
        /// Промпт инструмента.
        /// </summary>
        private const string Prompt =
            $$$"""
              Сегодня: {{{{{AiPromptTemplates.AiTool.CurrentDateName}}}}}.
              Определи, от кого пришло данное письмо, обычно указывается в конце письма в виде Фамилия И.О., или И.О. Фамилия, или Имя Фамилия, чаще всего указывается рядом с должностью этого человека. Письмо начни с обращения к этому человеку. Если нет явного указания на то, кто подписал данное письмо, то не придумывай Фамилию И.О., не пиши ФИО, которой нет в исходном документе.
              Сформируй текст ответного письма. Нужен только текст, не подписывай, от кого это письмо. Ответ напиши с учетом запроса пользователя.
              Запрос пользователя:
              """;

        private static readonly AiToolSettings toolInfo = new()
        {
            ID = "outgoing_write",
            Name = "Написание ответа на письмо",
            Description = "Дополнительные условия: этот инструмент используется только для формирования ответа на приложенное письмо. Не используется для вопросов по документу, формирования аннотации по документу, поиска по документу.",
            Hint = "Позволяет сформировать текст ответного письма по приложенному письму (файлу входящего документа).",
            RequireFile = true,

            PluginName = nameof(OutgoingWriterAiAgentPlugin),
            Prompts = AiPromptsHelper.GetPrompts([new(AiPromptTemplates.AiTool.RootPromptName, Prompt)]),
        };

        private AiCachedPrompts? cachedPrompts;

        /// <summary>
        /// Идентификатор шаблона файла, содержащего исходящее письмо.
        /// </summary>
        private static readonly Guid outgoingLetterTemplateID = new(0x8a4f7f1d, 0x3cc2, 0x4d44, 0xb5, 0xb1, 0xcd, 0x99, 0x53, 0xe2, 0x6a, 0x2a);

        /// <summary>
        /// Ключ в дополнительной информации, передаваемой в запрос на получение контента файла, по которому расположен текст письма, созданный ИИ.
        /// </summary>
        private const string PlaceholderTextKey = "Text";

        #endregion

        #region Fields

        private readonly ICardTransactionStrategy cardTransactionStrategy = NotNullOrThrow(cardTransactionStrategy);
        private readonly ICardStreamServerRepository cardStreamServerRepository = NotNullOrThrow(cardStreamServerRepository);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly ICardFileManager cardFileManager = NotNullOrThrow(cardFileManager);
        private readonly ICardServerPermissionsProvider cardServerPermissionsProvider = NotNullOrThrow(cardServerPermissionsProvider);
        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        #endregion

        #region IAiAgentPlugin Members

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            new([toolInfo]);

        /// <inheritdoc />
        public ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<OutgoingWriterAiAgentPlugin>(toolId, toolInfo.ID);
            return new(toolInfo);
        }

        /// <inheritdoc />
        public ValueTask<AiToolApplicability> IsApplicableAsync(string toolId, AiRequest? request, CancellationToken cancellationToken)
        {
            if (toolId != toolInfo.ID)
            {
                return new (new AiToolApplicability());
            }

            if (request is not null)
            {
                return request.Context?.Type == AiContextType.Card 
                    && request.Context.CardType == DefaultCardTypes.IncomingTypeID 
                    && request.Messages?.Any(static i => i.Role == AiRoles.User 
                        && i.Content?.Any(static j => j.Type == AiMessagePartType.File) is true) is true
                        ? new (new AiToolApplicability { Available = true, Visible = true })
                        : new (new AiToolApplicability());
            }

            return new ( new AiToolApplicability { Available = true });
        }

        /// <inheritdoc />
        public async ValueTask<AiInstructionsResult> GetInstructionsAsync(
            string toolId,
            AiAgentContext context,
            CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<OutgoingWriterAiAgentPlugin>(toolId, toolInfo.ID);

            var validationResult = new ValidationResultBuilder();

            var result = await this.CheckPermissionsAsync(
                validationResult,
                cancellationToken);

            AiHelper.Logger.LogResult(validationResult);

            if (!result)
            {
                context.Response.Message = await LocalizeAsync("$Ai_OutgoingWriterAiAgentPlugin_NotPermissions");
                return new(AiInstructionResultCode.Error);
            }

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, null, cancellationToken);
            this.cachedPrompts = actual;

            var prompt = new AiPromptBuilder()
                .WithTemplates(actual.Templates)
                .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                .WithCurrentDateTime(DateTime.UtcNow + context.Session.ClientUtcOffset, false, context.Session.ClientUICulture)
                .Build();

            return new(prompt);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(
            string toolId,
            string? data,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            AiHelper.ThrowIfInvalidTool<OutgoingWriterAiAgentPlugin>(toolId, toolInfo.ID);

            var text = data?.Trim();

            context.Response.Data = text;
            context.Response.Message = text;
            context.Response.Buttons = [
                AiActions.Create,
                AiActions.Reject];

            return AiRecognitionResult.Confirmation;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(
            string toolId,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            AiHelper.ThrowIfInvalidTool<OutgoingWriterAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Request.Data is not string text)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            Guid? resultingCardID = null;

            var isSuccessful = await this.cardTransactionStrategy.ExecuteInTransactionAsync(
                context.ValidationResult,
                async p =>
                {
                    var newRequest = new CardNewRequest
                    {
                        CardTypeID = DefaultCardTypes.OutgoingTypeID
                    };
                    newRequest.Info[KrConstants.Keys.DocTypeID] = DefaultDocTypes.OutgoingDocTypeID;
                    newRequest.Info[KrConstants.Keys.DocTypeTitle] = DefaultDocTypes.OutgoingDocTypeTitle;
                    newRequest.Info[KrCreateBasedOnHelper.CardIDKey] = context.AiContext!.ID;

                    var newResponse = await this.cardRepository.NewAsync(
                        newRequest,
                        cancellationToken: p.CancellationToken);

                    var validationResultResponse = newResponse.ValidationResult;
                    p.ValidationResult.Add(validationResultResponse);

                    if (!validationResultResponse.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    var card = newResponse.Card;

                    await this.FillCardAsync(
                        card,
                        context,
                        cancellationToken: cancellationToken);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    var cardToStore = card.Clone();
                    cardToStore.RemoveAllButChanged(cardToStore.StoreMode);

                    var storeResponse = await this.cardRepository.StoreAsync(
                        new CardStoreRequest()
                        {
                            Card = cardToStore,
                        },
                        cancellationToken: p.CancellationToken);

                    p.ValidationResult.Add(storeResponse.ValidationResult);
                    if (!storeResponse.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    card.Version = storeResponse.CardVersion;
                    card.RemoveChanges();

                    var contentResult = await this.cardStreamServerRepository.GenerateFileFromTemplateAsync(
                        outgoingLetterTemplateID,
                        card.ID,
                        info: new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            [PlaceholderTextKey] = text,
                        },
                        cancellationToken: p.CancellationToken);

                    p.ValidationResult.Add(contentResult.Response.ValidationResult);

                    if (!contentResult.HasContent
                        || !contentResult.Response.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    await using var container = await this.cardFileManager.CreateContainerAsync(
                        card,
                        cancellationToken: p.CancellationToken);

                    var suggestedFileName = contentResult.Response.TryGetSuggestedFileName() ?? "Outgoing.docx";
                    var size = contentResult.Response.Size;

                    await container.FileContainer
                        .BuildFile(suggestedFileName)
                        .SetContent(
                            contentResult.GetContentOrThrowAsync!,
                            _ => new(size))
                        .AddWithNotificationAsync(cancellationToken: p.CancellationToken);

                    var fileStoreResponse = await container.StoreAsync(
                        (_, request, _) =>
                        {
                            this.cardServerPermissionsProvider.SetFullPermissions(request);
                            return ValueTask.CompletedTask;
                        },
                        cancellationToken: p.CancellationToken);

                    p.ValidationResult.Add(fileStoreResponse.ValidationResult);

                    if (!fileStoreResponse.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    resultingCardID = card.ID;
                },
                cancellationToken: cancellationToken);

            if (!isSuccessful
                || !resultingCardID.HasValue)
            {
                return;
            }

            context.Response.Message = await LocalizeAsync("$Ai_OutgoingWriterAiAgentPlugin_CardCreated");

            context.Response.Action = new AiAutoAction(
                AiActions.OpenCardOnClientID,
                AiAutoActionType.OpenCard,
                new AiAutoActionData()
                {
                    CardID = resultingCardID,
                    CardTypeID = DefaultCardTypes.OutgoingTypeID,
                });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Проверяет разрешено ли выполнение инструмента в соответствии с правами доступа.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если выполнение разрешено, иначе - <see langword="false"/>.</returns>
        private async Task<bool> CheckPermissionsAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardTypeID = DefaultCardTypes.OutgoingTypeID,
                    DocTypeID = DefaultDocTypes.OutgoingDocTypeID,
                    ValidationResult = validationResult,
                    ServiceType = CardServiceType.Client,
                },
                cancellationToken: cancellationToken);

            return permissionsContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.CreateCard),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => true,
                _ => throw ArgumentOutOfRange(permissionsContextResult.Status)
            };
        }

        /// <summary>
        /// Заполняет карточку исходящего документа.
        /// </summary>
        /// <param name="newCard">Карточка.</param>
        /// <param name="context"><inheritdoc cref="AiAgentContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        private async Task FillCardAsync(
            Card newCard,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            newCard.ID = Guid.NewGuid();

            var getResponse = await this.cardRepository.GetAsync(
                new CardGetRequest()
                {
                    CardID = context.AiContext!.ID,
                    RestrictionFlags = CardGetRestrictionFlags.RestrictFiles
                        | CardGetRestrictionFlags.RestrictTasks
                        | CardGetRestrictionFlags.RestrictTaskHistory,
                    GetMode = CardGetMode.ReadOnly,
                },
                cancellationToken: cancellationToken);

            context.ValidationResult.Add(getResponse.ValidationResult);

            if (!getResponse.ValidationResult.IsSuccessful())
            {
                return;
            }

            var sourceCard = getResponse.Card;
            var firstRecipient = sourceCard.Sections
                .TryGet(KrConstants.Recipients.SectionName)
                ?.TryGetRows()
                ?.FirstOrDefault();

            if (firstRecipient is not null)
            {
                var sections = newCard.Sections;
                var dciFields = sections[KrConstants.DocumentCommonInfo.Name].Fields;

                dciFields[KrConstants.DocumentCommonInfo.SignedByID] = firstRecipient[KrConstants.Recipients.UserID];
                dciFields[KrConstants.DocumentCommonInfo.SignedByName] = firstRecipient[KrConstants.Recipients.UserName];
            }
        }

        #endregion
    }
}
