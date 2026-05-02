import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'TypedTaskStageTypeFormatter' })
export class TypedTaskStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _taskTypeCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrTypedTaskSettingsVirtual',
    'TaskTypeCaption'
  );
  private static readonly _taskDigest = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrTypedTaskSettingsVirtual',
    'TaskDigest'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.typedTaskDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(TypedTaskStageTypeFormatter._taskTypeCaption),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(TypedTaskStageTypeFormatter._taskDigest),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    context.displaySettings = builder.text;
  }

  //#endregion
}
