using Tessa.Platform;
using Tessa.Platform.Placeholders;

namespace Tessa.Extensions.Default.Imaging.Placeholders
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void FinalizeRegistration()
        {
            this.UnityContainer
                .TryResolve<IPlaceholderFormatterContainer>()?
                .Register(ImagePlaceholderFormatter.FormatterName, new ImagePlaceholderFormatter())
                .Register(BarcodePlaceholderFormatter.FormatterName, new BarcodePlaceholderFormatter())
                .Register(QRCodePlaceholderFormatter.FormatterName, new QRCodePlaceholderFormatter())
                ;
        }
    }
}
