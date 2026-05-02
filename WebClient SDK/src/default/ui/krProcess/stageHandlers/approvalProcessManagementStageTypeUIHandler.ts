import { FieldStorageMapChangedEventArgs, Guid, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardRow,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { IBlockViewModel } from 'tessa/ui/cards';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { Visibility } from 'tessa/platform';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.approvalProcessManagementDescriptor}.
 */
@extension({ name: 'ApprovalProcessManagementStageTypeUIHandler' })
export class ApprovalProcessManagementStageTypeUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly _stateIdField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalProcessManagementSettingsVirtual',
    'StateID'
  );

  private static readonly _stateNameField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalProcessManagementSettingsVirtual',
    'StateName'
  );

  private static readonly _showRevokeButtonField =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrApprovalProcessManagementSettingsVirtual',
      'ShowRevokeButton'
    );

  private static readonly _updateHistoryGroupField =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrApprovalProcessManagementSettingsVirtual',
      'UpdateHistoryGroup'
    );

  private static readonly _controlTypeIdField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalProcessManagementSettingsVirtual',
    'ControlTypeID'
  );

  private static readonly _changeStateControlTypeId = '3c4d94ba-d32d-4416-a09c-5dbc6c0bcf04';

  private _settings: CardRow | null;
  private _changeStateBlock?: IBlockViewModel;

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.approvalProcessManagementDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    this._changeStateBlock = context.settingsForms
      .find(x => x.name === WorkflowCardTypes.KrApprovalProcessManagementStageTypeSettingsTypeName)
      ?.blocks.find(x => x.name === 'ChangeStateBlock');

    this._settings = context.row;
    this._settings.fieldChanged.add(this.onSettingsFieldChanged);

    this.updateBlockVisibility(
      this._settings.getString(ApprovalProcessManagementStageTypeUIHandler._controlTypeIdField),
      true
    );
  }

  async finalize(): Promise<void> {
    if (this._settings) {
      this._settings.fieldChanged.remove(this.onSettingsFieldChanged);
      this._settings = null;
    }

    this._changeStateBlock = undefined;
  }

  //#endregion

  //#region private methods

  private readonly onSettingsFieldChanged = async (
    e: FieldStorageMapChangedEventArgs
  ): Promise<void> => {
    if (e.fieldName === ApprovalProcessManagementStageTypeUIHandler._controlTypeIdField) {
      this.updateBlockVisibility(e.fieldValue as string);
    }
  };

  private updateBlockVisibility(controlTypeId: string | null, isInit = false): void {
    if (!this._changeStateBlock || !this._settings) {
      return;
    }

    const blockVisible = Guid.equals(
      controlTypeId,
      ApprovalProcessManagementStageTypeUIHandler._changeStateControlTypeId
    );

    if (blockVisible) {
      this._changeStateBlock.blockVisibility = Visibility.Visible;
    } else {
      this._changeStateBlock.blockVisibility = Visibility.Collapsed;
      if (!isInit) {
        this._settings.set(ApprovalProcessManagementStageTypeUIHandler._stateIdField, null);
        this._settings.set(ApprovalProcessManagementStageTypeUIHandler._stateNameField, null);
        this._settings.set(
          ApprovalProcessManagementStageTypeUIHandler._showRevokeButtonField,
          TypedField.falseBoolean
        );
        this._settings.set(
          ApprovalProcessManagementStageTypeUIHandler._updateHistoryGroupField,
          TypedField.falseBoolean
        );
      }
    }
  }

  //#endregion
}
