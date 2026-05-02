import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { extension, localize } from '@tessa/application';
import { GridViewModel } from 'tessa/ui/cards/controls';
import { FieldType } from '@tessa/core';
import { CardRowState } from '@tessa/platform';
import { runtimeCard } from '../../workflow/krProcess/krUIHelper';

@extension()
export class KrStageDialogTitleUIExtension extends CardUIExtension {
  //#region base overrides

  public initialized(context: ICardUIExtensionContext): void {
    const cardModel = context.model;

    // пытаемся найти контрол таблицы этапов маршрута
    const approvalStagesTable = cardModel.controls.get('ApprovalStagesTable') as GridViewModel;
    if (!approvalStagesTable) {
      return;
    }

    this.disposeList.add(
      approvalStagesTable.rowInitialized.addWithDispose(args => {
        const stageTypeCaption = args.row.tryGet<string>('StageTypeCaption', FieldType.String);

        approvalStagesTable.dialogTitle =
          args.row.state === CardRowState.Inserted && runtimeCard(args.cardModel.card.typeId)
            ? localize(
                '$UI_StageRowWindowTitle',
                stageTypeCaption,
                args.row.tryGet<string>('StageGroupName', FieldType.String)
              )
            : localize(stageTypeCaption);
      })!
    );
  }

  //#endregion
}
