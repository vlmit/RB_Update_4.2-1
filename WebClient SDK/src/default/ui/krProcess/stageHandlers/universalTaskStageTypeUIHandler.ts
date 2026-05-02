import {
  FieldType,
  Guid,
  IValidationResultBuilder,
  StorageArray,
  ValidationKey,
  ValidationResultBuilder,
  ValidationResultType
} from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardRow,
  CardRowState,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { GridRowAction, GridRowEventArgs, GridViewModel } from 'tessa/ui/cards/controls';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { showNotEmpty } from 'tessa/ui';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.universalTaskDescriptor}.
 */
@extension()
export class UniversalTaskStageTypeUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly _krUniversalTaskOptionsSettingsVirtualSynthetic =
    KrStageTypeFormattingHelper.formatSectionName('KrUniversalTaskOptionsSettingsVirtual');

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.universalTaskDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    const grid = context.settingsForms
      .find(i => i.name === WorkflowCardTypes.KrUniversalTaskStageTypeSettingsTypeName)
      ?.blocks.find(i => i.name === 'MainInfo')
      ?.controls.find(i => i.name === 'CompletionOptions') as GridViewModel;

    if (grid) {
      grid.rowInvoked.add(this.rowInvoked);
      grid.rowEditorClosing.add(this.rowClosing);
    }
  }

  async finalize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    const grid = context.settingsForms
      .find(i => i.name === WorkflowCardTypes.KrUniversalTaskStageTypeSettingsTypeName)
      ?.blocks.find(i => i.name === 'MainInfo')
      ?.controls.find(i => i.name === 'CompletionOptions') as GridViewModel;

    if (grid) {
      grid.rowInvoked.remove(this.rowInvoked);
      grid.rowEditorClosing.remove(this.rowClosing);
    }
  }

  //#endregion

  //#region private methods

  private rowInvoked(args: GridRowEventArgs): void {
    if (args.action === GridRowAction.Inserted) {
      args.row.set('OptionID', Guid.newGuid(), FieldType.Guid);
    }
  }

  private async rowClosing(args: GridRowEventArgs): Promise<void> {
    const row = args.row;
    let validationResult: IValidationResultBuilder | undefined;

    const optionId = row.get<string>('OptionID');
    if (optionId) {
      const rows = args.cardModel.card.sections.tryGet(
        UniversalTaskStageTypeUIHandler._krUniversalTaskOptionsSettingsVirtualSynthetic
      )?.rows;

      if (
        rows &&
        UniversalTaskStageTypeUIHandler.checkDuplicatesOptionId(rows, row.rowId, optionId)
      ) {
        validationResult ??= new ValidationResultBuilder();
        validationResult.add(
          ValidationKey.unknown,
          ValidationResultType.Error,
          '$KrProcess_UniversalTask_CompletionOptionIDNotUnique'
        );
        args.cancel = true;
      }
    } else {
      validationResult ??= new ValidationResultBuilder();
      validationResult.add(
        ValidationKey.unknown,
        ValidationResultType.Error,
        '$KrProcess_UniversalTask_CompletionOptionIDEmpty'
      );
      args.cancel = true;
    }

    if (!row.tryGet('Caption')) {
      validationResult ??= new ValidationResultBuilder();
      validationResult.add(
        ValidationKey.unknown,
        ValidationResultType.Error,
        '$KrProcess_UniversalTask_CompletionOptionCaptionEmpty'
      );
      args.cancel = true;
    }

    if (validationResult) {
      await showNotEmpty(validationResult.build());
    }
  }

  private static checkDuplicatesOptionId(
    rows: StorageArray<CardRow>,
    rowId: string,
    optionId: string
  ): boolean {
    for (const row of rows) {
      if (Guid.equals(row.rowId, rowId) || row.state === CardRowState.Deleted) {
        continue;
      }

      const iOptionId = row.tryGet<string>('OptionID');

      if (!iOptionId) {
        continue;
      }

      if (Guid.equals(optionId, iOptionId)) {
        return true;
      }
    }

    return false;
  }

  //#endregion
}
