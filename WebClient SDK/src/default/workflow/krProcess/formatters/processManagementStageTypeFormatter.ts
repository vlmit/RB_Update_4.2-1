import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';
import { ProcessManagementStageTypeMode } from './../processManagementStageTypeMode';

@extension({ name: 'ProcessManagementStageTypeFormatter' })
export class ProcessManagementStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _managePrimaryProcess = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'ManagePrimaryProcess'
  );
  private static readonly _modeId = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'ModeID'
  );
  private static readonly _modeName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'ModeName'
  );
  private static readonly _stageName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'StageName'
  );
  private static readonly _groupName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'StageGroupName'
  );
  private static readonly _groupRowName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'StageRowGroupName'
  );
  private static readonly _signal = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrProcessManagementStageSettingsVirtual',
    'Signal'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.processManagementDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(ProcessManagementStageTypeFormatter._modeName),
      '',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    switch (context.stageRow.tryGet(ProcessManagementStageTypeFormatter._modeId)) {
      case ProcessManagementStageTypeMode.StageMode:
        const stageName = context.stageRow.tryGetString(
          ProcessManagementStageTypeFormatter._stageName
        );

        if (stageName) {
          this.appendString(
            builder,
            stageName,
            '',
            true,
            false,
            KrStageTypeFormatter.defaultSettingMax
          );

          const groupRowName = context.stageRow.tryGet<string>(
            ProcessManagementStageTypeFormatter._groupRowName
          );

          if (groupRowName) {
            builder.text += ' (';
            this.appendString(
              builder,
              groupRowName,
              '',
              true,
              false,
              KrStageTypeFormatter.defaultSettingMax,
              false
            );
            builder.text += ')';
          }
        }
        break;
      case ProcessManagementStageTypeMode.GroupMode:
        this.appendString(
          builder,
          context.stageRow.tryGetString(ProcessManagementStageTypeFormatter._groupName),
          '',
          true,
          false,
          KrStageTypeFormatter.defaultSettingMax
        );
        break;
      case ProcessManagementStageTypeMode.SendSignalMode:
        this.appendString(
          builder,
          context.stageRow.tryGetString(ProcessManagementStageTypeFormatter._signal),
          '',
          true,
          false,
          KrStageTypeFormatter.defaultSettingMax
        );
        break;
    }

    if (context.stageRow.tryGetBoolean(ProcessManagementStageTypeFormatter._managePrimaryProcess)) {
      if (builder.text.length > 0) {
        builder.text += '\n';
      }

      builder.text += '{$CardTypes_Controls_ManagePrimaryProcess}\n';
    }

    context.displaySettings = builder.text;
  }

  //#endregion
}
