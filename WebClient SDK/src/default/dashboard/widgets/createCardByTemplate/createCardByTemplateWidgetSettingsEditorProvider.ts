import { localize } from '@tessa/application';
import { AutocompleteMode, IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { AutocompleteDataViewContext } from 'ui/autocomplete/core/autocompleteDataViewContext';
import {
  PropertyGridBuilderInstance,
  PropertyGridDataProvider,
  AutocompleteProperty,
  TextProperty
} from 'tessa/ui/propertyGrid';
import { DefaultWidgetNames } from '../widgetNames';
import { ButtonWidgetSettingsEditorProvider } from '../button/buttonWidgetSettingsEditorProvider';
import { CreateCardByTemplateWidgetSettings } from './createCardByTemplateWidgetSettings';
import { CreateCardByTemplateWidget } from './createCardByTemplateWidget';
import { CardTemplate } from './cardTemplate';
export class CreateCardByTemplateWidgetSettingsEditorProvider extends ButtonWidgetSettingsEditorProvider<CreateCardByTemplateWidgetSettings> {
  //#region ctor

  constructor(widget: CreateCardByTemplateWidget) {
    super(widget);
  }

  //#endregion

  //#region methods

  override modifyBuilder(
    builder: PropertyGridBuilderInstance,
    data: PropertyGridDataProvider
  ): PropertyGridBuilderInstance {
    const templateContext = new AutocompleteDataViewContext({
      unique: true,
      viewAlias: 'Templates',
      idColumn: 'TemplateID',
      nameColumn: 'TemplateCaption',
      parameterAlias: 'TemplateCaption',
      refSection: 'Templates',
      multiple: false
    });
    const templateConverter = new AutocompleteTemplateDataConverter();

    return builder
      .addAutocompleteProperty({
        alias: 'template',
        caption: '$Dashboard_Widget_CreateCardByTemplate_Template',
        data: data,
        dataContext: templateContext,
        dataConverter: templateConverter,
        required: true,
        onInitialized: async ({ control }) => {
          control.mode = AutocompleteMode.NonDroppable;
          control.menu.openAction.isCollapsed = true;
        }
      })
      .onGridInitialized(grid => {
        const template = grid.findProperty<AutocompleteProperty>('template');
        const caption = grid.findProperty<TextProperty>('caption');
        if (!template || !caption) {
          return;
        }

        let manualInput = false;
        let changing = false;

        caption.control.onChange.add(() => {
          if (!changing) {
            manualInput = true;
          }
        });
        template.control.onAddedRecord.add(event => {
          if (!manualInput) {
            changing = true;
            caption.value = event.record.name ?? '';
            changing = false;
          }
        });
      })
      .onGridCreated(grid => {
        grid.title = DefaultWidgetNames.CreateCardByTemplateTitle;
      });
  }

  //#endregion
}

class AutocompleteTemplateDataConverter implements IAutocompleteDataConverter<CardTemplate> {
  toRecord(value: CardTemplate): IAutocompleteRecord {
    return {
      id: value.id,
      name: localize(value.caption)
    };
  }
  fromRecord(record: IAutocompleteRecord): CardTemplate {
    const template = new CardTemplate();
    template.id = record.id as string;
    template.caption = record.name!;

    return template;
  }
}
