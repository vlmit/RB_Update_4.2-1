using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tessa.Extensions.Default.Server.OnlyOffice.Token;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Web;
using Tessa.Web.Services;

namespace Tessa.Extensions.Default.Server.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class OnlyOfficeJwtAuthorizationAttribute :
        Attribute,
        IAsyncActionFilter
    {
        #region IAsyncActionFilter Members

        /// <inheritdoc/>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var token = httpContext.Request.Headers.TryGetAuthorizationHeaderValue()?.Trim();

            if (string.IsNullOrEmpty(token)
                && context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
                token = context.TryGetStringParameterWith<OnlyOfficeJwtTokenAttribute>(actionDescriptor)?.Trim();
            }

            var authorize = false;
            var verifyError = string.Empty;
            if (string.IsNullOrEmpty(token))
            {
                verifyError = "token is null or empty.";
            }
            else
            {
                var unityHolder = context.HttpContext.RequestServices.GetService<IWebUnityHolder>();
                var container = unityHolder?.ContainerIsAvailable == true ? unityHolder.Container : null;

                var r7TokenManager = container?.TryResolve<IOnlyOfficeR7TokenManager>();

                if (r7TokenManager is not null)
                {
                    await using var _ = SessionContext.Create(Session.CreateSystemToken(container?.TryResolve<ITessaServerSettings>()));
                    if (await r7TokenManager.VerifyTokenAsync(token) is { } onlyOfficeJwtToken)
                    {
                        var checkTessaToken = true;
                        if (onlyOfficeJwtToken.OnlyOfficeUrl is not null)
                        {
                            Uri uri = new Uri(onlyOfficeJwtToken.OnlyOfficeUrl);

                            var ooToken = HttpUtility.ParseQueryString(uri.Query).Get("token");
                            if (ooToken is not null)
                            {
                                checkTessaToken = false;
                                token = ooToken;
                            }
                        }

                        if (checkTessaToken)
                        {
                            if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor2)
                            {
                                token = context.TryGetStringParameterWith<OnlyOfficeJwtTokenAttribute>(actionDescriptor2)?.Trim();
                            }
                        }
                    }

                    if (token is not null
                        && container?.TryResolve<IOnlyOfficeTokenManager>() is { } tokenManager
                        && await tokenManager.VerifyTokenAsync(token) is { } jwtToken)
                    {
                        httpContext.SetJwtToken(jwtToken);
                        authorize = true;
                    }
                }
            }

            if (!authorize)
            {
                throw new UnauthorizedAccessException($"Error check authorization by JWT Bearer token: {verifyError}");
            }

            await next();
        }

        #endregion
    }
}
