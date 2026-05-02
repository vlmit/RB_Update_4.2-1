import { AsyncLazy } from '@tessa/core';
import { extension } from '@tessa/application';
import { getNameAndExtForFile } from 'common';
import { MenuAction, UIContext } from 'tessa/ui';
import { FileVersionExtension, IFileVersionExtensionContext } from 'tessa/ui/files';
import { FileVersionState } from 'tessa/files';
import {
  PdfAnnotationDialogViewModelFactory,
  PdfAnnotationDialogViewModelFactory$,
  EditorMode,
  IPdfAnnotationDialogViewModel
} from 'tessa/pdf-annotations';

@extension({ name: 'PdfAnnotationsFileVersionExtension' })
export class PdfAnnotationsFileVersionExtension extends FileVersionExtension {
  constructor(
    @PdfAnnotationDialogViewModelFactory$({ lazy: true })
    private readonly dialogFactory: AsyncLazy<PdfAnnotationDialogViewModelFactory>
  ) {
    super();
  }

  public openingMenu(context: IFileVersionExtensionContext): void {
    if ('pdf' !== getNameAndExtForFile(context.version.name).ext?.toLocaleLowerCase()) {
      return;
    }

    const version = context.version;
    if (version.state !== FileVersionState.Success || version === context.file.model.versionAdded) {
      return;
    }

    const sepIndex = context.actions.findIndex(x => x.name === 'Separator');
    const file = context.file.model;
    const editor = UIContext.current.cardEditor!;

    const handler =
      ({
        editorMode,
        vmTransform
      }: {
        editorMode: EditorMode;
        onInit?: (vm: IPdfAnnotationDialogViewModel) => void;
        vmTransform?: (vm: IPdfAnnotationDialogViewModel) => IPdfAnnotationDialogViewModel;
      }) =>
      async () => {
        await version.ensureContentDownloaded();

        const factory = await this.dialogFactory.getValue();
        let viewModel = await factory({
          file,
          version,
          editor,
          onBeforeSave: async ({ annotations }) => {
            return { annotations, cancel: false };
          },
          editorMode
        });

        viewModel = vmTransform?.(viewModel) || viewModel;

        viewModel.stamps = [];

        await viewModel.showDialog();
      };

    context.actions.splice(
      sepIndex + 1,
      0,
      new MenuAction(
        'FacsimileAndAnnotationsMenuGroup',
        '$UI_FacsimileAndAnnotationsGroup',
        'ta icon-thin-439',
        null,
        [
          new MenuAction(
            'EditAnnotations',
            '$UI_FacsimileAndAnnotationsGroup_EditAnnotations',
            'ta icon-thin-120',
            handler({
              editorMode: EditorMode.ViewAnnotations
            }),
            null,
            false
          )
        ],
        false
      )
    );
  }
}
