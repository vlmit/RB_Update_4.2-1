import { extension } from '@tessa/application';
import Platform from 'common/platform';
import { PdfPreviewerViewModel } from 'tessa/ui/preview/pdf/pdfPreviewerViewModel';
import { FilePreviewExtension, IFilePreviewExtensionContext } from 'tessa/ui/preview';

/**
 * Расширение для карточек.
 *
 * Если предпросмотр карточки открывается в мобильной версии,
 * то принудительно включаем режим бесконечного скроллинга.
 */
@extension({ name: 'CardMobilePreviewUIExtension' })
export class CardMobilePreviewUIExtension extends FilePreviewExtension {
  //#region FilePreviewExtension

  override async initializing(context: IFilePreviewExtensionContext): Promise<void> {
    if (!Platform.isMobile()) {
      return;
    }

    const { previewerViewModel } = context;

    if (previewerViewModel instanceof PdfPreviewerViewModel) {
      previewerViewModel.infiniteMode = true;
    }
  }

  //#endregion
}
