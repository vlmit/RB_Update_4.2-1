using System.Threading.Tasks;
using Tessa.Extensions.Platform.Shared.Initialization;
using Tessa.Platform;
using Tessa.Platform.Initialization;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    public class SignatureArchiveInitializationExtension(
        ISignatureArchivePermissionProvider signatureArchivePermissionProvider) 
        : ServerInitializationExtension
    {
        #region Fields

        private readonly ISignatureArchivePermissionProvider signatureArchivePermissionProvider = NotNullOrThrow(signatureArchivePermissionProvider);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(IServerInitializationExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return;
            }

            if (await this.signatureArchivePermissionProvider.IsAdministratorAsync(context.CancellationToken))
            {
                context.Response!.Info[InitializationExtensionHelper.SignatureArchiveAdministrator] = BooleanBoxes.True;
            }
        }

        #endregion

    }
}
