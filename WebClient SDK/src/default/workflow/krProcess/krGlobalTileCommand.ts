import { IKrTileCommand } from './interfaces';
import { Guid } from 'tessa/platform';
import { IUIContext, LoadingOverlay, showConfirm, showNotEmpty } from 'tessa/ui';
import {
  KrSecondaryProcessTileUIHandlerRegistry,
  launchProcess
} from 'tessa/ui/workflow/krProcess';
import { ITile } from 'tessa/ui/tiles';
import { KrProcessInstance, KrTileInfo } from '@tessa/platform';
import { IStorage } from '@tessa/core';

export class KrGlobalTileCommand implements IKrTileCommand {
  //#region ctor

  private constructor() {}

  //#endregion

  //#region instance

  private static _instance: KrGlobalTileCommand;

  public static get instance(): KrGlobalTileCommand {
    if (!KrGlobalTileCommand._instance) {
      KrGlobalTileCommand._instance = new KrGlobalTileCommand();
    }
    return KrGlobalTileCommand._instance;
  }

  //#endregion

  //#region IKrTileCommand members

  public async onClickAction(
    context: IUIContext,
    _tile: ITile,
    tileInfo: KrTileInfo
  ): Promise<void> {
    if (tileInfo.id === Guid.empty) {
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

    await LoadingOverlay.instance.show(async () => {
      const process = KrProcessInstance.create({
        processId: tileInfo.id,
        processInfo: additionalStorage
      });
      const result = await launchProcess(process, { raiseErrorWhenExecutionIsForbidden: true });
      if (result) {
        await showNotEmpty(result.validationResult.build());
      }
    });
  }

  //#endregion
}
