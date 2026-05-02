import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'AddFileFromTemplateStageTypeFormatter' })
export class AddFileFromTemplateStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _name = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrAddFromTemplateSettingsVirtual',
    'Name'
  );
  private static readonly _fileTemplateName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrAddFromTemplateSettingsVirtual',
    'FileTemplateName'
  );
  private static readonly _fileCategoryName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrAddFromTemplateSettingsVirtual',
    'FileCategoryName'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.addFromTemplateDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(AddFileFromTemplateStageTypeFormatter._fileTemplateName),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(AddFileFromTemplateStageTypeFormatter._name),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(AddFileFromTemplateStageTypeFormatter._fileCategoryName),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    context.displaySettings = builder.text;
    context.displayParticipants = '';
    context.displayTimeLimit = '';
  }

  //#endregion
}
