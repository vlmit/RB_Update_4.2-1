import {
  TileExtension,
  ITileGlobalExtensionContext,
  Tile,
  TileGroups,
  ITileLocalExtensionContext,
  TileHotkey,
  TileEvaluationEventArgs
} from 'tessa/ui/tiles';
import { UIContext, LoadingOverlay, showNotEmpty, showConfirmWithCancel } from 'tessa/ui';
import { Guid, DotNetType, createTypedField } from 'tessa/platform';
import { CardSingletonCache, CardStoreMode } from 'tessa/cards';
import { ICardModel } from 'tessa/ui/cards';
import {
  CardGetFileContentRequest,
  CardGetFileContentResponse,
  CardService
} from 'tessa/cards/service';
import { LocalizationManager } from 'tessa/localization';
import { extension } from '@tessa/application';
import Barcode from './../images/barcode.png';

const Name = 'PrintBarcode';

@extension()
export class DocLoadPrintBarcodeTileExtension extends TileExtension {
  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const panel = context.workspace.leftPanel;
    const contextSource = panel.contextSource;

    // const cardOthers = panel.tryGetTile('CardOthers');
    // if (!cardOthers) {
    //   return;
    // }

    const tile = new Tile({
      name: Name,
      caption: '$CardTypes_Controls_DocLoad_PrintBarcode',
      icon: 'ta icon-thin-181',
      contextSource,
      command: this.selectPrinterAndPrintAsync.bind(this),
      group: TileGroups.CardsTop,
      order: 1000,
      evaluating: this.evaluatingPrintBarcode,
      toolTip: '$CardTypes_Controls_DocLoad_PrintBarcode'
    });

    panel.tiles.push(tile);
  }

  public initializingLocal(context: ITileLocalExtensionContext): void {
    const leftPanel = context.workspace.leftPanel;

    const tile = leftPanel.tryGetTile(Name);
    if (!tile) {
      return;
    }

    const hotkeyStorage = leftPanel.contextSource.hotkeyStorage;
    hotkeyStorage.addTileHotkey(new TileHotkey(tile, 'Alt+P', 'KeyP', { alt: true }));
  }

  private evaluatingPrintBarcode(e: TileEvaluationEventArgs) {
    const editor = e.currentTile.context.cardEditor;
    let model: ICardModel | null;

    const settingsCard = CardSingletonCache.instance.cards.get('DocLoad');
    if (!settingsCard) {
      return;
    }
    const fields = settingsCard.sections.tryGet('DocLoadSettings')!.fields;
    const isEnabled = !!fields.tryGet('IsEnabled');
    const tableName = fields.tryGet<string>('DefaultBarcodeTableName')!;
    const fieldName = fields.tryGet<string>('DefaultBarcodeFieldName')!;

    e.setIsEnabledWithCollapsing(
      e.currentTile,
      isEnabled &&
        !!tableName &&
        !!fieldName &&
        !!editor &&
        !!(model = editor.cardModel) &&
        model.card.storeMode === CardStoreMode.Update &&
        model.card.sections.has(tableName) &&
        model.card.sections.tryGet(tableName)!.fields.has(fieldName)
    );
  }

  private async selectPrinterAndPrintAsync() {
    const context = UIContext.current;
    const editor = context.cardEditor;
    const model = editor && editor.cardModel;
    if (model == null) {
      return;
    }

    const settingsCard = CardSingletonCache.instance.cards.get('DocLoad');
    if (!settingsCard) {
      return;
    }

    const card = model.card;

    const barcodeBytes = await this.downloadBarcode(card.id);
    if (barcodeBytes == null) {
      return;
    }

    const imageUrl = URL.createObjectURL(barcodeBytes);

    const fields = settingsCard.sections.tryGet('DocLoadSettings')!.fields;
    const showHeader = !!fields.tryGet('ShowHeader');
    const offsetWidth = fields.tryGet<number>('OffsetWidth')!;
    const offsetHeight = fields.tryGet<number>('OffsetHeight')!;

    this.printImage(imageUrl, model.digest, showHeader, offsetHeight, offsetWidth);
  }

  private async downloadBarcode(cardId: string) {
    const context = UIContext.current;
    const editor = context.cardEditor;
    const model = editor && editor.cardModel;

    if (model == null) {
      return null;
    }

    const fileName = 'Barcode.bmp';

    const request = new CardGetFileContentRequest();
    request.cardId = cardId;
    request.fileId = Guid.empty;
    request.fileName = fileName;
    request.versionRowId = Guid.empty;
    request.fileTypeName = 'Barcode';
    if (model.digest) {
      request.info['.digest'] = createTypedField(model.digest, DotNetType.String);
    }

    let response!: CardGetFileContentResponse;
    await LoadingOverlay.instance.show(async () => {
      response = await CardService.instance.getFileContent(request);
    });

    const validationResult = response.validationResult.build();
    await showNotEmpty(validationResult);
    if (!validationResult.isSuccessful || !response.content) {
      return;
    }

    const content = response.content;

    // TODO: временный коммент: пока вместе с файлом не приходит инфа
    // if (response.info['RefreshCard']) {
    if (model.card.storeMode === CardStoreMode.Insert || (await model.hasChanges())) {
      if (!(await showConfirmWithCancel('$UI_Common_ConfirmSave'))) {
        return null;
      }
    }

    if (!(await (editor && editor.saveCard(context)))) {
      return null;
    }
    // }

    return response.hasContent ? content : null;
  }

  printImage(
    imagePath: string,
    digest: string,
    showHeader: boolean,
    offsetHeight = 0,
    offsetWidth = 0
  ): void {
    const width = window.innerWidth;
    const height = window.innerHeight;
    const header =
      (showHeader &&
        `<div style="font-size: 27px;position: absolute;left:270px;top:135px">${LocalizationManager.instance.localize(
          '$CardTypes_TypesNames_DocLoad'
        )}</div><div style="font-size: 20px;position: absolute;left:270px;top:165px">${LocalizationManager.instance.format(
          '$CardTypes_Controls_DocLoad_DocNumber',
          digest
        )}</div><img src="${Barcode}" style="position: absolute;left:50px;top:50px" />`) ||
      '';

    const content = `<!DOCTYPE html><html><body onload="window.focus(); window.print(); window.close();">${header}<img src="${imagePath}" style="position: absolute;left:${
      250 + offsetWidth
    }px;top:${250 + offsetHeight}px"/></body></html>`;
    const options =
      'toolbar=no,location=no,directories=no,menubar=no,scrollbars=yes,width=' +
      width +
      ',height=' +
      height;
    const printWindow = window.open('', 'print', options);
    if (!printWindow) {
      return;
    }
    printWindow.document.open();
    printWindow.document.write(content);
    printWindow.document.close();
    printWindow.focus();
  }
}
