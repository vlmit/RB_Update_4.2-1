#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для свободного общения.
    /// </summary>
    public class FreeCommunicationAiAgentPlugin : IAiAgentPlugin
    {
        #region Static Fields

        private static readonly AiToolSettings baseSettings = new()
        {
            ID = "free_communication",
            Name = "Свободное общение",
            Description = "Прямое общение с моделью",
            Hint = "Позволяет общаться с моделью на любые темы.",
            PluginName = nameof(FreeCommunicationAiAgentPlugin),
            Infinite = true
        };

        #endregion

        /// <inheritdoc />
        public ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            new([baseSettings]);

        /// <inheritdoc />
        public ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FreeCommunicationAiAgentPlugin>(toolId, baseSettings.ID);
            return new(baseSettings);
        }

        /// <inheritdoc />
        public ValueTask<AiToolApplicability> IsApplicableAsync(string toolId, AiRequest? request, CancellationToken cancellationToken)
        {
            if (toolId != baseSettings.ID)
            {
                return new(new AiToolApplicability());
            }

            if (request is not null)
            {
                return
                    request.Context?.Type is null or AiContextType.None
                        ? new (new AiToolApplicability { Available = true })
                        : new (new AiToolApplicability());
            }

            return new (new AiToolApplicability { Available = true });
        }

        /// <inheritdoc />
        public ValueTask<AiInstructionsResult> GetInstructionsAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FreeCommunicationAiAgentPlugin>(toolId, baseSettings.ID);
            // У данного инструмента нет ни схемы, ни промпта.
            return new (new AiInstructionsResult(AiInstructionResultCode.Success));
        }

        /// <inheritdoc />
        public ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context, CancellationToken cancellationToken)
        {
            AiHelper.ThrowIfInvalidTool<FreeCommunicationAiAgentPlugin>(toolId, baseSettings.ID);
            context.Response.Message = data;
            return new (AiRecognitionResult.Clarification);
        }

        /// <inheritdoc />
        public ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken) =>
            // Данный инструмент явным образом свою работу не прерывает.
            // Поскольку это свободное общение, здесь нет обработки результата от ИИ.
            throw new NotImplementedException();
    }
}
