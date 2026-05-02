import { AsyncLazy } from '@tessa/core';
import { extension } from '@tessa/application';
import { getNameAndExtForFile } from 'common';
import { MenuAction, UIContext } from 'tessa/ui';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import {
  AnnotationClassType,
  AnnotationState,
  AnnotationType,
  EditorMode,
  ImageAnnotation,
  TextboxAnnotation,
  PdfAnnotationDialogViewModelFactory,
  PdfAnnotationDialogViewModelFactory$
} from 'tessa/pdf-annotations';

@extension()
export class PdfAnnsCarFileExtension extends FileExtension {
  constructor(
    @PdfAnnotationDialogViewModelFactory$({ lazy: true })
    private readonly dialogFactory: AsyncLazy<PdfAnnotationDialogViewModelFactory>
  ) {
    super();
  }

  public shouldExecute(): boolean {
    return true;
  }

  public openingMenu(context: IFileExtensionContext): void {
    if (
      'pdf' !== getNameAndExtForFile(context.file.model?.lastVersion.name).ext?.toLocaleLowerCase()
    ) {
      return;
    }

    const file = context.file.model;
    const version = file.lastVersion;
    const editor = UIContext.current.cardEditor!;

    context.actions.splice(
      context.actions.length,
      0,
      new MenuAction(
        'ExamplePdfAnns',
        'ExamplePdfAnns',
        'ta icon-thin-119',
        async () => {
          await version.ensureContentDownloaded();

          const factory = await this.dialogFactory.getValue();
          const viewModel = await factory({
            file,
            version,
            editor,
            onBeforeSave: async ({ annotations }) => {
              return { annotations, cancel: false };
            },
            editorMode: EditorMode.EditAnnotations,
            fileName: file.name
          });

          viewModel.stamps = [];

          await viewModel.showDialog();
        },
        [
          new MenuAction(
            'EPA_showEditor',
            'EPA_showEditor',
            'ta icon-thin-119',
            async () => {
              await version.ensureContentDownloaded();

              const factory = await this.dialogFactory.getValue();
              const viewModel = await factory({
                file,
                version,
                editor,
                onBeforeSave: async ({ annotations }) => {
                  // You can modify actions before a save or cancel one
                  return { annotations, cancel: false };
                },
                editorMode: EditorMode.EditAnnotations,
                fileName: file.name
              });

              viewModel.stamps = [];

              await viewModel.showDialog();
            },
            null,
            false
          ),
          new MenuAction(
            'EPA_addAnns',
            'EPA_addAnns',
            'ta icon-thin-119',
            async () => {
              const { getRandomPdfRef, getBytesFromDataUrl } =
                await import('tessa/pdf-annotations/chunk');
              await version.ensureContentDownloaded();

              const factory = await this.dialogFactory.getValue();
              const viewModel = await factory({
                file,
                version,
                editor,
                onBeforeSave: async ({ annotations }) => {
                  // You can modify actions before a save or cancel one
                  return { annotations, cancel: false };
                },
                editorMode: EditorMode.EditAndImage,
                fileName: file.name
              });

              const page = 1;
              const textAnnotation: TextboxAnnotation = {
                id: getRandomPdfRef(),
                class: AnnotationClassType.Annotation,
                type: AnnotationType.Textbox,
                state: AnnotationState.Inserted,
                page,
                x: 100,
                y: 100,
                width: 200,
                height: 30,
                size: 20,
                color: '#66CC00',
                content: 'Это ромашка',
                comments: [],
                statuses: []
              };
              const imageAnnotation: ImageAnnotation = {
                id: getRandomPdfRef(),
                class: AnnotationClassType.Annotation,
                type: AnnotationType.Image,
                state: AnnotationState.Inserted,
                page,
                x: 120,
                y: 130,
                width: 64,
                height: 64,
                content: 'Это ромашка',
                comments: [],
                statuses: [],
                fileName: 'somename.png',
                opacity: 1,
                dataUrl: ChamomilePngDataUrl,
                bytes: getBytesFromDataUrl(ChamomilePngDataUrl)
              };

              viewModel.addAnnotation(page, textAnnotation);
              viewModel.addAnnotation(page, imageAnnotation);

              await viewModel.saveFileToCard();
            },
            null,
            false
          )
        ],
        false
      )
    );
  }
}

const ChamomilePngDataUrl =
  'data:image/png;base64,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';
