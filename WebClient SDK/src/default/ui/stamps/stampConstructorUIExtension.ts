import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridViewModel } from 'tessa/ui/cards/controls';
import { MenuAction, UIContext, showMessage, showNotEmpty } from 'tessa/ui';
import { getTessaIcon } from 'common/utility';
import { extension } from '@tessa/application';
import { PreviewHelper } from 'tessa/ui/preview';
import { autorun } from 'mobx';
import {
  AnnotationState,
  AnnotationType,
  BaseXYAnnotation,
  EditorMode,
  ImageAnnotation,
  IPdfAnnotationsService,
  IPdfAnnotationsService$,
  StampsAnnotationDialogViewModelFactory,
  StampsAnnotationDialogViewModelFactory$,
  ExportType
} from 'tessa/pdf-annotations';
import type { IPdfStamp } from 'tessa/pdf-annotations';
import {
  AsyncLazy,
  Base64Helper,
  FieldType,
  Guid,
  TypedField,
  TypedJsonConverter,
  ValidationResult,
  ValidationResultType
} from '@tessa/core';
import { readFileContentAsBase64 } from 'tessa/files';
import { CardRowState, IFileContentSaver, IFileContentSaver$ } from '@tessa/platform';

const allowedExts = ['png', 'jpg', 'jpeg', 'svg'];

@extension({ name: 'StampConstructorUIExtension' })
export class StampConstructorUIExtension extends CardUIExtension {
  private _disposes: Array<(() => void) | null> = [];
  private _syncPageEvents: Array<() => void> = [];

  constructor(
    @StampsAnnotationDialogViewModelFactory$({ lazy: true })
    private readonly dialogFactory: AsyncLazy<StampsAnnotationDialogViewModelFactory>,
    @IPdfAnnotationsService$({ lazy: true })
    private readonly pdfAnnotationServiceFactory: AsyncLazy<IPdfAnnotationsService>,
    @IFileContentSaver$() protected readonly fileContentSaver: IFileContentSaver
  ) {
    super();
  }

  public initialized(context: ICardUIExtensionContext): void {
    // запрещаем добавлять файлы с расширением .exe
    this._disposes.push(
      context.fileContainer.containerFileChanging.addWithDispose(async e => {
        const file = e.added;
        if (file && !isAllowedExt(file.getExtension(), allowedExts)) {
          await showMessage(`Allowed extensions: ${allowedExts.join(', ')}`);
          e.cancel = true;
        }
      })
    );

    const stampGrid = context.model.controls.get('Stamps') as GridViewModel;
    if (stampGrid) {
      stampGrid.contextMenuGenerators.push(ctx => {
        ctx.menuActions.splice(
          0,
          0,
          MenuAction.create({
            type: 'normal',
            name: 'Name',
            caption: '$UI_FacsimileAndAnnotationsGroup_Edit_stamp',
            icon: 'ta icon-thin-120',
            action: async () => {
              const { mapAnnsToStorage, mapStorageToAnns, getSvgBlob } =
                await import('tessa/pdf-annotations/chunk');
              const editor = UIContext.current.cardEditor!;

              const annsStr = ctx.row.row.getString('Annotations');
              const annotations = annsStr
                ? mapStorageToAnns(TypedJsonConverter.deserializeList(annsStr))
                : [];

              const factory = await this.dialogFactory.getValue();
              const viewModel = factory({
                editor,
                stampName: ctx.row.row.getString('Name') || '',
                annotations,
                editorMode: EditorMode.EditAndImage,
                stamps: [],
                width: 650,
                height: 650,
                canvas: true,
                onSave: async anns => {
                  const annsStorage = mapAnnsToStorage(
                    anns.map(x => {
                      x.state = AnnotationState.Stored;
                      return x;
                    })
                  );

                  ctx.row.row.set(
                    'Annotations',
                    TypedField.createString(
                      TypedJsonConverter.serialize(annsStorage, { preservePropOrder: true })
                    )
                  );

                  const getFileToSave = async (
                    svg: SVGSVGElement,
                    placeholder = false
                  ): Promise<string | undefined> => {
                    const svgBlob = getSvgBlob(anns as BaseXYAnnotation[], svg, placeholder);

                    const fileData = await readFileContentAsBase64(
                      new File([svgBlob], ctx.row.row.get('Name') + '.svg')
                    );
                    if (!fileData) {
                      await showNotEmpty(
                        ValidationResult.fromText(
                          'Can not read file content.',
                          ValidationResultType.Error
                        )
                      );
                      return;
                    }

                    return fileData;
                  };

                  const svg = viewModel
                    .tryGetComponentRef<HTMLDivElement>()
                    ?.querySelector<SVGSVGElement>('.annotationSvgLayer');

                  if (!svg) {
                    return;
                  }

                  let fileData = await getFileToSave(svg);
                  if (!fileData) {
                    return;
                  }

                  ctx.row.row.set('PreviewFile', TypedField.create(fileData, FieldType.Binary));

                  fileData = await getFileToSave(svg, true);
                  if (!fileData) {
                    return;
                  }

                  ctx.row.row.set('File', TypedField.create(fileData, FieldType.Binary));
                }
              });

              await viewModel.showDialog();
            }
          }),
          MenuAction.create({
            type: 'normal',
            name: 'EditRow',
            caption: '$UI_FacsimileAndAnnotationsGroup_Edit_row',
            icon: getTessaIcon('Thin2'),
            action: async () => {
              await stampGrid.editRow(ctx.row, ctx.columnIndex);
            }
          }),

          MenuAction.create({
            type: 'normal',
            name: 'TestPreviewFile',
            caption: '$UI_FacsimileAndAnnotationsGroup_Save_preview_to_file',
            icon: getTessaIcon('Thin2'),
            action: async () => {
              const file = ctx.row.row.get('PreviewFile') as string;
              if (!file) {
                return;
              }

              const svgUrl = 'data:image/svg+xml;base64,' + file;
              await this.fileContentSaver.save(svgUrl, ctx.row.row.get('Name') + '.svg');
            }
          }),

          MenuAction.create({
            type: 'normal',
            name: 'TestPlaceholderedFile',
            caption: '$UI_FacsimileAndAnnotationsGroup_Save_placeholdered_to_file',
            icon: getTessaIcon('Thin2'),
            action: async () => {
              const file = ctx.row.row.get('File') as string;
              if (!file) {
                return;
              }

              const svgUrl = 'data:image/svg+xml;base64,' + file;
              await this.fileContentSaver.save(svgUrl, ctx.row.row.get('Name') + '.svg');
            }
          }),

          MenuAction.create({
            type: 'normal',
            name: 'TestPreviewPng',
            caption: '$UI_FacsimileAndAnnotationsGroup_Save_preview_png',
            icon: getTessaIcon('Thin2'),
            action: async () => {
              const pdfAnnotationsService = await this.pdfAnnotationServiceFactory.getValue();
              const { base64File, validationResult } = await pdfAnnotationsService.export(
                context.card.id,
                ctx.row.row.rowId,
                context.card.id,
                true,
                ExportType.Png
              );
              if (validationResult && (await showNotEmpty(validationResult))) {
                return;
              }

              const svgUrl = 'data:image/png;base64,' + base64File;
              await this.fileContentSaver.save(svgUrl, ctx.row.row.get('Name') + '.svg');
            }
          })
        );
      });
      this._disposes.push(
        autorun(async () => {
          const selectedRow = stampGrid.selectedRow;
          if (selectedRow) {
            const file = selectedRow.row.get('PreviewFile') as string | undefined;
            if (!file) {
              return;
            }
            context.model.previewManager.showPreview(
              await PreviewHelper.createFileVersionForPreview({
                fileName: selectedRow.row.get('Name') + '.svg',
                getContent: async () => {
                  const svgUrl = file;

                  return Base64Helper.base64ToBlob(svgUrl);
                }
              })
            );
          }
        })
      );
    }

    const stampPlacesGrid = context.model.controls.get('StampPlaces') as GridViewModel;
    if (stampPlacesGrid) {
      stampPlacesGrid.contextMenuGenerators.push(ctx => {
        ctx.menuActions.splice(
          0,
          0,
          MenuAction.create({
            type: 'normal',
            name: 'Name',
            caption: '$UI_FacsimileAndAnnotationsGroup_Edit_stamp_place',
            icon: 'ta icon-thin-120',
            action: async () => {
              const { mapAnnsToStorage, mapStorageToAnns } =
                await import('tessa/pdf-annotations/chunk');
              const editor = UIContext.current.cardEditor!;

              const stamps: IPdfStamp[] = [];
              const stampsRows = context.card.sections.tryGet('Stamps')?.rows || [];
              for (const row of stampsRows) {
                const fileStr = row.get('PreviewFile');
                if (!fileStr) {
                  continue;
                }

                stamps.push({
                  name: (row.get('Name') as string) + '.svg',
                  url: 'data:image/svg+xml;base64,' + fileStr,
                  stampConstructorCardId: context.card.id,
                  isStamp: true,
                  referenceId: row.rowId
                });
              }

              const annsStr = ctx.row.row.getString('Place');
              const annotations = annsStr
                ? mapStorageToAnns(TypedJsonConverter.deserializeList(annsStr))
                : [];

              const stampPlaceId = ctx.row.rowId;

              const factory = await this.dialogFactory.getValue();
              const viewModel = factory({
                editor,
                stampName: ctx.row.row.getString('Name') || '',
                annotations,
                editorMode: EditorMode.ImageAdding,
                stamps,
                width: 595,
                height: 842,
                canvas: true,
                onSave: async anns => {
                  const annsStorage = mapAnnsToStorage(
                    anns.map(x => {
                      x.state = AnnotationState.Stored;
                      return x;
                    })
                  );

                  const stampsStampsPlaces = context.card
                    .tryGetSections()!
                    .get('StampsStampsPlaces');

                  // to remove rows
                  // to add rows
                  const rows = stampsStampsPlaces.rows.filter(
                    x => x.get('StampPlaceRowID') == stampPlaceId
                  );
                  let stampIds = Object.keys(
                    (
                      (anns.filter(x => x.type === AnnotationType.Image) as ImageAnnotation[])
                        .map(x => x.referenceId)
                        .filter(Boolean) as string[]
                    ).reduce((prev, curr) => ({ ...prev, [curr]: true }), {})
                  );

                  for (const stampRow of rows) {
                    const stampId = stampRow.get('StampRowID');
                    if (!stampIds.some(x => x === stampId)) {
                      stampRow.state = CardRowState.Deleted;
                    } else {
                      stampIds = stampIds.filter(x => x !== stampId);
                    }
                  }

                  for (const stampId of stampIds) {
                    const row = stampsStampsPlaces.rows.add();
                    row.state = CardRowState.Inserted;
                    row.rowId = Guid.newGuid();
                    row.set('StampPlaceRowID', TypedField.createGuid(stampPlaceId));
                    row.set('StampRowID', TypedField.createGuid(stampId));
                    const stampRow = stampsRows.find(x => x.rowId === stampId);
                    let name = '';
                    if (stampRow) {
                      name = stampRow.get('Name') as string;
                    }
                    row.set('StampName', TypedField.createString(name));
                  }

                  ctx.row.row.set(
                    'Place',
                    TypedField.createString(
                      TypedJsonConverter.serialize(annsStorage, { preservePropOrder: true })
                    )
                  );

                  const fileData = await readFileContentAsBase64(
                    new File([await viewModel.generateFile()], ctx.row.row.get('Name') + '.svg')
                  );

                  ctx.row.row.set('File', TypedField.create(fileData, FieldType.Binary));
                }
              });

              await viewModel.showDialog();
            }
          }),
          MenuAction.create({
            type: 'normal',
            name: 'EditRow',
            caption: '$UI_FacsimileAndAnnotationsGroup_Edit_row',
            icon: getTessaIcon('Thin2'),
            action: async () => {
              await stampPlacesGrid.editRow(ctx.row, ctx.columnIndex);
            }
          })
        );
      });
      this._disposes.push(
        autorun(async () => {
          const selectedRow = stampPlacesGrid.selectedRow;
          if (selectedRow) {
            const file = selectedRow.row.get('File') as string | undefined;
            if (!file) {
              return;
            }
            context.model.previewManager.showPreview(
              await PreviewHelper.createFileVersionForPreview({
                fileName: selectedRow.row.get('Name') + '.pdf',
                getContent: async () => {
                  const svgUrl = file;
                  return Base64Helper.base64ToBlob(svgUrl);
                }
              })
            );
          }
        })
      );
    }
  }

  public finalized(): void {
    for (const func of this._disposes) {
      if (func) {
        func();
      }
    }
    this._disposes.length = 0;

    this.clearSyncPagesEvents();
  }

  private clearSyncPagesEvents(): void {
    for (const f of this._syncPageEvents) {
      f();
    }
    this._syncPageEvents.length = 0;
  }
}

const isAllowedExt = (ext: string, allowed: string[]) => allowed.indexOf(ext) !== -1;
