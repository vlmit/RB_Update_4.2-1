import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';
import { ForkStageTypeFormatterBase } from './forkStageTypeFormatterBase';

@extension({ name: 'ForkManagementStageTypeFormatter' })
export class ForkManagementStageTypeFormatter extends ForkStageTypeFormatterBase {
  //#region constants and static fields

  private static readonly _modeName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrForkManagementSettingsVirtual',
    'ModeName'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.forkManagementDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(ForkManagementStageTypeFormatter._modeName),
      '',
      true
    );

    this.appendSecondaryProcessesNames(builder, context);

    context.displaySettings = builder.text;
  }

  //#endregion
}
