import {
  FieldStorageMapChangedEventArgs,
  ValidationResult,
  ValidationResultType
} from '@tessa/core';
import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { Visibility } from 'tessa/platform';
import { IControlViewModel } from 'tessa/ui/cards';
import { ProcessManagementStageTypeMode } from './../../../workflow/krProcess/processManagementStageTypeMode';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.processManagementDescriptor}.
 */
@extension({ name: 'ProcessManagementUIHandler' })
export class ProcessManagementUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly _modeId = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'ModeID'
  );

  private _stageControl: IControlViewModel | null;
  private _groupControl: IControlViewModel | null;
  private _signalControl: IControlViewModel | null;

  //#endregion

  //#region base overrides

  public descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.processManagementDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    const block = context.settingsForms
      .find(i => i.name === WorkflowCardTypes.KrProcessManagementStageTypeSettingsTypeName)
      ?.blocks.find(i => i.name === 'MainInfo');

    if (!block) {
      return;
    }

    this._stageControl = block.controls.find(i => i.name === 'StageRow')!;
    this._groupControl = block.controls.find(i => i.name === 'StageGroup')!;
    this._signalControl = block.controls.find(i => i.name === 'Signal')!;

    if (!this._stageControl || !this._groupControl || !this._signalControl) {
      return;
    }

    this.updateVisibility(context.row.tryGet<number>(ProcessManagementUIHandler._modeId));

    context.row.fieldChanged.add(this.modeChanged);
  }

  async finalize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    this._stageControl = null;
    this._groupControl = null;
    this._signalControl = null;

    context.row.fieldChanged.remove(this.modeChanged);
  }

  async validate(context: IKrStageTypeUIHandlerContext): Promise<void> {
    if (context.row.tryGet(ProcessManagementUIHandler._modeId) == null) {
      context.validationResult.add(
        ValidationResult.fromText(
          '$KrStages_ProcessManagement_ModeNotSpecified',
          ValidationResultType.Error
        )
      );
    }
  }

  //#endregion

  //#region private methods

  private readonly modeChanged = (args: FieldStorageMapChangedEventArgs): void => {
    if (args.fieldName !== ProcessManagementUIHandler._modeId) {
      return;
    }

    this.updateVisibility(args.value as number);
  };

  private updateVisibility(field: number | null | undefined): void {
    this._stageControl!.controlVisibility = Visibility.Collapsed;
    this._groupControl!.controlVisibility = Visibility.Collapsed;
    this._signalControl!.controlVisibility = Visibility.Collapsed;

    switch (field) {
      case ProcessManagementStageTypeMode.StageMode:
        this._stageControl!.controlVisibility = Visibility.Visible;
        break;
      case ProcessManagementStageTypeMode.GroupMode:
        this._groupControl!.controlVisibility = Visibility.Visible;
        break;
      case ProcessManagementStageTypeMode.SendSignalMode:
        this._signalControl!.controlVisibility = Visibility.Visible;
        break;
    }
  }

  //#endregion
}
