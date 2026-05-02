import { IKrTileCommand } from './interfaces';
import { IUIContext, showConfirm } from 'tessa/ui';
import {
  KrSecondaryProcessTileUIHandlerRegistry,
  launchProcessWithCardEditor
} from 'tessa/ui/workflow/krProcess';
import { Guid } from 'tessa/platform';
import { ICardModel, CardSavingRequest, CardSavingMode } from 'tessa/ui/cards';
import { ITile } from 'tessa/ui/tiles';
import { KrProcessInstance, KrTileInfo } from '@tessa/platform';
import { IStorage } from '@tessa/core';

export class KrLocalTileCommand implements IKrTileCommand {
  //#region ctor

  private constructor() {}

  //#endregion

  //#region instance

  private static _instance: KrLocalTileCommand;

  public static get instance(): KrLocalTileCommand {
    if (!KrLocalTileCommand._instance) {
      KrLocalTileCommand._instance = new KrLocalTileCommand();
    }
    return KrLocalTileCommand._instance;
  }

  //#endregion

  //#region IKrTileCommand members

  public async onClickAction(
    context: IUIContext,
    _tile: ITile | null,
    tileInfo: KrTileInfo
  ): Promise<void> {
    const cardEditor = context.cardEditor;
    let model: ICardModel;
    if (tileInfo.id === Guid.empty || !cardEditor || !(model = cardEditor.cardModel!)) {
      return;
    }

    let additionalStorage: IStorage | undefined = undefined;
    if (tileInfo.handlerId) {
      additionalStorage = {};
      const handler = KrSecondaryProcessTileUIHandlerRegistry.instance.get(tileInfo.handlerId);
      if (handler && !(await handler.handleTile(context, tileInfo, additionalStorage))) {
        return;
      }
    }

    if (tileInfo.askConfirmation) {
      const result = await showConfirm(tileInfo.confirmationMessage);
      if (!result) {
        return;
      }
    }

    await cardEditor.setOperationInProgress(async () => {
      if (await model.hasChanges()) {
        if (
          !(await cardEditor.saveCard(
            context,
            undefined,
            new CardSavingRequest(CardSavingMode.RefreshOnSuccess)
          ))
        ) {
          return;
        }
      }
      const process = KrProcessInstance.create({
        processId: tileInfo.id,
        processInfo: additionalStorage,
        cardId: context.cardEditor!.cardModel!.card.id
      });

      await launchProcessWithCardEditor(process, cardEditor, true);
    });
  }

  //#endregion
}
