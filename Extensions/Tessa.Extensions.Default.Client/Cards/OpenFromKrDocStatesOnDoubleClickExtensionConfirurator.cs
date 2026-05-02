#nullable enable
using Tessa.Extensions.Platform.Client.Cards;
using Tessa.UI.Cards;

namespace Tessa.Extensions.Default.Client.Cards
{
    /// <summary>
    /// Конфигуратор расширения <see cref="OpenFromKrDocStatesOnDoubleClickExtension"/>.
    /// </summary>
    public sealed class OpenFromKrDocStatesOnDoubleClickExtensionConfigurator(CreateDialogFormFuncAsync createDialogFormFunc)
        : OpenInDialogOnDoubleClickExtensionConfiguratorBase(createDialogFormFunc, null, "$OpenFromKrDocStatesOnDoubleClickExtension_Description");
}
