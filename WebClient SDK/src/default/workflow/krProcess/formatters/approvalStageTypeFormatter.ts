import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'ApprovalStageTypeFormatter' })
export class ApprovalStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _isAdvisory = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalSettingsVirtual',
    'Advisory'
  );
  private static readonly _isParallel = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrApprovalSettingsVirtual',
    'IsParallel'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.approvalDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    let sb = '';
    if (context.stageRow.tryGet(ApprovalStageTypeFormatter._isAdvisory)) {
      sb += '{$UI_KrApproval_Advisory}\n\r';
    }

    sb += context.stageRow.tryGet(ApprovalStageTypeFormatter._isParallel)
      ? '{$UI_KrApproval_Parallel}'
      : '{$UI_KrApproval_Sequential}';
    context.displaySettings = sb;
  }

  //#endregion
}
