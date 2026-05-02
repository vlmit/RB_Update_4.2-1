using Microsoft.Extensions.DependencyInjection;
using Tessa.Web.Client.Services;
using Tessa.Web.Registrations;

namespace Tessa.Extensions.Default.Server.Web.Services
{
    [WebRegistrator]
    public sealed class WebRegistrator : WebRegistratorBase
    {
        public override void RegisterServices() =>
            this.Services.Configure<WebClientPathOptions>(options =>
            {
                options.RedirectableToLoginPrefixes.Add("playground");

                // PlaygroundController не только возвращает индексную страницу, но и проверяет, что сессия от администратора;
                // поэтому здесь не добавляем префикс playground для открытия SPA-приложения, отдавая это на откуп контроллеру PlaygroundController
                // options.IndexViewPrefixes.Add("playground");
            });
    }
}
