import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { ValidationResult, ValidationResultType } from 'tessa/platform/validation';
import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.addFromTemplateDescriptor}.
 */
@extension({ name: 'AddFromTemplateUIHandler' })
export class AddFromTemplateUIHandler extends KrStageTypeUIHandler {
  //#endregion fields

  private static readonly _fileTemplateIdFieldName =
    KrStageTypeFormattingHelper.formatPlainColumnName(
      'KrAddFromTemplateSettingsVirtual',
      'FileTemplateID'
    );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.addFromTemplateDescriptor];
  }

  async validate(context: IKrStageTypeUIHandlerContext): Promise<void> {
    if (!context.row.tryGet(AddFromTemplateUIHandler._fileTemplateIdFieldName)) {
      context.validationResult.add(
        ValidationResult.fromText(
          '$KrStages_AddFromTemplate_TemplateIsRequiredWarning',
          ValidationResultType.Warning
        )
      );
    }
  }

  //#endregion
}
