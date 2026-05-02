using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tessa.Platform.Runtime;
using Tessa.Web;
using Tessa.Web.Client.Services;

namespace Tessa.Extensions.Default.Server.Web.Controllers
{
    [AllowAnonymous, ApiController, ApiExplorerSettings(IgnoreApi = true)]
    public sealed class PlaygroundController(IClientViewProvider viewProvider) :
        Controller
    {
        #region Fields

        private readonly IClientViewProvider viewProvider = NotNullOrThrow(viewProvider);

        #endregion

        #region Route Methods

        [Route("playground"), SessionMethod(UserAccessLevel.Administrator)]
        public Task<IActionResult> GetIndexViewAsync(CancellationToken cancellationToken = default) =>
            this.viewProvider.GetIndexViewAsync(this, cancellationToken: cancellationToken);

        #endregion
    }
}
