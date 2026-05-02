import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'EditStageTypeFormatter' })
export class EditStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _changeState = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrEditSettingsVirtual',
    'ChangeState'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.editDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);
    context.displaySettings = context.stageRow.tryGet(EditStageTypeFormatter._changeState)
      ? '$UI_KrEdit_ChangeState'
      : '';
  }

  //#endregion
}
