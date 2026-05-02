import { extension } from '@tessa/application';
import { Guid } from '@tessa/core';
import {
  CardRowState,
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'NotificationStageTypeFormatter' })
export class NotificationStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _excludeDeputies = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrNotificationSettingVirtual',
    'ExcludeDeputies'
  );
  private static readonly _excludeSubscribers = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrNotificationSettingVirtual',
    'ExcludeSubscribers'
  );
  private static readonly _optionalRecipients = KrStageTypeFormattingHelper.formatSectionName(
    'KrNotificationOptionalRecipientsVirtual'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.notificationDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    const excludeDeputies = !!context.stageRow.get(NotificationStageTypeFormatter._excludeDeputies);
    const excludeSubscribers = !!context.stageRow.get(
      NotificationStageTypeFormatter._excludeSubscribers
    );
    const stageRowId = context.stageRow.rowId;
    const optionalRecipients = context.card.sections
      .tryGet(NotificationStageTypeFormatter._optionalRecipients)
      ?.tryGetRows()
      ?.filter(
        x => x.state !== CardRowState.Deleted && Guid.equals(x.get('StageRowID'), stageRowId)
      )
      .map(x => x.get<string>('RoleName')!);

    context.displayTimeLimit = '';
    context.displaySettings = this.getDisplaySettings(
      excludeDeputies,
      excludeSubscribers,
      optionalRecipients
    );
  }

  private getDisplaySettings(
    excludeDeputies: boolean,
    excludeSubscribers: boolean,
    optionalRecipients?: Array<string>
  ): string {
    let settings = '';

    if (excludeDeputies) {
      settings = '{$UI_KrNotification_ExcludeDeputies}';
    }

    if (excludeSubscribers) {
      if (excludeDeputies) {
        settings += '\n';
      }
      settings += '{$UI_KrNotification_ExcludeSubscribers}';
    }

    if (optionalRecipients && optionalRecipients.length > 0) {
      if (settings.length > 0) {
        settings += '\n';
      }
      settings += '{$CardTypes_Controls_OptionalRecipients}: ';
      optionalRecipients.forEach((x, i) => {
        if (i > 0) {
          settings += ', ';
        }
        settings += x;
      });
    }

    return settings;
  }

  //#endregion
}
