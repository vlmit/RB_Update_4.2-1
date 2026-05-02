import {
  FieldStorageMap,
  FieldStorageMapChangedEventArgs,
  ValidationResult,
  ValidationResultType
} from '@tessa/core';
import { extension } from '@tessa/application';
import {
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.createCardDescriptor}.
 */
@extension({ name: 'CreateCardUIHandler' })
export class CreateCardUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly _templateId = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'TemplateID'
  );

  private static readonly _templateCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'TemplateCaption'
  );

  private static readonly _typeId = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'TypeID'
  );

  private static readonly _typeCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'TypeCaption'
  );

  private static readonly _modeId = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrCreateCardStageSettingsVirtual',
    'ModeID'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.createCardDescriptor];
  }

  async validate(context: IKrStageTypeUIHandlerContext): Promise<void> {
    const template = context.row.tryGet(CreateCardUIHandler._templateId);
    const type = context.row.tryGet(CreateCardUIHandler._typeId);

    if (!template && !type) {
      context.validationResult.add(
        ValidationResult.fromText(
          '$KrStages_CreateCard_TemplateAndTypeNotSpecified',
          ValidationResultType.Error
        )
      );
    } else if (template && type) {
      context.validationResult.add(
        ValidationResult.fromText(
          '$KrStages_CreateCard_TemplateAndTypeSelected',
          ValidationResultType.Error
        )
      );
    }

    if (context.row.tryGet(CreateCardUIHandler._modeId) == null) {
      context.validationResult.add(
        ValidationResult.fromText('$KrStages_CreateCard_ModeRequired', ValidationResultType.Error)
      );
    }
  }

  public async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    context.row.fieldChanged.add(CreateCardUIHandler.onSettingsFieldChanged);
  }

  public async finalize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    context.row.fieldChanged.remove(CreateCardUIHandler.onSettingsFieldChanged);
  }

  //#endregion

  //#region private methods

  private static onSettingsFieldChanged(
    e: FieldStorageMapChangedEventArgs,
    s: FieldStorageMap
  ): void {
    if (e.fieldName === CreateCardUIHandler._typeId) {
      if (e.fieldValue) {
        s.set(CreateCardUIHandler._templateId, null);
        s.set(CreateCardUIHandler._templateCaption, null);
      }
    } else if (e.fieldName === CreateCardUIHandler._templateId) {
      if (e.fieldValue) {
        s.set(CreateCardUIHandler._typeId, null);
        s.set(CreateCardUIHandler._typeCaption, null);
      }
    }
  }

  //#endregion
}
