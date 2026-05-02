import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'SigningStageTypeFormatter' })
export class SigningStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _isParallel = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrSigningStageSettingsVirtual',
    'IsParallel'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.signingDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    context.displaySettings = context.stageRow.tryGetBoolean(SigningStageTypeFormatter._isParallel)
      ? '$UI_KrApproval_Parallel'
      : '$UI_KrApproval_Sequential';
  }

  //#endregion
}
