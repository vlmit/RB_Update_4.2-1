import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'RegistrationStageTypeFormatter' })
export class RegistrationStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _comment = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrRegistrationStageSettingsVirtual',
    'Comment'
  );
  private static readonly _withoutTask = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrRegistrationStageSettingsVirtual',
    'WithoutTask'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.registrationDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(RegistrationStageTypeFormatter._comment),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    if (context.stageRow.tryGetBoolean(RegistrationStageTypeFormatter._withoutTask)) {
      if (builder.text.length > 0) {
        builder.text += '\n';
      }

      builder.text += '{$CardTypes_Controls_WithoutTask}';
    }

    context.displaySettings = builder.text;
  }

  //#endregion
}
