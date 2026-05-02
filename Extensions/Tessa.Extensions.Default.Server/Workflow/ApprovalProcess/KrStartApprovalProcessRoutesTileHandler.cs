#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.Workflow.ApprovalProcess;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Обработчик кнопки вторичного процесса, запускающая процесс согласования.
    /// </summary>
    /// <param name="approvalProcessInstanceRepository"><inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="approvalProcessAccessManager"><inheritdoc cref="IApprovalProcessAccessManager" path="/summary"/></param>
    public class KrStartApprovalProcessRoutesTileHandler(
        IApprovalProcessInstanceRepository approvalProcessInstanceRepository,
        ISession session,
        IApprovalProcessAccessManager approvalProcessAccessManager) : IKrSecondaryProcessTileHandler
    {
        #region Fields

        private readonly IApprovalProcessInstanceRepository approvalProcessInstanceRepository = NotNullOrThrow(approvalProcessInstanceRepository);
        private readonly ISession session = NotNullOrThrow(session);
        private readonly IApprovalProcessAccessManager approvalProcessAccessManager = NotNullOrThrow(approvalProcessAccessManager);

        public static readonly KrSecondaryProcessTileHandlerDescriptor Descriptor = new()
        {
            ID = new Guid(0x6a85b64e, 0x96df, 0x47f8, 0xb4, 0xe0, 0x27, 0x4b, 0xd0, 0xb6, 0xe9, 0x43), // {6A85B64E-96DF-47F8-B4E0-274BD0B6E943}
            Name = "$ApprovalProcess_TileHandlers_StartApprovalProcess",
        };

        #endregion

        #region IKrSecondaryProcessTileHandler Members

        /// <inheritdoc/>
        public async ValueTask<bool> CheckVisibilityAsync(
            IKrProcessButton button,
            Card? card,
            CancellationToken cancellationToken = default)
        {
            if (card is null
                || this.session.Token?.ApplicationID != ApplicationIdentifiers.WebClient
                || !await this.approvalProcessAccessManager.CanCreateInstanceAsync(card, null, cancellationToken))
            {
                return false;
            }

            var processes = await this.approvalProcessInstanceRepository.GetInstancesForCardWithCacheAsync(
                card,
                cancellationToken);

            return processes.Count == 0;
        }

        #endregion
    }
}
