using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    public sealed class KrDocTypeInvalidateSettingsCacheDeleteExtension : CardDeleteExtension
    {
        private readonly IKrTypesCache cache;

        public KrDocTypeInvalidateSettingsCacheDeleteExtension(IKrTypesCache cache)
        {
            this.cache = cache;
        }

        public override Task AfterRequestFinally(ICardDeleteExtensionContext context)
        {
            if (!context.RequestIsSuccessful || !context.ValidationResult.IsSuccessful())
            {
                return Task.CompletedTask;
            }

            return this.cache.InvalidateAsync();
        }
    }
}
