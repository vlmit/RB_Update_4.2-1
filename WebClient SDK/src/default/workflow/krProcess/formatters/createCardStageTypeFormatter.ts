import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'CreateCardStageTypeFormatter' })
export class CreateCardStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _typeCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'TypeCaption'
  );
  private static readonly _templateCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'TemplateCaption'
  );
  private static readonly _modeName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'ModeName'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.createCardDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(CreateCardStageTypeFormatter._typeCaption),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(CreateCardStageTypeFormatter._templateCaption),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(CreateCardStageTypeFormatter._modeName),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    context.displaySettings = builder.text;
  }

  //#endregion
}
