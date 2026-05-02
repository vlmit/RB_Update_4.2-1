import { extension } from '@tessa/application';
import { getNameAndExtForFile } from 'common';
import { MenuAction, showNotEmpty, UIContext } from 'tessa/ui';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import { AsyncLazy, ValidationResult } from '@tessa/core';
import { FileVersionState } from 'tessa/files';
import {
  PdfAnnotationDialogViewModelFactory,
  PdfAnnotationDialogViewModelFactory$,
  PdfAnnotationsHelper,
  EditorMode,
  IPdfAnnotationDialogViewModel
} from 'tessa/pdf-annotations';
import Stamp from './../images/stamp.png';

@extension({ name: 'PdfAnnotationsFileExtension' })
export class PdfAnnotationsFileExtension extends FileExtension {
  constructor(
    @PdfAnnotationDialogViewModelFactory$({ lazy: true })
    private readonly dialogFactory: AsyncLazy<PdfAnnotationDialogViewModelFactory>
  ) {
    super();
  }

  public openingMenu(context: IFileExtensionContext): void {
    if (
      'pdf' !== getNameAndExtForFile(context.file.model?.lastVersion.name).ext?.toLocaleLowerCase()
    ) {
      return;
    }

    if (context.files.length > 1) {
      return;
    }

    const file = context.file.model;
    if (
      file.lastVersion.state !== FileVersionState.Success ||
      file.lastVersion === file.versionAdded
    ) {
      return;
    }
    const annsExisted = !!file.info[PdfAnnotationsHelper.PdfAnnotationsKey];

    const sepIndex = context.actions.findIndex(x => x.name === 'Separator');
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
        await file.lastVersion.ensureContentDownloaded();

        const factory = await this.dialogFactory.getValue();
        let viewModel = await factory({
          file,
          version: file.lastVersion,
          editor,
          onBeforeSave: async ({ annotations }) => {
            return { annotations, cancel: false };
          },
          editorMode,
          fileName: file.name
        });

        viewModel = vmTransform?.(viewModel) || viewModel;

        viewModel.stamps = [
          {
            name: 'tessa.png',
            url: Stamp
          }
        ];

        await viewModel.showDialog();
      };

    context.actions.splice(
      sepIndex + 1,
      0,
      new MenuAction(
        'EditAnnotations',
        '$UI_FacsimileAndAnnotationsGroup_EditAnnotations',
        'ta icon-thin-120',
        handler({
          editorMode: file.permissions.canEdit
            ? EditorMode.EditAnnotations
            : EditorMode.ViewAnnotations
        }),
        null,
        false
      ),
      new MenuAction(
        'Fascimile',
        '$UI_FacsimileAndAnnotationsGroup_Fascimile',
        'ta icon-thin-119',
        handler({ editorMode: EditorMode.ImageAdding }),
        null,
        !file.permissions.canEdit
      ),
      new MenuAction(
        'DeleteAnnotations',
        '$UI_FacsimileAndAnnotationsGroup_DeleteAnnotations',
        'ta icon-thin-119',
        async () => {
          const { deleteAnnotationsWithConfirmation } = await import('tessa/pdf-annotations/chunk');
          const deletionIsApproved = await deleteAnnotationsWithConfirmation(file);
          if (!deletionIsApproved) {
            return;
          }

          const resultClosure: { validationResult?: ValidationResult } = {};
          await editor.saveCard(editor.context, undefined, undefined, resultClosure);
          if (resultClosure.validationResult && !resultClosure.validationResult.isSuccessful) {
            await showNotEmpty(resultClosure.validationResult);
            return;
          }
        },
        null,
        !file.permissions.canEdit || !annsExisted
      )
    );
  }
}
