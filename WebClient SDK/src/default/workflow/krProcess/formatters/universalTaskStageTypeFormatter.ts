import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'UniversalTaskStageTypeFormatter' })
export class UniversalTaskStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _digest = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrUniversalTaskSettingsVirtual',
    'Digest'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.universalTaskDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(UniversalTaskStageTypeFormatter._digest),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    context.displaySettings = builder.text;
  }

  //#endregion
}
