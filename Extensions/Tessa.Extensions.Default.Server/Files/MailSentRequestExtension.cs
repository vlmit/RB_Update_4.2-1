using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Notices;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Files
{
    /// <summary>
    /// Расширение можно вызвать только с сервера (в т.ч. из серверных плагинов Chronos).
    /// </summary>
    public sealed class MailSentRequestExtension :
        CardRequestExtension
    {
        public override Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || context.Request.ServiceType != CardServiceType.Default
                || context.Request.TryGetInfo() is not {} info
                || info.TryGet<Dictionary<string, object>>("MailInfo") is not {} mainInfoStorage)
            {
                return Task.CompletedTask;
            }

            var mailInfo = new MailInfo(mainInfoStorage);

            ListStorage<MailFile> files = mailInfo.TryGetFiles();
            if (files is not { Count: > 0 })
            {
                return Task.CompletedTask;
            }

            foreach (MailFile file in files)
            {
                string filePath = file.Info.TryGet<string>("ServerFilePath");

                if (!string.IsNullOrEmpty(filePath)
                    && file.Info.TryGet<bool>("RemoveFile")
                    && File.Exists(filePath))
                {
                    bool removeFolder = file.Info.TryGet<bool>("RemoveFolder");
                    FileHelper.ReleaseFilePath(filePath, keepFolder: !removeFolder);
                }
            }

            return Task.CompletedTask;
        }
    }
}
