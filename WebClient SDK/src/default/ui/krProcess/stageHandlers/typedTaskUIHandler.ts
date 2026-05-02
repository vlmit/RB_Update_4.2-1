import { ValidationResult, ValidationResultType } from '@tessa/core';
import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.typedTaskDescriptor}.
 */
@extension({ name: 'TypedTaskUIHandler' })
export class TypedTaskUIHandler extends KrStageTypeUIHandler {
  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.typedTaskDescriptor];
  }

  async validate(context: IKrStageTypeUIHandlerContext): Promise<void> {
    if (
      !context.row.tryGet(
        KrStageTypeFormattingHelper.formatPlainColumnName(
          'KrTypedTaskSettingsVirtual',
          'TaskTypeID'
        )
      )
    ) {
      context.validationResult.add(
        ValidationResult.fromText('$KrStages_TypedTask_TaskType', ValidationResultType.Error)
      );
    }
  }

  //#endregion
}
