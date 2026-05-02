import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'DialogsStageTypeFormatter' })
export class DialogsStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _kindCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrTaskKindSettingsVirtual',
    'KindCaption'
  );
  private static readonly _taskDigest = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrDialogStageTypeSettingsVirtual',
    'TaskDigest'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.dialogDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(DialogsStageTypeFormatter._kindCaption),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(DialogsStageTypeFormatter._taskDigest),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    context.displaySettings = builder.text;
  }

  //#endregion
}
