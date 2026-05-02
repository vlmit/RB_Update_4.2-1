#nullable enable
using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Platform.Server.Initialization;
using Tessa.FileConverters;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Files.VirtualFiles
{
    /// <summary>
    /// Расширение инвалидирует кэш виртуальных файлов, если изменилась настройка сервера "Отключить конвертацию HTML в PDF",
    /// а также перерегистрирует поддерживаемые конвертером расширения.
    /// </summary>
    public sealed class VirtualFilesServerInstanceStoreExtension :
        CardStoreExtension
    {
        #region Constructors

        public VirtualFilesServerInstanceStoreExtension(
            IKrVirtualFileCache virtualFileCache)
        {
            this.virtualFileCache = NotNullOrThrow(virtualFileCache);
        }

        #endregion

        #region Fields

        private readonly IKrVirtualFileCache virtualFileCache;

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task AfterRequest(ICardStoreExtensionContext context)
        {
            if (!context.RequestIsSuccessful || context.Request.TryGetCard() is not { } card)
            {
                return;
            }

            // Если изменялась настройка сервера "Отключить конвертацию HTML в PDF", необходимо сбросить кэш виртуальных файлов
            // и перерегистрировать поддерживаемые конвертером расширения.
            var conversionDisabled =
                card.Sections.TryGet(ServerInitializationHelper.ServerInstancesSectionName)?
                    .RawFields.TryGet<bool?>(ServerInitializationHelper.DisableHtmlToPdfConversionKey);

            if (conversionDisabled.HasValue)
            {
                await this.virtualFileCache.InvalidateAsync();

                FileConverterFormat.RegisterExtensions(
                    FileConverterFormat.Pdf,
                    ServerInitializationHelper.PdfExtension,
                    conversionDisabled.Value
                        ? FileConverterFormat.InputExtensionsWithoutHtmlSupport
                        : FileConverterFormat.DefaultInputExtensions);
            }
        }

        #endregion
    }
}
