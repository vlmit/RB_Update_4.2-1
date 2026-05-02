using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Web;
using ISession = Tessa.Platform.Runtime.ISession;

namespace Tessa.Extensions.Default.Server.Web.AbTest
{
    /// <summary>
    /// Test controller for demo purposes. Available for administrator sessions only.
    /// </summary>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="slugsGenerator"><inheritdoc cref="ISlugsGenerator" path="/summary"/></param>
    [Route("abtest"), AllowAnonymous, ApiController]
    [ProducesErrorResponseType(typeof(PlainValidationResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public sealed class AbTestController(ISession session, ISlugsGenerator slugsGenerator) : Controller
    {
        #region Fields

        private readonly ISession session = NotNullOrThrow(session);

        private readonly ISlugsGenerator slugsGenerator = NotNullOrThrow(slugsGenerator);

        #endregion

        #region Controller Methods

        /// <summary>
        /// Create test info when using toolbar button "Get table" in a card of type "AbCar".
        /// </summary>
        /// <param name="id">Card identifier.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        // GET service/car-table-request?id=...
        [HttpGet("car-table-request"), SessionMethod(UserAccessLevel.Administrator)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Dictionary<string, object?>>>> GetCarTableRequest(
            [FromQuery] Guid id,
            CancellationToken cancellationToken = default)
        {
            var count = Random.Shared.Next(1, 10);

            var user = this.session.User;
            var result = new List<Dictionary<string, object?>>(count)
            {
                new()
                {
                    ["Guid"] = user.ID,
                    ["String"] = user.Name,
                    ["DateTime"] = DateTime.UtcNow,
                    ["Number"] = Random.Shared.Next(),
                    ["LinkID"] = id,
                    ["LinkName"] = "Current card"
                }
            };

            for (var i = 1; i < count; i++)
            {
                result.Add(new()
                {
                    ["Guid"] = Guid.NewGuid(),
                    ["String"] = await this.slugsGenerator.GenerateSlugsAsync(cancellationToken: cancellationToken),
                    ["DateTime"] = DateTime.UtcNow.AddDays(i),
                    ["Number"] = Random.Shared.Next(),
                    ["LinkID"] = Guid.NewGuid(),
                    ["LinkName"] = await this.slugsGenerator.GenerateSlugsAsync("{n}", cancellationToken: cancellationToken)
                });
            }

            // для того чтобы ответ на запрос содержал информацию по типам данных его свойств, надо вызвать метод-расширение TypedJsonAsync
            return await this.TypedJsonAsync(result, cancellationToken: cancellationToken);
        }

        #endregion
    }
}
