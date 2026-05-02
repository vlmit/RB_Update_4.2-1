#nullable enable

using Tessa.Ai.Agent;
using Tessa.Ai.Files;
using Tessa.Ai.Plugins.CardTool;
using Tessa.Extensions.Default.Server.Ai.Plugins;
using Tessa.Extensions.Default.Server.Ai.Plugins.CardTool;
using Unity;

namespace Tessa.Extensions.Default.Server.Ai
{
    /// <inheritdoc />
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        /// <inheritdoc />
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IAiFileAccessValidator, DefaultAiFileAccessValidator>()
                .RegisterSingleton<IAiAgentPlugin, AddDeputyAiAgentPlugin>(nameof(AddDeputyAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, ContractInfoAiAgentPlugin>(nameof(ContractInfoAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, CreateIncomingAiAgentPlugin>(nameof(CreateIncomingAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, DeputyInfoAiAgentPlugin>(nameof(DeputyInfoAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, FileDiscussionAiAgentPlugin>(nameof(FileDiscussionAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, FreeCommunicationAiAgentPlugin>(nameof(FreeCommunicationAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, OutgoingWriterAiAgentPlugin>(nameof(OutgoingWriterAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, PartnerCreateCardAiAgentPlugin>(nameof(PartnerCreateCardAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, TextEnhancementAiAgentPlugin>(nameof(TextEnhancementAiAgentPlugin))
                .RegisterSingleton<IAiAgentPlugin, UserCreateCardAiAgentPlugin>(nameof(UserCreateCardAiAgentPlugin))
                .RegisterSingleton<ICardToolPermissionsProvider, KrCardToolPermissionsProvider>()
                ;
    }
}
