import {
  TileExtension,
  ITileGlobalExtensionContext,
  Tile,
  TileGroups,
  TileEvaluationEventArgs
} from 'tessa/ui/tiles';
import { ICardModel, CardSavingRequest, CardSavingMode } from 'tessa/ui/cards';
import { hasFlag, createTypedField, DotNetType } from 'tessa/platform';
import { CardTypeFlags } from 'tessa/cards/types';
import { CardStoreMode } from 'tessa/cards';
import { UIContext, tryGetFromInfo } from 'tessa/ui';
import { IStorage } from 'tessa/platform/storage';
import { WorkflowQueue } from 'tessa/cards/workflow';
import { extension } from '@tessa/application';

@extension()
export class AbTestProcessTileExtension extends TileExtension {
  public async initializingGlobal(context: ITileGlobalExtensionContext): Promise<void> {
    const contextSource = context.workspace.leftPanel.contextSource;
    context.workspace.leftPanel.tiles.push(
      new Tile({
        name: 'StartTestProcess',
        caption: '$AbTest_TestApprovalTile',
        icon: 'ta icon-thin-127',
        contextSource,
        command: AbTestProcessTileExtension.startTestProcessAction,
        group: TileGroups.CardsTop,
        order: 6,
        evaluating: AbTestProcessTileExtension.enableOnTestTypesAndNoProcesses
      }),
      new Tile({
        name: 'SendTestSignal',
        caption: '$AbTest_TestSignalTile',
        icon: 'ta icon-thin-229',
        contextSource,
        command: AbTestProcessTileExtension.sendTestSignalAction,
        group: TileGroups.CardsTop,
        order: 6,
        evaluating: AbTestProcessTileExtension.enableOnTestTypesAndHasProcesses
      })
    );
  }

  private static enableOnTestTypesAndNoProcesses(e: TileEvaluationEventArgs) {
    const editor = e.currentTile.context.cardEditor;
    let model: ICardModel;
    e.setIsEnabledWithCollapsing(
      e.currentTile,
      !!editor &&
        !!(model = editor.cardModel!) &&
        hasFlag(model.cardType.flags, CardTypeFlags.AllowTasks) &&
        model.cardType.name === 'AbCar' &&
        model.card.storeMode === CardStoreMode.Update &&
        model.card.sections.get('WorkflowProcesses').rows.length === 0
    );
  }

  private static enableOnTestTypesAndHasProcesses(e: TileEvaluationEventArgs) {
    const editor = e.currentTile.context.cardEditor;
    let model: ICardModel;
    e.setIsEnabledWithCollapsing(
      e.currentTile,
      !!editor &&
        !!(model = editor.cardModel!) &&
        hasFlag(model.cardType.flags, CardTypeFlags.AllowTasks) &&
        model.cardType.name === 'AbCar' &&
        model.card.storeMode === CardStoreMode.Update &&
        model.card.sections.get('WorkflowProcesses').rows.length > 0
    );
  }

  private static async startTestProcessAction() {
    const context = UIContext.current;
    const editor = context.cardEditor;

    if (!editor || !editor.cardModel) {
      return;
    }

    await editor.saveCard(context, {
      '.startProcess': createTypedField('AbTestProcess', DotNetType.String)
    });
  }

  private static async sendTestSignalAction() {
    const context = UIContext.current;
    const editor = context.cardEditor;

    if (!editor || !editor.cardModel) {
      return;
    }

    await editor.saveCard(
      context,
      undefined,
      new CardSavingRequest(CardSavingMode.RefreshOnSuccess, card => {
        const info = card.info;
        let storage: IStorage;
        let queue: WorkflowQueue;
        if ((storage = tryGetFromInfo(info, '.workflowQueue', null)!)) {
          queue = new WorkflowQueue(storage);
        } else {
          queue = new WorkflowQueue();
          info['.workflowQueue'] = queue.getStorage();
        }

        queue.addSignal({ processTypeName: 'Main', name: 'TestSignal' });
      })
    );
  }
}
