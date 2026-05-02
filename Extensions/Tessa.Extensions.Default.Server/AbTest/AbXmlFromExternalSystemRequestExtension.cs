#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.AbTest
{
    /// <summary>
    /// Расширение на сохранение фейковой карточки, которое возвращает нужные нам данные.
    /// Это расширение на сохранение, а не на загрузку, т.к. расширение на загрузку обязано вернуть корректный пакет карточки.
    /// </summary>
    public sealed class AbXmlFromExternalSystemRequestExtension :
        CardRequestExtension
    {
        public override Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return Task.CompletedTask;
            }

            if (!context.Session.User.IsAdministrator())
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .Error(ValidationKeys.UserIsNotAdmin)
                    .End();

                return Task.CompletedTask;
            }

            var name = context.Request.Info.Get<string>("Name");
            var driver = context.Request.Info.Get<string>("Driver");
            var files = context.Request.Info.Get<List<object?>>("Files");

            var xmlText = AbXmlExternalSystemProvider.GetXml(name, driver, files);
            context.Response!.Info["Xml"] = xmlText;

            return Task.CompletedTask;
        }
    }
}
