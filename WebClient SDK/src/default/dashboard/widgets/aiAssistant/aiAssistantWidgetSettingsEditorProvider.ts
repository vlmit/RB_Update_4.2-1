import {
  IPropertyGrid,
  PropertyGridBuilder,
  PropertyGridBuilderInstance,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import {
  DashboardWidgetSettingsHelper,
  IDashboardWidgetSettingsEditorProvider
} from 'tessa/ui/dashboard';
import { AutocompleteMode } from 'ui/autocomplete';
import {
  AiAssistantToolAutocompleteDataContext,
  AiAssistantToolAutocompleteItem,
  AiAssistantToolAutocompleteRecord,
  AiToolInfo
} from 'tessa/ui/ai';
import { AutocompleteDataConverter } from 'ui/autocomplete/core/autocompleteDataConverter';
import { DefaultWidgetNames } from '../widgetNames';
import { AiAssistantWidget } from './aiAssistantWidget';

/** Провайдер настроек виджета {@link AiAssistantWidget}. */
export class AiAssistantWidgetSettingsEditorProvider implements IDashboardWidgetSettingsEditorProvider {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link AiAssistantWidgetSettingsEditorProvider}.
   * @param _widget Виджет "ИИ ассистент".
   */
  constructor(private readonly _widget: AiAssistantWidget) {}

  //#endregion

  //#region properties

  get hasSettingsEditor(): boolean {
    return true;
  }

  //#endregion

  //#region methods

  async createEditor(modal: boolean): Promise<IPropertyGrid | null> {
    const provider = DashboardWidgetSettingsHelper.createSettingsProvider(this._widget, modal);

    const builder = PropertyGridBuilder.create(provider);
    DashboardWidgetSettingsHelper.addBaseContainerSettings(provider, builder);
    this.addSpecialSettings(provider, builder);

    return builder.build();
  }

  //#endregion

  //#region private methods

  private addSpecialSettings(
    data: PropertyGridDataProvider,
    builder: PropertyGridBuilderInstance
  ): PropertyGridBuilderInstance {
    return builder
      .startGroup('$Dashboard_Widget_SpecialSettings')
      .addColorProperty({
        data,
        alias: 'chatBackgroundColor',
        caption: '$Dashboard_Widget_AiAssistant_Settings_ChatBackgroundColor'
      })
      .addColorProperty({
        data,
        alias: 'incomingMessageColor',
        caption: '$Dashboard_Widget_AiAssistant_Settings_IncomingMessageColor'
      })
      .addColorProperty({
        data,
        alias: 'outgoingMessageColor',
        caption: '$Dashboard_Widget_AiAssistant_Settings_OutgoingMessageColor'
      })
      .addBooleanProperty({
        data,
        alias: 'outgoingMessageMarkdown',
        visibility: false,
        caption: '$Dashboard_Widget_AiAssistant_Settings_OutgoingMessageMarkdown',
        tooltip: '$Dashboard_Widget_AiAssistant_Settings_OutgoingMessageMarkdown_Tooltip',
        controlCaption: 'override',
        controlTooltip: 'inherit'
      })
      .addBooleanProperty({
        data,
        alias: 'messageShowHeader',
        caption: '$Dashboard_Widget_AiAssistant_Settings_MessageShowHeader',
        controlCaption: 'override'
      })
      .addBooleanProperty({
        data,
        alias: 'messageShowFooter',
        caption: '$Dashboard_Widget_AiAssistant_Settings_MessageShowFooter',
        controlCaption: 'override'
      })
      .addNumericProperty({
        data,
        alias: 'incomingMessageTypingSpeed',
        caption: '$Dashboard_Widget_AiAssistant_Settings_IncomingMessageTypingSpeed',
        tooltip: '$Dashboard_Widget_AiAssistant_Settings_IncomingMessageTypingSpeed_Tooltip',
        controlType: 'Integer',
        minValue: 0,
        maxValue: 100
      })
      .addAutocompleteProperty({
        data,
        alias: 'predefinedTool',
        caption: '$Dashboard_Widget_AiAssistant_Settings_PredefinedTool',
        tooltip: '$Dashboard_Widget_AiAssistant_Settings_PredefinedTool_Tooltip',
        dataContext: new AiAssistantToolAutocompleteDataContext(
          this._widget.aiAssistant,
          () => data.getEntry<AiToolInfo | null>('predefinedTool').getPropertyValue()?.id ?? null
        ),
        dataConverter: new AutocompleteDataConverter(false),
        onInitialized: async ({ control }) => {
          control.mode = AutocompleteMode.NonDroppable;
          control.menu.canOpenRecord = false;
          control.dropdown.itemComponent = AiAssistantToolAutocompleteItem;
          control.records.forEach(r => (r.component = AiAssistantToolAutocompleteRecord));
          control.records.collectionChanged.add(({ added }) =>
            added.forEach(r => (r.component = AiAssistantToolAutocompleteRecord))
          );
        }
      })
      .onGridCreated(grid => {
        grid.title = DefaultWidgetNames.AiAssistantTitle;
        grid.leftCaption = false;
      });
  }

  //#endregion
}
