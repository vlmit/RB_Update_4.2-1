import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'AcquaintanceStageTypeFormatter' })
export class AcquaintanceStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _excludeDeputies = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrAcquaintanceSettingsVirtual',
    'ExcludeDeputies'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.acquaintanceDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    context.displayTimeLimit = '';
    context.displaySettings = context.stageRow.tryGetBoolean(
      AcquaintanceStageTypeFormatter._excludeDeputies
    )
      ? '$UI_KrAcquaintance_ExcludeDeputies'
      : '';
  }

  //#endregion
}
