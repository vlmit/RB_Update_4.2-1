import { reaction } from 'mobx';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  isIFormWithBlocksViewModel
} from 'tessa/ui/cards';
import { hasNotFlag, hasFlag, Visibility } from 'tessa/platform';
import { CardTypeFlags } from 'tessa/cards/types';
import {
  GridViewModel,
  GridRowEventArgs,
  GridRowAction,
  CheckBoxViewModel
} from 'tessa/ui/cards/controls';
import { CardPermissionFlags } from 'tessa/cards';
import { extension } from '@tessa/application';
import { IRuntimeBlockViewModel } from 'tessa/ui/formEditor/types';
import { RuntimeItemWithStateViewModel } from 'tessa/ui/formEditor/controls/runtimeItemWithStateViewModel';

@extension()
export class KrHideApprovalStagePermissionsDisclaimer extends CardUIExtension {
  private _disposes: (Function | null)[] = [];

  public initialized(context: ICardUIExtensionContext): void {
    const model = context.model;

    if (hasNotFlag(model.cardType.flags, CardTypeFlags.AllowTasks)) {
      return;
    }

    const rootBlock = model.wysiwygForm?.getRootBlock();
    const approvalTab = model.mainForm
      ? model.forms.find(x => x.name === 'ApprovalProcess')
      : rootBlock?.getItem<IRuntimeBlockViewModel>('ApprovalProcess');

    if (!approvalTab) {
      return;
    }

    if (isIFormWithBlocksViewModel(approvalTab)) {
      for (const block of approvalTab.blocks) {
        if (block.name === 'ApprovalStagesBlock') {
          for (const control of block.controls) {
            if (control instanceof GridViewModel) {
              control.rowInvoked.add(this.gridRowInvoked);
            }
          }
        }
      }
      return;
    }

    const gridViewModel = approvalTab
      .getItem<RuntimeItemWithStateViewModel>('ApprovalStagesTable')
      ?.getCurrent<GridViewModel>();
    if (gridViewModel) {
      gridViewModel.rowInvoked.add(this.gridRowInvoked);
    }
  }

  public finalized(): void {
    for (const dispose of this._disposes) {
      if (dispose) {
        dispose();
      }
    }
    this._disposes.length = 0;
  }

  private gridRowInvoked = (e: GridRowEventArgs) => {
    if (e.action !== GridRowAction.Inserted && e.action !== GridRowAction.Opening) {
      return;
    }

    const label = e.rowModel!.controls.get('DisclaimerControl');
    if (!label) {
      return;
    }

    const readOnly = hasFlag(
      e.rowModel!.card.permissions.resolver.getRowPermissions('KrStagesVirtual', e.row.rowId),
      CardPermissionFlags.ProhibitModify
    );

    if (readOnly) {
      const fieldName = 'KrApprovalSettingsVirtual__IsParallel';
      if (!e.row.get(fieldName)) {
        label.controlVisibility = Visibility.Collapsed;
      }
    } else {
      const isParallelControl = e.rowModel!.controls.get('IsParallelFlag');
      if (isParallelControl) {
        this._disposes.push(
          reaction(
            () => (isParallelControl as CheckBoxViewModel).isChecked,
            () => {
              label.controlVisibility =
                label.controlVisibility === Visibility.Collapsed
                  ? Visibility.Visible
                  : Visibility.Collapsed;
            }
          )
        );
      }
    }
  };
}
