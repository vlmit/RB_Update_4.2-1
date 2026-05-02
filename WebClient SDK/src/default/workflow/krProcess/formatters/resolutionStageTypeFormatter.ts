import { extension } from '@tessa/application';
import {
  IKrStageTypeFormatterContext,
  KrStageTypeFormatter,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors
} from '@tessa/platform';

@extension({ name: 'ResolutionStageTypeFormatter' })
export class ResolutionStageTypeFormatter extends KrStageTypeFormatter {
  //#region constants and static fields

  private static readonly _kindCaption = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrResolutionSettingsVirtual',
    'KindCaption'
  );
  private static readonly _authorName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrAuthorSettingsVirtual',
    'AuthorName'
  );
  private static readonly _controllerName = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrResolutionSettingsVirtual',
    'ControllerName'
  );
  private static readonly _planned = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrResolutionSettingsVirtual',
    'Planned'
  );
  private static readonly _durationInDays = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrResolutionSettingsVirtual',
    'DurationInDays'
  );
  private static readonly _withControl = KrStageTypeFormattingHelper.formatPlainColumnName(
    'KrResolutionSettingsVirtual',
    'WithControl'
  );

  //#endregion

  //#region base overrides

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.resolutionDescriptor];
  }

  format(context: IKrStageTypeFormatterContext): void {
    super.format(context);

    const builder = { text: '' };

    this.appendString(
      builder,
      context.stageRow.tryGetString(ResolutionStageTypeFormatter._kindCaption),
      '{$CardTypes_Controls_Kind}',
      true,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    this.appendString(
      builder,
      context.stageRow.tryGetString(ResolutionStageTypeFormatter._authorName),
      '{$CardTypes_Controls_From}',
      false,
      false,
      KrStageTypeFormatter.defaultSettingMax
    );

    if (context.stageRow.tryGetBoolean(ResolutionStageTypeFormatter._withControl)) {
      this.appendString(
        builder,
        context.stageRow.tryGetString(ResolutionStageTypeFormatter._controllerName),
        '{$CardTypes_Controls_Controller}',
        false,
        true,
        KrStageTypeFormatter.defaultSettingMax
      );
    }

    context.displaySettings = builder.text;

    const planned = context.stageRow.tryGetString(ResolutionStageTypeFormatter._planned);
    const timeLimit = context.stageRow.tryGetNumber(ResolutionStageTypeFormatter._durationInDays);
    this.defaultDateFormatting(planned, timeLimit, context);
  }

  //#endregion
}
