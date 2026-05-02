#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Events
{
    public static class KrEventExtensions
    {
        #region Public Methods

        public static IExtensionContainer RegisterKrEventExtensionTypes(
            this IExtensionContainer extensionContainer) =>
            NotNullOrThrow(extensionContainer)
                .RegisterType<IKrEventExtension>(x => x
                        .MethodAsync<IKrEventExtensionContext>(y => y.HandleEvent),
                    x => x.Register(KrEventFilterPolicy.Instance));

        public static IExtensionPolicyContainer WhenEventType(
            this IExtensionPolicyContainer policyContainer,
            params string[] eventTypes) =>
            NotNullOrThrow(policyContainer)
                .Register(new KrEventPolicy(eventTypes));

        public static IExtensionPolicyContainer WhenEventType(
            this IExtensionPolicyContainer policyContainer,
            IReadOnlyCollection<string> eventTypes) =>
            NotNullOrThrow(policyContainer)
                .Register(new KrEventPolicy(eventTypes));

        public static Task RaiseAsync(
            this IKrEventManager manager,
            string eventType,
            Stage? currentStage,
            KrProcessRunnerMode runnerMode,
            IKrProcessRunnerContext runnerContext,
            IDictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(manager);
            ThrowIfNull(runnerContext);

            var context = new KrEventExtensionContext(
                eventType,
                info,
                runnerContext.CardID,
                runnerContext.CardType,
                runnerContext.DocTypeID,
                runnerContext.MainCardAccessStrategy,
                runnerContext.SecondaryProcess,
                runnerContext.ContextualSatellite,
                runnerContext.ProcessHolderSatellite,
                runnerContext.CardContext,
                runnerContext.ValidationResult,
                currentStage,
                runnerContext.WorkflowProcess,
                runnerContext.ProcessHolder,
                runnerContext.ProcessInfo,
                runnerMode,
                runnerContext.InitiationCause,
                cancellationToken);
            return manager.RaiseAsync(context);
        }

        public static Task RaiseAsync(
            this IKrEventManager manager,
            string eventType,
            IStageTypeHandlerContext handlerContext,
            IDictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(manager);
            ThrowIfNull(handlerContext);

            var context = new KrEventExtensionContext(
                eventType,
                info,
                handlerContext.MainCardID,
                handlerContext.MainCardType,
                handlerContext.MainCardDocTypeID,
                handlerContext.MainCardAccessStrategy,
                handlerContext.SecondaryProcess,
                handlerContext.ContextualSatellite,
                handlerContext.ProcessHolderSatellite,
                handlerContext.CardExtensionContext,
                handlerContext.ValidationResult,
                handlerContext.Stage,
                handlerContext.WorkflowProcess,
                handlerContext.ProcessHolder,
                handlerContext.ProcessInfo,
                handlerContext.RunnerMode,
                handlerContext.InitiationCause,
                cancellationToken);
            return manager.RaiseAsync(context);
        }

        public static Task RaiseAsync(
            this IKrEventManager manager,
            string eventType,
            IGlobalSignalHandlerContext stateHandler,
            IDictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(manager);
            ThrowIfNull(stateHandler);

            var runnerContext = stateHandler.RunnerContext;
            var context = new KrEventExtensionContext(
                eventType,
                info,
                runnerContext.CardID,
                runnerContext.CardType,
                runnerContext.DocTypeID,
                runnerContext.MainCardAccessStrategy,
                runnerContext.SecondaryProcess,
                runnerContext.ContextualSatellite,
                runnerContext.ProcessHolderSatellite,
                runnerContext.CardContext,
                runnerContext.ValidationResult,
                stateHandler.Stage,
                runnerContext.WorkflowProcess,
                runnerContext.ProcessHolder,
                runnerContext.ProcessInfo,
                stateHandler.RunnerMode,
                runnerContext.InitiationCause,
                cancellationToken);
            return manager.RaiseAsync(context);
        }

        public static Task RaiseAsync(
            this IKrEventManager manager,
            string eventType,
            IStateHandlerContext stateHandler,
            IDictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(manager);
            ThrowIfNull(stateHandler);

            var runnerContext = stateHandler.RunnerContext;
            var context = new KrEventExtensionContext(
                eventType,
                info,
                runnerContext.CardID,
                runnerContext.CardType,
                runnerContext.DocTypeID,
                runnerContext.MainCardAccessStrategy,
                runnerContext.SecondaryProcess,
                runnerContext.ContextualSatellite,
                runnerContext.ProcessHolderSatellite,
                runnerContext.CardContext,
                runnerContext.ValidationResult,
                stateHandler.Stage,
                runnerContext.WorkflowProcess,
                runnerContext.ProcessHolder,
                runnerContext.ProcessInfo,
                stateHandler.RunnerMode,
                runnerContext.InitiationCause,
                cancellationToken);
            return manager.RaiseAsync(context);
        }

        public static Task RaiseAsync(
            this IKrEventManager manager,
            string eventType,
            Guid cardID,
            CardType cardType,
            Guid? docTypeID,
            IMainCardAccessStrategy cardAccessStrategy,
            ICardExtensionContext extensionContext,
            IValidationResultBuilder validationResult,
            IDictionary<string, object?>? info = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(manager);

            var context = new KrEventExtensionContext(
                eventType: eventType,
                info: info,
                mainCardID: cardID,
                mainCardType: cardType,
                mainCardDocTypeID: docTypeID,
                mainCardAccessStrategy: cardAccessStrategy,
                secondaryProcess: null,
                contextualSatellite: null,
                processHolderSatellite: null,
                cardExtensionContext: extensionContext,
                validationResult: validationResult,
                stage: null,
                workflowProcess: null,
                processHolder: null,
                processInfo: null,
                runnerMode: null,
                initiationCause: null,
                cancellationToken: cancellationToken);
            return manager.RaiseAsync(context);
        }

        #endregion
    }
}
