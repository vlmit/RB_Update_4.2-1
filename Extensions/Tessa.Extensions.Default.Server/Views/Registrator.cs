#nullable enable

using Tessa.Extensions.Default.Server.Views.SignatureArchive;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Server.Views
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IViewInterceptor, TaskHistoryInterceptor>(nameof(TaskHistoryInterceptor))
                .RegisterSingleton<IViewInterceptor, TopicParticipantsInterceptor>(nameof(TopicParticipantsInterceptor))
                .RegisterSingleton<IViewInterceptor, CardTasksInterceptor>(nameof(CardTasksInterceptor))
                .RegisterSingleton<IViewInterceptor, FileCategoriesViewInterceptor>(nameof(FileCategoriesViewInterceptor))
                .RegisterSingleton<IViewInterceptor, MyTasksViewInterceptor>(nameof(MyTasksViewInterceptor))
                .RegisterSingleton<IViewInterceptor, DeletedFilesViewInterceptor>(nameof(DeletedFilesViewInterceptor))
                .RegisterSingleton<IViewInterceptor, SignatureArchiveViewInterceptor>(nameof(SignatureArchiveViewInterceptor))
                .RegisterSingleton<IExtraViewListProvider, KrPermissionsViewListProvider>(nameof(KrPermissionsViewListProvider))
            ;
    }
}
