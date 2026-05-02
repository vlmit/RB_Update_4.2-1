import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'ChangeStateStageTypeFormatter' })
export class ChangeStateStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _stateName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrChangeStateSettingsVirtual',
    'StateName'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.changesStateDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };
    this.appendString(
      builder,
      context.stageRow.tryGetString(ChangeStateStageTypeFormatter._stateName),
      '$UI_KrChangeState_State',
      true
    );
    context.displaySettings = builder.text;
  }

  //#endregion
}
