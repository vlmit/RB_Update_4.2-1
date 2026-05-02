#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.Prompts;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для улучшения текста.
    /// </summary>
    public class TextEnhancementAiAgentPlugin: IAiAgentPlugin
    {
        #region Constants

        private const string ToolPrompt =
            """
            Ты умный помощник, который может выполнять действия только при помощи настроенных инструментов.
            По запросу пользователя преобразуй его текст, не теряя ключевой сути.
            Необходимо: исправить опечатки, стилистические и пунктуационные ошибки, привести текст к формальному деловому виду.
            Не добавляй вступлений, обращений, подписи и т.п., только исправь текст.
            Текст должен быть без форматирования (не выделяй жирным, курсивом и т.п.).
            ВАЖНО: при исправлении текста не выдумывай новых деталей и фактов, можно исправлять и переформулировать только то, что содержится в запросах пользователя.
            """;

        private const string FromFieldKey = "FromField";

        private static readonly AiAction completeSessionAction = new(AiActions.RejectID, "$Ai_TextEnhancementPlugin_CompleteSession", false);

        #endregion

        #region Fields

        private static readonly AiToolSettings toolInfo = new()
        {
            ID = "text_enhancement",
            Name = "Улучшение текста",
            Description =
                """
                Инструмент служит для улучшения текста (исправления опечаток, приведения к деловому виду).
                Этот инструмент используется только в случае, если в запросе пользователя явно указано: исправь текст, улучши текст, измени текст, замени текст.
                """,
            Hint = "Инструмент исправляет опечатки и приводит текст к деловому стилю.",
            PluginName = nameof(TextEnhancementAiAgentPlugin),
            Prompts = { [AiPromptTemplates.AiTool.RootPromptName] = ToolPrompt, },
            Infinite = true,
        };

        private AiCachedPrompts? cachedPrompts;

        #endregion

        #region IAiAgentPlugin Members

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            new([toolInfo]);

        public ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<TextEnhancementAiAgentPlugin>(toolId, toolInfo.ID);
            return new(toolInfo);
        }

        /// <inheritdoc />
        public ValueTask<AiToolApplicability> IsApplicableAsync(string toolId, AiRequest? request, CancellationToken cancellationToken)
        {
            if (toolId != toolInfo.ID)
            {
                return new (new AiToolApplicability());
            }

            // Инструмент может быть вызван в глобальном чате, чате карточки, по кнопке в текстовом поле.
            if (request is not null)
            {
                switch (request.Context?.Type)
                {
                    case null or AiContextType.None:
                        return new ( new AiToolApplicability { Available = true, Visible = false });
                    case AiContextType.Card:
                        if (request.Context.Info.TryGet<bool?>(FromFieldKey) ?? false)
                        {
                            return new ( new AiToolApplicability { Available = true, Visible = true });
                        }
                        return new (new AiToolApplicability { Available = true });
                    default:
                        return new(new AiToolApplicability());
                }
            }

            return new ( new AiToolApplicability { Available = true });
        }

        /// <inheritdoc />
        public async ValueTask<AiInstructionsResult> GetInstructionsAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<TextEnhancementAiAgentPlugin>(toolId, toolInfo.ID);

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, null, cancellationToken);
            this.cachedPrompts = actual;

            var prompt = new AiPromptBuilder()
                .WithTemplates(actual.Templates)
                .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                .Build();

            return new(prompt);
        }

        /// <inheritdoc />
        public ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FileDiscussionAiAgentPlugin>(toolId, toolInfo.ID);
            if (string.IsNullOrWhiteSpace(data))
            {
                context.ValidationResult.AddError("$Ai_Plugins_Validation_NoResponse");
                return new(AiRecognitionResult.Error);
            }

            // Передаем чистый ответ от ИИ без какой-либо обработки
            context.Response.Message = data;

            switch (context.Request.Context?.Type)
            {
                case null or AiContextType.None:
                    // Если плагин вызван в глобальном чате или в чате карточки то после каждого последнего сообщения от ИИ выводим кнопку "Завершить работу инструмента".
                    context.Response.Buttons = [completeSessionAction];
                    return new(AiRecognitionResult.Confirmation);
                case AiContextType.Card:
                    // Если выполняется инструмент по кнопке из текстового поля, то после каждого последнего сообщения от ИИ выведем кнопку "Применить".
                    if (context.Request.Context.Info.TryGet<bool?>(FromFieldKey) ?? false)
                    {
                        context.Response.Buttons = [AiActions.Apply];
                    }
                    else
                    {
                        context.Response.Buttons = [completeSessionAction];
                    }
                    return new(AiRecognitionResult.Confirmation);
                default:
                    context.ValidationResult.AddError(AiConstants.ToolIsNotApplicableAiLocalization);
                    return new(AiRecognitionResult.Error);
            }
        }

        /// <inheritdoc />
        public ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        #endregion
    }
}
