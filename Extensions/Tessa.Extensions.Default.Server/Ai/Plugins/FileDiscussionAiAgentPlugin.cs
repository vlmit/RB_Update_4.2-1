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
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента по свободному общению по файлу.
    /// </summary>
    public class FileDiscussionAiAgentPlugin : IAiAgentPlugin
    {
        #region Private fields and constants

        private const string ToolPrompt =
            """
            Ты умный помощник, который умеет анализировать файл и отвечать на вопросы по нему.
            При формировании ответа на поставленный вопрос пользователя не делай догадок, не придумывай, отвечай только ту информацию, которая есть в тексте.
            Если в тексте невозможно найти ответ на вопрос пользователя, то напиши, что данных в тексте недостаточно.
            """;

        private static readonly AiToolSettings toolInfo = new()
        {
            ID = "file_discussion",
            Name = "Вопросы по файлу",
            Description = "Вопрос по содержанию текста документа.",

            PluginName = nameof(FileDiscussionAiAgentPlugin),
            Prompts = AiPromptsHelper.GetPrompts(AiPromptTemplates.AiTool.Templates.Append(
                new(AiPromptTemplates.AiTool.CustomPromptName, ToolPrompt))),

            RequireFile = true,
            Infinite = true,
        };

        private AiCachedPrompts? cachedPrompts;

        #endregion

        #region IAiAgentPlugin Implementation

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            new([toolInfo]);

        /// <inheritdoc />
        public ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FileDiscussionAiAgentPlugin>(toolId, toolInfo.ID);
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
                // Если есть вложение хотя бы в одном сообщении - инструмент применим.
                // Чтобы применять инструмент и без файлов, нужно перенести это условие в GetInstructionsAsync
                return request.Messages is { Count: > 0 } messages
                    && messages.Any(static m => m.Content is not null 
                        && m.Content.Any(static c => c.Type == AiMessagePartType.File))
                        ? new (new AiToolApplicability { Available = true, Visible = true })
                        : new (new AiToolApplicability());
            }

            return new (new AiToolApplicability { Available = true });
        }

        /// <inheritdoc />
        public async ValueTask<AiInstructionsResult> GetInstructionsAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FileDiscussionAiAgentPlugin>(toolId, toolInfo.ID);

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, null, cancellationToken);
            this.cachedPrompts = actual;
            
            var prompt = new AiPromptBuilder()
                .WithTemplates(actual.Templates)
                .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                .WithName(AiPromptTemplates.AiTool.AutoPromptName, string.Empty) // выключаем автопромпт
                .WithCurrentDateTime(DateTime.UtcNow + context.Session.ClientUtcOffset, false, context.Session.ClientUICulture)
                .Build();

            return new(prompt);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FileDiscussionAiAgentPlugin>(toolId, toolInfo.ID);
            if (string.IsNullOrWhiteSpace(data))
            {
                context.ValidationResult.AddError("$Ai_Plugins_Validation_NoResponse");
                return AiRecognitionResult.Error;
            }

            // Передаем чистый ответ от ИИ без какой-либо обработки
            context.Response.Message = data;

            if (context.Request.Context?.Type is null or AiContextType.None)
            {
                // Кнопка "Завершить работу с файлами" это фактически кнопка отклонения, то есть завершения сессии.
                // Используем стандартную кнопку с измененным заголовком.
                context.Response.Buttons = [new AiAction(AiActions.RejectID, "$Ai_FileDiscussionPlugin_CompleteSession", true)];
            }

            return AiRecognitionResult.Clarification;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken) =>
            // Данный инструмент явным образом свою работу не прерывает.
            // Поскольку это свободное общение, здесь нет обработки результата от ИИ.
            throw new NotImplementedException();

        #endregion
    }
}
