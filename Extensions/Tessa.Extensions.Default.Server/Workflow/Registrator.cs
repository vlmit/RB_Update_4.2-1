#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.ObjectLocking;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow
{
    [Registrator]
    public class Registrator :
        RegistratorBase
    {
        #region Base Overrides

        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<AddPerformerToHistoryCardStoreTaskExtension>()
                .RegisterSingleton<IKrHistoryStrategy, KrHistoryStrategy>()
                .RegisterSingleton<IKrGetSqlPerformersStrategy, KrGetSqlPerformersStrategy>()
                .RegisterFactory<IObjectTransactionLockingStrategy>(
                    nameof(ReadersWritersObjectLockingStrategy),
                    static c => new ObjectTransactionLockingStrategy(
                        c.Resolve<IReadersWritersObjectLockingStrategy>()),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IReadersWritersObjectLockingStrategy, ReadersWritersObjectLockingStrategy>(new ContainerControlledLifetimeManager());

        /// <inheritdoc/>
        public override void RegisterExtensions(IExtensionContainer extensionContainer) =>
            extensionContainer
                .RegisterExtension<ICardStoreTaskExtension, KrClearWasteInAdditionalApprovalStoreTaskExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 998)
                    .WithSingleton()
                    .WhenTaskTypes(DefaultTaskTypes.KrAdditionalApprovalTypeID))
                .RegisterExtension<ICardStoreTaskExtension, KrClearWasteInApprovalStoreTaskExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 999)
                    .WithSingleton()
                    .WhenTaskTypes(DefaultTaskTypes.KrApproveTypeID, DefaultTaskTypes.KrSigningTypeID, DefaultTaskTypes.KrAdditionalApprovalTypeID))
                .RegisterExtension<ICardStoreTaskExtension, AddPerformerToHistoryCardStoreTaskExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer))
                ;

        #endregion
    }
}
