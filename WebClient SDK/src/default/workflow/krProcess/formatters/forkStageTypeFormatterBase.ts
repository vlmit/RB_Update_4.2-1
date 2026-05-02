import { Guid } from '@tessa/core';
import {
  CardRowState,
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper
} from '@tessa/platform';

export class ForkStageTypeFormatterBase extends KrStageTypeFormatter {
  //#region constants and static fields

  protected static readonly _secondaryProcessName = 'SecondaryProcessName';
  protected static readonly _krForkSecondaryProcessesSettingsVirtualSynthetic =
    KrStageTypeFormattingHelper.formatSectionName('KrForkSecondaryProcessesSettingsVirtual');
  protected static readonly _stageRowIDReferenceToOwner = 'StageRowID';

  //#endregion

  //#region protected methods

  protected appendSecondaryProcessesNames(
    builder: { text: string },
    context: IKrStageTypeFormatterContext
  ): void {
    const settingsRows = context.card.sections
      .tryGet(ForkStageTypeFormatterBase._krForkSecondaryProcessesSettingsVirtualSynthetic)
      ?.tryGetRows();

    if (settingsRows && settingsRows.length > 0) {
      const stageRowId = context.stageRow.rowId;

      for (const settingsRow of settingsRows) {
        const t = settingsRow.tryGetString(ForkStageTypeFormatterBase._stageRowIDReferenceToOwner);
        if (settingsRow.state === CardRowState.Deleted || !Guid.equals(t, stageRowId)) {
          continue;
        }

        this.appendString(
          builder,
          settingsRow.tryGetString(ForkStageTypeFormatterBase._secondaryProcessName),
          '',
          true,
          false,
          KrStageTypeFormatter.defaultSettingMax
        );
      }
    }
  }

  //#endregion
}
