#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Shared.Settings
{
    public sealed class KrSettingsLazy(Func<CancellationToken, ValueTask<KrSettings>> getSettingsFuncAsync)
    {
        #region Fields

        private readonly Func<CancellationToken, ValueTask<KrSettings>> getSettingsFuncAsync = NotNullOrThrow(getSettingsFuncAsync);

        private KrSettings? value;

        #endregion

        #region Properties

        public async ValueTask<KrSettings> GetValueAsync(CancellationToken cancellationToken = default) =>
            this.value ??= await this.getSettingsFuncAsync(cancellationToken).ConfigureAwait(false);

        #endregion
    }
}
