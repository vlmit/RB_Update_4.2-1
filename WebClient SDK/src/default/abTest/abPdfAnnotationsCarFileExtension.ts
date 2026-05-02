import { AsyncLazy } from '@tessa/core';
import { extension, localize } from '@tessa/application';
import { FileHelper } from '@tessa/platform';
import { getNameAndExtForFile, userSession } from 'common';
import { MenuAction, MessageBoxButtons, show, UIContext } from 'tessa/ui';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import {
  PdfAnnotationDialogViewModelFactory,
  PdfAnnotationDialogViewModelFactory$,
  AnnotationState,
  ImageAnnotation,
  AnnotationClassType,
  EditorMode,
  AnnotationType,
  ImageLikeTypeSet,
  IPdfAnnotationDialogViewModel
} from 'tessa/pdf-annotations';

const AbCarCardTypeID = 'd0006e40-a342-4797-8d77-6501c4b7c4ac';

@extension({ name: 'AbPdfAnnotationsCarFileExtension' })
export class AbPdfAnnotationsCarFileExtension extends FileExtension {
  constructor(
    @PdfAnnotationDialogViewModelFactory$({ lazy: true })
    private readonly dialogFactory: AsyncLazy<PdfAnnotationDialogViewModelFactory>
  ) {
    super();
  }

  public shouldExecute(): boolean {
    if (UIContext.current.cardEditor?.cardModel?.cardType.id !== AbCarCardTypeID) {
      return false;
    }

    return true;
  }

  public openingMenu(context: IFileExtensionContext): void {
    if (
      'pdf' !== getNameAndExtForFile(context.file.model?.lastVersion.name).ext?.toLocaleLowerCase()
    ) {
      return;
    }

    let fascimileActionIndex = -1;
    for (const itemName of ['DeleteAnnotations', 'Fascimile', 'EditAnnotations']) {
      fascimileActionIndex = context.actions.findIndex(x => x.name === itemName);
      if (fascimileActionIndex !== -1) {
        break;
      }
    }

    if (fascimileActionIndex === -1) {
      return;
    }

    const file = context.file.model;
    const version = file.lastVersion;
    const editor = UIContext.current.cardEditor!;

    const handler =
      ({
        editorMode,
        onInit,
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
          onInit,
          editorMode,
          fileName: file.name
        });

        viewModel = vmTransform?.(viewModel) || viewModel;

        viewModel.stamps = [];

        await viewModel.showDialog();
      };

    context.actions.splice(
      fascimileActionIndex + 1,
      0,
      new MenuAction(
        'PdfAnnsExamples',
        '$AbTest_UI_FacsimileAndAnnotationsGroup_PdfAnnsExamples',
        'ta icon-thin-119',
        null,
        [
          new MenuAction(
            'AddFascimile',
            '$AbTest_UI_FacsimileAndAnnotationsGroup_AddFascimile',
            'ta icon-thin-119',
            () => {
              const input = document.createElement('INPUT');
              input.setAttribute('type', 'file');
              input.setAttribute('accept', '.jpg,.jpeg,.png');
              input.style.display = 'none';
              document.body.appendChild(input);
              input.addEventListener('change', async e => {
                const target = e.target as HTMLInputElement;
                const file = target.files![0];
                target.remove();
                const bytes = await file.arrayBuffer();
                const reader = new FileReader();
                reader.onload = e => {
                  handler({
                    editorMode: EditorMode.ImageAdding,
                    onInit: vm => {
                      const image = new Image();
                      image.src = e.target!.result as string;
                      image.onload = () => {
                        const dataUrl = FileHelper.makeRightImageDataUrl(
                          e.target?.result as string,
                          file.name
                        );

                        vm.addAnnotation(1, {
                          class: AnnotationClassType.Annotation,
                          type: AnnotationType.Image,
                          page: 1,
                          userId: userSession.UserID,
                          userName: userSession.UserName,
                          fileName: file.name,
                          x: 0,
                          y: 0,
                          width: image.width,
                          height: image.height,
                          bytes,
                          opacity: 0.5,
                          dataUrl
                        });
                      };
                    }
                  })();
                };
                reader.readAsDataURL(file);
              });
              input.click();
            },
            null,
            false
          ),
          new MenuAction(
            'GetAddedAnnotation',
            '$AbTest_UI_FacsimileAndAnnotationsGroup_SendForSigning',
            'ta icon-thin-119',
            handler({
              editorMode: EditorMode.ImageAdding,
              vmTransform: vm => {
                const saveButton = vm.mainActions.find(x => x.name === 'SaveButton');
                if (saveButton) {
                  saveButton.caption = '$AbTest_UI_FacsimileAndAnnotationsGroup_SendButton';
                  saveButton.buttonAction = async () => {
                    const annsObj = vm.annotations;
                    if (annsObj) {
                      const anns = Object.values(annsObj)
                        .flat()
                        .filter(
                          x => x.state === AnnotationState.Inserted && ImageLikeTypeSet.has(x.type)
                        ) as ImageAnnotation[];
                      if (anns.length === 0) {
                        await show({
                          text: '$AbTest_UI_FacsimileAndAnnotationsGroup_DoesntAddPicture',
                          caption: '$AbTest_UI_FacsimileAndAnnotationsGroup_Man',
                          buttons: MessageBoxButtons.OK
                        });
                      } else {
                        await show({
                          text: anns
                            .map(ann =>
                              localize(
                                '$AbTest_UI_FacsimileAndAnnotationsGroup_GoodBoyResult',
                                ann.page,
                                ann.x,
                                ann.y,
                                ann.width,
                                ann.height
                              )
                            )
                            .join('\n'),
                          caption: '$AbTest_UI_FacsimileAndAnnotationsGroup_GoodBoy',
                          buttons: MessageBoxButtons.OK
                        });
                      }
                    }
                  };
                }

                return vm;
              }
            }),
            null,
            !file.permissions.canEdit
          )
        ]
      )
    );
  }
}
