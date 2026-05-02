import { extension } from '@tessa/application';
import { Guid } from '@tessa/core';
import { showLoadingOverlay } from 'tessa/ui';
import { GridViewModel } from 'tessa/ui/cards/controls/grid/gridViewModel';
import { openCard } from 'tessa/ui/uiHost';
import { DoubleClickInfo, openCardDoubleClickAction } from 'tessa/ui/views';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { IWorkplaceViewComponent } from 'tessa/ui/views/workplaceViewComponent';

/**
 * Расширение, открывающее параметры этапа по двойному клику по строке представления <b>KrStageRows</b>.
 */
@extension()
export class OpenStageSettingsOnDoubleClickExtension extends WorkplaceViewComponentExtension {
  //#region base overrides

  override getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Cards.OpenStageSettingsOnDoubleClickExtension';
  }

  override initialize(model: IWorkplaceViewComponent): void {
    if (!model.doubleClickAction || model.inSelectionMode()) {
      return;
    }

    model.doubleClickAction = async (info: DoubleClickInfo) => {
      await openCardDoubleClickAction(info, async (cardId, displayValue, context) => {
        const selectedRow = info.context.viewContext?.selectedRow;
        if (!selectedRow) {
          return;
        }

        const stageRowID = selectedRow.get('StageRowID');
        if (!Guid.isValid(stageRowID)) {
          return;
        }

        const editor = await showLoadingOverlay(async () => {
          return await openCard({
            cardId,
            displayValue,
            context
          });
        });

        if (!editor) {
          return;
        }

        const cardModel = editor.cardModel!;

        const approvalProcessTab = cardModel.mainFormWithTabs?.tabs.find(
          i => i.name == 'ApprovalProcess'
        );
        if (!approvalProcessTab) {
          return;
        }

        const grid = cardModel.controls.get('ApprovalStagesTable') as GridViewModel;
        if (!grid) {
          return;
        }

        const stageRowViewModel = grid.rows.find(i => Guid.equals(i.rowId, stageRowID));
        if (!stageRowViewModel) {
          return;
        }

        cardModel.mainFormWithTabs!.selectedTab = approvalProcessTab;

        if (grid.selectedRow && grid.selectedRow !== stageRowViewModel) {
          grid.selectedRow.isSelected = false;
        }
        stageRowViewModel.isSelected = true;

        await grid.editRow(stageRowViewModel);
      });
    };
  }

  //#endregion
}
