#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Tessa.Roles.Deputies;

namespace Tessa.Test.Default.Shared.Roles
{
    /// <summary>
    /// Объект, предоставляющий настройки замещения <see cref="IDeputiesManagementSettings"/>, используемые в тестах.
    /// </summary>
    public sealed class TestDeputiesManagementSettingsProvider(IDeputiesManagementSettings? settings = null) : IDeputiesManagementSettingsProvider
    {
        #region Constants And Static Fields

        private static readonly IDeputiesManagementSettings defaultSettings = new DeputiesManagementSettings
        {
            UseDeputyRoleSeparation = true,
            UseRoleDeputies = true,
        };

        private readonly IDeputiesManagementSettings settings = settings ?? defaultSettings;

        #endregion

        #region IDeputiesManagementSettingsProvider Members

        /// <inheritdoc/>
        public ValueTask<IDeputiesManagementSettings> GetSettingsAsync(CancellationToken cancellationToken = default) => new(this.settings);

        /// <inheritdoc/>
        public ValueTask ResetSettingsAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        #endregion
    }
}
