import { FileControlExtension, IFileControlExtensionContext } from 'tessa/ui/files';
import { FileTagViewModel } from 'tessa/ui/cards/controls';
import { extension, localize } from '@tessa/application';
import { PdfAnnotationsHelper } from 'tessa/pdf-annotations';

/**
 *  Add icons and an additional info for pdf annotations files.
 */
@extension({ name: 'PdfAnnotationsFileControlExtension' })
export class PdfAnnotationsFileControlExtension extends FileControlExtension {
  public initializing(context: IFileControlExtensionContext): void {
    const control = context.control;
    const container = control.fileContainer;

    for (const file of control.files) {
      const cardFile = container.files.find(x => x.id === file.id);
      const marker = cardFile?.info[PdfAnnotationsHelper.PdfAnnotationsKey];
      if (marker) {
        file.tag = new FileTagViewModel('ta m-annotation');
        file.additionalInfoDelegate.set(() =>
          localize('$UI_FacsimileAndAnnotationsGroup_AnnotationsFile')
        );
      }
    }
  }
}
