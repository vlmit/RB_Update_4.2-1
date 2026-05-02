import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'HistoryManagementStageTypeFormatter' })
export class HistoryManagementStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _taskHistoryGroupTypeCaption =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrHistoryManagementStageSettingsVirtual',
      'TaskHistoryGroupTypeCaption'
    );
  private static readonly _parentTaskHistoryGroupTypeCaption =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrHistoryManagementStageSettingsVirtual',
      'ParentTaskHistoryGroupTypeCaption'
    );
  private static readonly _newIteration = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrHistoryManagementStageSettingsVirtual',
    'NewIteration'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.historyManagementDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(
        HistoryManagementStageTypeFormatter._taskHistoryGroupTypeCaption
      ),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );
    this.appendString(
      builder,
      context.stageRow.tryGetString(
        HistoryManagementStageTypeFormatter._parentTaskHistoryGroupTypeCaption
      ),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    if (context.stageRow.tryGetBoolean(HistoryManagementStageTypeFormatter._newIteration)) {
      if (builder.text.length > 0) {
        builder.text += '\n';
      }

      builder.text += '{$UI_KrHistoryManagement_NewIteration}';
    }

    context.displaySettings = builder.text;
  }

  //#endregion
}
