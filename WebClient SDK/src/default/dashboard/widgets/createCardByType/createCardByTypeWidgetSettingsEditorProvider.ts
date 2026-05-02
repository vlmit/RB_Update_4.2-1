import { AutocompleteMode } from 'ui/autocomplete';
import {
  PropertyGridBuilderInstance,
  PropertyGridDataProvider,
  AutocompleteProperty,
  TextProperty
} from 'tessa/ui/propertyGrid';
import { DefaultWidgetNames } from '../widgetNames';
import { ButtonWidgetSettingsEditorProvider } from '../button/buttonWidgetSettingsEditorProvider';
import { AutocompleteDataCardTypeSelectorContext } from './autocompleteDataCardTypeSelectorContext';
import { AutocompleteCardTypeDataConverter } from './autocompleteCardTypeDataConverter';
import { CreateCardByTypeWidgetSettings } from './createCardByTypeWidgetSettings';
import { ICardTypeSelectorTypesProvider } from './cardTypeSelector/cardTypeSelectorTypes';
import { CreateCardByTypeWidget } from './createCardByTypeWidget';

export class CreateCardByTypeWidgetSettingsEditorProvider extends ButtonWidgetSettingsEditorProvider<CreateCardByTypeWidgetSettings> {
  //#region fields

  private readonly _typesProvider: ICardTypeSelectorTypesProvider;

  //#endregion

  //#region ctor

  constructor(widget: CreateCardByTypeWidget, typesProvider: ICardTypeSelectorTypesProvider) {
    super(widget);

    this._typesProvider = typesProvider;
  }

  //#endregion

  //#region methods

  override modifyBuilder(
    builder: PropertyGridBuilderInstance,
    data: PropertyGridDataProvider
  ): PropertyGridBuilderInstance {
    const context = new AutocompleteDataCardTypeSelectorContext(this._typesProvider);
    const converter = new AutocompleteCardTypeDataConverter();

    return builder
      .addAutocompleteProperty({
        alias: 'cardType',
        caption: '$Dashboard_Widget_CreateCardByType_Type',
        data: data,
        dataContext: context,
        dataConverter: converter,
        required: true,
        onInitialized: async ({ control }) => {
          control.mode = AutocompleteMode.NonDroppable;
        }
      })
      .onGridInitialized(grid => {
        const type = grid.findProperty<AutocompleteProperty>('cardType');
        const caption = grid.findProperty<TextProperty>('caption');
        if (!type || !caption) {
          return;
        }

        let manualInput = false;
        let changing = false;

        caption.control.onChange.add(() => {
          if (!changing) {
            manualInput = true;
          }
        });
        type.control.onAddedRecord.add(event => {
          if (!manualInput) {
            changing = true;
            caption.value = event.record.name ?? '';
            changing = false;
          }
        });
      })
      .onGridCreated(grid => {
        grid.title = DefaultWidgetNames.CreateCardByTypeTitle;
      });
  }

  //#endregion
}
