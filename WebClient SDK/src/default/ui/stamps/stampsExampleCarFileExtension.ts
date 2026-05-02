import { extension, inject } from '@tessa/application';
import { getNameAndExtForFile } from 'common';
import { MenuAction, showNotEmpty, showViewModelDialog, UIContext } from 'tessa/ui';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import { ICardService$, ICardService, CardGetRequest, Card } from '@tessa/platform';
import {
  StampPlace,
  StampPlaceSelectionDialogViewModel
} from './stampPlaceSelectionDialogViewModel';

import { StampPlaceSelectionDialog } from './stampPlaceSelectionDialog';
import type { IPdfStamp } from 'tessa/pdf-annotations';
import {
  EditorMode,
  IPdfAnnotationDialogViewModel,
  IPdfAnnotationsService,
  IPdfAnnotationsService$,
  PdfAnnotationDialogViewModelFactory,
  PdfAnnotationDialogViewModelFactory$
} from 'tessa/pdf-annotations';
import { AsyncLazy } from '@tessa/core';

const CarCardTypeID = 'd0006e40-a342-4797-8d77-6501c4b7c4ac';
const StampConstructorCardID = '79307713-0da1-4559-a530-460acdbb5faa';
@extension({ name: 'StampsExampleCarFileExtension' })
export class StampsExampleCarFileExtension extends FileExtension {
  constructor(
    @inject(ICardService$) private _cardService: ICardService,
    @IPdfAnnotationsService$({ lazy: true })
    private readonly pdfAnnotationServiceFactory: AsyncLazy<IPdfAnnotationsService>,
    @PdfAnnotationDialogViewModelFactory$({ lazy: true })
    private readonly pdfAnnotationsDialogFactory: AsyncLazy<PdfAnnotationDialogViewModelFactory>
  ) {
    super();
  }

  static async getCard(cardService: ICardService): Promise<Card | undefined> {
    const cardGetRequest = new CardGetRequest();

    cardGetRequest.cardId = StampConstructorCardID;
    const response = await cardService.get(cardGetRequest);

    if (!response.validationResult.isSuccessful) {
      return undefined;
    }

    return response.card;
  }

  static async getStamps(cardId: string, cardService: ICardService): Promise<IPdfStamp[]> {
    const cardGetRequest = new CardGetRequest();

    cardGetRequest.cardId = StampConstructorCardID;
    const response = await cardService.get(cardGetRequest);

    if (!response.validationResult.isSuccessful) {
      await showNotEmpty(response.validationResult.build());
      return [];
    }

    const stamps: IPdfStamp[] = [];
    const rows = response.card.sections.get('Stamps').rows;
    for (const row of rows) {
      const file = row.get('PreviewFile') as string;
      if (!file) {
        continue;
      }

      const svgUrl = 'data:image/svg+xml;base64,' + file;

      stamps.push({
        name: (row.get('Name') as string) + '.png',
        url: svgUrl,
        isStamp: true,
        cardId,
        stampConstructorCardId: StampConstructorCardID,
        stampId: row.rowId
      });
    }

    return stamps;
  }

  public shouldExecute(): boolean {
    if (UIContext.current.cardEditor?.cardModel?.cardType.id !== CarCardTypeID) {
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
        vmTransform
      }: {
        editorMode: EditorMode;
        onInit?: (vm: IPdfAnnotationDialogViewModel) => void;
        vmTransform?: (vm: IPdfAnnotationDialogViewModel) => IPdfAnnotationDialogViewModel;
      }) =>
      async () => {
        await version.ensureContentDownloaded();

        const factory = await this.pdfAnnotationsDialogFactory.getValue();
        let viewModel = await factory({
          file,
          version,
          editor,
          onBeforeSave: async ({ annotations }) => {
            return { annotations, cancel: false };
          },
          editorMode: EditorMode.EditAndImage
        });

        viewModel = vmTransform?.(viewModel) || viewModel;

        viewModel.stamps = await StampsExampleCarFileExtension.getStamps(
          editor.cardModel!.card.id,
          this._cardService
        );

        await viewModel.showDialog();
      };

    // const menus = (await StampsExampleCarFileExtension.getCard(this._cardService))?.sections;

    context.actions.splice(
      fascimileActionIndex + 1,
      0,

      new MenuAction(
        'GetStamps',
        'Stamps',
        'ta icon-thin-119',
        handler({
          editorMode: EditorMode.EditAndImage
        }),
        null,
        !file.permissions.canEdit
      ),
      new MenuAction(
        'InsertServerStamp',
        'InsertServerStamp',
        'ta icon-thin-119',

        async () => {
          const stampConstructor = await StampsExampleCarFileExtension.getCard(this._cardService);

          const stampPlacesRows = stampConstructor?.sections.tryGet('StampPlaces')?.rows;
          if (!stampPlacesRows) {
            return;
          }

          const stampsStampsPlaces = stampConstructor?.sections
            .tryGet('StampsStampsPlaces')
            ?.rows.map(x => ({
              StampName: x.get<string>('StampName')!,
              StampPlaceRowID: x.get<string>('StampPlaceRowID')!
            }));

          const stampPlaces: StampPlace[] = stampPlacesRows.map(x => {
            const rowId = x.get<string>('RowID')!;
            return {
              RowID: rowId,
              Name: x.get<string>('Name')!,
              Stamps:
                stampsStampsPlaces
                  ?.filter(y => y.StampPlaceRowID === rowId)
                  .map(y => ({ Name: y.StampName })) || []
            };
          });

          const dialogViewModel = new StampPlaceSelectionDialogViewModel(stampPlaces);

          const dialogResult = await showViewModelDialog<{
            stampPlace: StampPlace | null;
            cancel: boolean;
          }>(dialogViewModel, StampPlaceSelectionDialog);

          let isCanceled = dialogResult.cancel;
          if (isCanceled || !dialogResult.stampPlace) {
            // диалог вернул "Без категории", хотя файлы без категории запрещены
            isCanceled = true;
            return;
          }

          const cardId = editor.cardModel!.card.id;
          const fileId = file.id;
          const pdfAnnotationsService = await this.pdfAnnotationServiceFactory.getValue();
          const { validationResult } = await pdfAnnotationsService.addStamp(
            cardId,
            fileId,
            StampConstructorCardID,
            dialogResult.stampPlace.RowID,
            true
          );
          if (validationResult) {
            showNotEmpty(validationResult);
          } else {
            await editor.refreshCard();
          }
        },
        null,
        !file.permissions.canEdit
      )
    );
  }
}
