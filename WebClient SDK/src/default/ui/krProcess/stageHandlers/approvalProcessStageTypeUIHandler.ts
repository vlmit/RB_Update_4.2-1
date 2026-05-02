import { FieldStorageMapChangedEventArgs, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardRow,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { IControlViewModel } from 'tessa/ui/cards';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.approvalProcessDescriptor}.
 */
@extension({ name: 'ApprovalProcessStageTypeUIHandler' })
export class ApprovalProcessStageTypeUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly _notReturnEditField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalProcessSettingsVirtual',
    'NotReturnEdit'
  );

  private static readonly _returnAfterDisapprovalField =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrApprovalProcessSettingsVirtual',
      'ReturnAfterDisapproval'
    );

  private static readonly _useProcessFromCardField =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrApprovalProcessSettingsVirtual',
      'UseProcessFromCard'
    );

  private static readonly _templateIDField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalProcessSettingsVirtual',
    'TemplateID'
  );

  private static readonly _templateNameField = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalProcessSettingsVirtual',
    'TemplateName'
  );

  private _settings: CardRow | null;
  private _returnAfterDisapprovalFlagControl?: IControlViewModel;
  private _notReturnEditFlagControl?: IControlViewModel;
  private _processTemplateControl?: IControlViewModel;
  private _useProcessFromCardControl?: IControlViewModel;

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.approvalProcessDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    const commonBlock = context.settingsForms
      .find(x => x.name === WorkflowCardTypes.KrApprovalProcessStageTypeSettingsTypeName)
      ?.blocks.find(x => x.name === 'CommonSettings');

    if (commonBlock) {
      this._returnAfterDisapprovalFlagControl = commonBlock.controls.find(
        x => x.name === 'ReturnAfterDisapproval'
      );

      this._notReturnEditFlagControl = commonBlock.controls.find(x => x.name === 'NotReturnEdit');

      this._processTemplateControl = commonBlock.controls.find(x => x.name === 'ProcessTemplate');

      this._useProcessFromCardControl = commonBlock.controls.find(
        x => x.name === 'UseProcessFromCard'
      );
    }

    this._settings = context.row;
    this._settings.fieldChanged.add(this.onSettingsFieldChanged);

    if (
      this._returnAfterDisapprovalFlagControl &&
      !this._returnAfterDisapprovalFlagControl.isReadOnly
    ) {
      this._returnAfterDisapprovalFlagControl.isReadOnly = !!this._settings.tryGetBoolean(
        ApprovalProcessStageTypeUIHandler._notReturnEditField
      );
    }

    if (this._notReturnEditFlagControl && !this._notReturnEditFlagControl.isReadOnly) {
      this._notReturnEditFlagControl.isReadOnly = !!this._settings.tryGetBoolean(
        ApprovalProcessStageTypeUIHandler._returnAfterDisapprovalField
      );
    }

    this.setRealOnlyIfEditable(
      this._returnAfterDisapprovalFlagControl,
      row => !!row.tryGetBoolean(ApprovalProcessStageTypeUIHandler._notReturnEditField)
    );

    this.setRealOnlyIfEditable(
      this._notReturnEditFlagControl,
      row => !!row.tryGetBoolean(ApprovalProcessStageTypeUIHandler._returnAfterDisapprovalField)
    );

    this.setRealOnlyIfEditable(
      this._processTemplateControl,
      row => !!row.tryGetBoolean(ApprovalProcessStageTypeUIHandler._useProcessFromCardField)
    );

    this.setRealOnlyIfEditable(
      this._useProcessFromCardControl,
      row => !!row.tryGetString(ApprovalProcessStageTypeUIHandler._templateIDField)
    );
  }

  async finalize(): Promise<void> {
    if (this._settings) {
      this._settings.fieldChanged.remove(this.onSettingsFieldChanged);
      this._settings = null;
    }
  }

  //#endregion

  //#region private methods

  private readonly onSettingsFieldChanged = async (
    e: FieldStorageMapChangedEventArgs
  ): Promise<void> => {
    if (e.fieldName === ApprovalProcessStageTypeUIHandler._notReturnEditField) {
      this.setRealOnly(!!e.fieldValue, this._returnAfterDisapprovalFlagControl, row =>
        row.set(
          ApprovalProcessStageTypeUIHandler._returnAfterDisapprovalField,
          TypedField.falseBoolean
        )
      );
    } else if (e.fieldName === ApprovalProcessStageTypeUIHandler._returnAfterDisapprovalField) {
      this.setRealOnly(!!e.fieldValue, this._notReturnEditFlagControl, row =>
        row.set(ApprovalProcessStageTypeUIHandler._notReturnEditField, TypedField.falseBoolean)
      );
    } else if (e.fieldName === ApprovalProcessStageTypeUIHandler._useProcessFromCardField) {
      this.setRealOnly(!!e.fieldValue, this._processTemplateControl, row => {
        row.set(ApprovalProcessStageTypeUIHandler._templateIDField, null);
        row.set(ApprovalProcessStageTypeUIHandler._templateNameField, null);
      });
    } else if (e.fieldName === ApprovalProcessStageTypeUIHandler._templateIDField) {
      this.setRealOnly(!!e.fieldValue, this._useProcessFromCardControl, row =>
        row.set(ApprovalProcessStageTypeUIHandler._useProcessFromCardField, TypedField.falseBoolean)
      );
    }
  };

  private setRealOnly(
    isReadOnly: boolean,
    control: IControlViewModel | undefined,
    resetFieldAction: (row: CardRow) => void
  ): void {
    if (isReadOnly && this._settings) {
      resetFieldAction(this._settings);
    }

    if (control) {
      control.isReadOnly = isReadOnly;
    }
  }

  private setRealOnlyIfEditable(
    controlViewModel: IControlViewModel | undefined,
    getIsReadOnly: (row: CardRow) => boolean
  ): void {
    if (this._settings && controlViewModel && !controlViewModel.isReadOnly) {
      controlViewModel.isReadOnly = getIsReadOnly(this._settings);
    }
  }

  //#endregion
}
