import moment from 'moment';
import { ShadowPropsHelper, UIHost } from '@tessa/ui';
import { AsyncLazy, FieldType, Guid } from '@tessa/core';
import { PlatformHelper, DateTimeTypeFormat } from '@tessa/platform';
import {
  SelectorProperty,
  DateTimeProperty,
  AutocompleteProperty,
  PropertyGridBuilder,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import {
  IAutocompleteRecord,
  AutocompleteDataConverter,
  AutocompleteDataViewContext
} from 'ui/autocomplete';
import { GridHelper } from 'ui/grid';
import { SyntaxHighlighting } from 'ui/uiEnums';
import { UIButton } from 'ui/common/uiButton';
import { Button } from 'ui/button/buttonViewModel';
import { ForegroundPaletteAlias } from 'ui/colorPicker';
import { AdvancedCardDialogManager } from 'tessa/ui/cards';
import { IconSelector, IconSelector$ } from 'tessa/ui/iconSelector';
import { PropertyGridArticle } from './propertyGridArticle';
import { PropertyGridDemoView } from './propertyGridDemoForm';

export class PropertyGridPropertiesArticle extends PropertyGridArticle {
  //#region constructors

  constructor(
    @IconSelector$({ lazy: true }) private readonly _iconSelector: AsyncLazy<IconSelector>
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  protected override readonly name = 'Properties';
  protected override readonly description =
    `This article shows how to work with any kind of default properties.` +
    '\n\n' +
    '**To implement custom properties, please refer to example #36 in the developer documentation.**';
  protected override readonly keywords = new Set<string>();

  override async initialize(): Promise<void> {
    this.keywords.add('boolean');
    this.booleanProperty();

    this.keywords.add('button');
    this.buttonProperty();

    this.keywords.add('label').add('message').add('hyperlink');
    this.labelProperty();

    this.keywords.add('slider');
    this.sliderProperty();

    this.keywords.add('text');
    this.textProperty();

    this.keywords.add('numeric').add('integer').add('float').add('decimal');
    this.numericProperty();

    this.keywords.add('icon');
    this.iconProperty();

    this.keywords.add('color');
    this.colorProperty();

    this.keywords.add('code');
    this.codeProperty();

    this.keywords.add('date').add('interval').add('time').add('datetime');
    this.dateTimeProperty();

    this.keywords.add('autocomplete').add('reference');
    this.autocompleteProperty();

    this.keywords.add('selector');
    this.selectorProperty();

    this.keywords.add('table').add('grid');
    this.tableProperty();
  }

  //#endregion

  //#region private methods

  private booleanProperty(): void {
    this.addBlock({
      caption: 'Boolean',
      description: `This article shows how to work with boolean property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ boolean: true }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addBooleanProperty({
            data: dataProvider,
            alias: 'boolean',
            caption: 'Boolean property',
            leftCaption: false,
            controlType: 'switch',
            controlCaption: 'override',
            controlTooltip: 'inherit'
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createBooleanProperty({
  data: new PropertyGridDataProvider({ boolean: true }, true),
  alias: 'boolean',
  caption: 'Boolean property',
  captionVisibility: false,
  leftCaption: false,
  controlType: 'switch',
  controlCaption: 'override',
  controlTooltip: 'inherit'
});
~~~`
    });
  }

  private buttonProperty(): void {
    this.addBlock({
      caption: 'Button',
      description: `This article shows how to work with button property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider();
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addButtonProperty({
            data: dataProvider,
            alias: 'buttons',
            caption: 'Button property',
            buttons: [
              Button.create({ name: 'undo', icon: 'm-undo', type: 'toolbar', theme: 'control' }),
              UIButton.create({ name: 'redo', icon: 'm-redo', type: 'toolbar', theme: 'control' })
            ],
            buttonsDirection: 'row'
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createButtonProperty({
  data: new PropertyGridDataProvider(),
  alias: 'buttons',
  caption: 'Button property',
  buttons: [
    Button.create({ name: 'undo', icon: 'm-undo', type: 'toolbar', theme: 'control' }),
    UIButton.create({ name: 'redo', icon: 'm-redo', type: 'toolbar', theme: 'control' })
  ],
  buttonsDirection: 'row'
});
~~~`
    });
  }

  private labelProperty(): void {
    this.addBlock({
      caption: 'Label',
      description: `This article shows how to work with label property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ label: 'Text inside label' }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addLabelProperty({
            data: dataProvider,
            alias: 'label',
            caption: 'Label property',
            type: 'hyperlink',
            theme: 'warning',
            href: 'https://tessa.ru'
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createLabelProperty({
  data: new PropertyGridDataProvider({ label: 'Text inside label' }, true),
  alias: 'label',
  caption: 'Label property',
  type: 'hyperlink',
  theme: 'warning',
  href: 'https://tessa.ru'
});
~~~`
    });
  }

  private sliderProperty(): void {
    this.addBlock({
      caption: 'Slider',
      description: `This article shows how to work with slider property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ slider: 50 }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addSliderProperty({
            data: dataProvider,
            alias: 'slider',
            caption: 'Slider property',
            step: 5,
            min: 10,
            max: 90,
            roundValues: 'round',
            displayValues: true,
            sliderSettings: {
              trackColors: [{ color: 'red' }, { color: 'blue', endPointColor: 1 }],
              trackDirection: 'to left',
              trackBackgroundColor: 'yellow',
              activeThumbColor: 'green',
              hoverThumbColor: 'blue',
              thumbColor: 'black'
            }
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createSliderProperty({
  data: new PropertyGridDataProvider({ slider: 50 }, true),
  alias: 'slider',
  caption: 'Slider property',
  step: 5,
  min: 10,
  max: 90,
  roundValues: 'round',
  displayValues: true,
  sliderSettings: {
    trackColors: [{ color: 'red' }, { color: 'blue', endPointColor: 1 }],
    trackDirection: 'to left',
    trackBackgroundColor: 'yellow',
    activeThumbColor: 'green',
    hoverThumbColor: 'blue',
    thumbColor: 'black'
  }
});
~~~`
    });
  }

  private textProperty(): void {
    this.addBlock({
      caption: 'Text',
      description: `This article shows how to work with text property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ text: 'Some text' }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addTextProperty({
            data: dataProvider,
            alias: 'text',
            caption: 'Text property',
            nullable: true,
            shouldLocalize: true,
            minLength: 2,
            maxLength: 128,
            minRows: 2,
            maxRows: 5,
            spellCheck: true,
            emojiButtonEnabled: true
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createTextProperty({
  data: new PropertyGridDataProvider({ text: 'Some text' }, true),
  alias: 'text',
  caption: 'Text property',
  nullable: true,
  shouldLocalize: true,
  minLength: 2,
  maxLength: 128,
  minRows: 2,
  maxRows: 5,
  spellCheck: true,
  emojiButtonEnabled: true
  // maskOptions
});
~~~`
    });
  }

  private numericProperty(): void {
    this.addBlock({
      caption: 'Numeric',
      description: `This article shows how to work with numeric property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ numeric: 3.14 }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addNumericProperty({
            data: dataProvider,
            alias: 'numeric',
            caption: 'Numeric property',
            controlType: 'Decimal',
            minValue: -1024,
            maxValue: 1024,
            separateGroups: true,
            groupSeparator: ' ',
            decimalSeparator: ',',
            digitsAfterSeparator: 2
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createNumericProperty({
  data: new PropertyGridDataProvider({ numeric: 3.14 }, true),
  alias: 'numeric',
  caption: 'Numeric property',
  controlType: 'Decimal',
  minValue: -1024,
  maxValue: 1024,
  separateGroups: true,
  groupSeparator: ' ',
  decimalSeparator: ',',
  digitsAfterSeparator: 2
});
~~~`
    });
  }

  private iconProperty(): void {
    this.addBlock({
      caption: 'Icon',
      description: `This article shows how to work with icon property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ icon: 'm-like' }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addIconProperty({
            data: dataProvider,
            alias: 'icon',
            caption: 'Icon property',
            iconSelector: await this._iconSelector.getValue()
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createIconProperty({
  data: new PropertyGridDataProvider({ icon: 'm-like' }, true),
  data: dataProvider,
  alias: 'icon',
  caption: 'Icon property',
  iconSelector: await di.getAsync(IconSelector$)
});
~~~`
    });
  }

  private colorProperty(): void {
    this.addBlock({
      caption: 'Color',
      description: `This article shows how to work with color property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ color: '#80000000' }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addColorProperty({
            data: dataProvider,
            alias: 'color',
            caption: 'Color property',
            palette: ForegroundPaletteAlias
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createColorProperty({
  data: new PropertyGridDataProvider({ color: '#80000000' }, true),
  data: dataProvider,
  alias: 'color',
  caption: 'Color property',
  palette: ForegroundPaletteAlias
});
~~~`
    });
  }

  private codeProperty(): void {
    this.addBlock({
      caption: 'Code',
      description: `This article shows how to work with code property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ code: '{ "key": "value" }' }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addCodeProperty({
            data: dataProvider,
            alias: 'code',
            caption: 'Code property',
            minRows: 3,
            maxRows: 10,
            showLineNumbers: true,
            stretchVertically: false,
            highlightingMode: SyntaxHighlighting.JSON
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createCodeProperty({
  data: new PropertyGridDataProvider({ code: '{ "key": "value" }' }, true),
  alias: 'code',
  caption: 'Code property',
  minRows: 3,
  maxRows: 10,
  showLineNumbers: true,
  stretchVertically: false,
  highlightingMode: SyntaxHighlighting.JSON
});
~~~`
    });
  }

  private dateTimeProperty(): void {
    this.addBlock({
      caption: 'DateTime',
      description: `This article shows how to work with date and time property.`,
      props: async () => {
        const data = {
          dateWithInterval: null,
          dateWithoutInterval: null,
          dateTimeIgnoringTimezone: null,
          dateTimeNotIgnoringTimezone: null,
          intervalIgnoringTimezone: null,
          intervalNotIgnoringTimezone: null,
          timeIgnoringTimezone: null,
          timeNotIgnoringTimezone: null
        };
        const dataProvider = new PropertyGridDataProvider(data, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .startGroup({ caption: 'Date', collapsed: true })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'dateWithInterval',
            caption: 'Date with interval',
            tooltip: 'Interval from 01.01.2000 to 31.12.2050',
            minDate: moment('01.01.2000', 'DD.MM.YYYY'),
            maxDate: moment('31.12.2050', 'DD.MM.YYYY'),
            formatType: DateTimeTypeFormat.Date
          })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'dateWithoutInterval',
            caption: 'Date without interval',
            tooltip: 'No interval',
            formatType: DateTimeTypeFormat.Date
          })
          .startGroup({ caption: 'Date and time', collapsed: true })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'dateTimeIgnoringTimezone',
            caption: 'Date and time without timezone',
            tooltip: 'Ignoring user timezone',
            formatType: DateTimeTypeFormat.DateTime,
            ignoreTimezone: true
          })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'dateTimeNotIgnoringTimezone',
            caption: 'Date and time with timezone',
            tooltip: 'Not ignoring user timezone',
            formatType: DateTimeTypeFormat.DateTime,
            ignoreTimezone: false
          })
          .startGroup({ caption: 'Interval', collapsed: true })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'intervalIgnoringTimezone',
            caption: 'Interval without timezone',
            tooltip: 'Ignoring user timezone',
            formatType: DateTimeTypeFormat.Interval,
            ignoreTimezone: true
          })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'intervalNotIgnoringTimezone',
            caption: 'Interval with timezone',
            tooltip: 'Not ignoring user timezone',
            formatType: DateTimeTypeFormat.Interval,
            ignoreTimezone: false
          })
          .startGroup({ caption: 'Time', collapsed: true })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'timeIgnoringTimezone',
            caption: 'Time without timezone',
            tooltip: 'Ignoring user timezone',
            formatType: DateTimeTypeFormat.Time,
            ignoreTimezone: true
          })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'timeNotIgnoringTimezone',
            caption: 'Time with timezone',
            tooltip: 'Not ignoring user timezone',
            formatType: DateTimeTypeFormat.Time,
            ignoreTimezone: false
          })
          .onGridCreated(grid => {
            grid.descriptionVisibility = true;
            grid.getProperties().forEach((property: DateTimeProperty) => {
              const sourceTooltip = property.tooltip.text;
              ShadowPropsHelper.add(property.tooltip, 'text', () => {
                const source = dataProvider.getEntry<string>(property.alias).getPropertyValue();
                return `${sourceTooltip}\n${source || 'Value is not specified'}`;
              });
            });
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~js
PropertyGridBuilder.createDateTimeProperty({
  data: new PropertyGridDataProvider({ date: '2020-01-01T01:01:01Z' }, true),
  alias: 'dateTime',
  caption: 'Date and time property',
  formatType: DateTimeTypeFormat.DateTime,
  ignoreTimezone: false
});
~~~`
    });
  }

  private autocompleteProperty(): void {
    this.addBlock({
      caption: 'Autocomplete',
      description: `This article shows how to work with autocomplete property.`,
      props: async () => {
        const systemUser = { id: PlatformHelper.systemUserId, name: PlatformHelper.systemUserName };
        const dataProvider = new PropertyGridDataProvider({ autocomplete: [systemUser] }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addAutocompleteProperty({
            data: dataProvider,
            alias: 'autocomplete',
            caption: 'Autocomplete property',
            tooltipVisibility: true,
            dataConverter: new AutocompleteDataConverter(),
            dataContext: new AutocompleteDataViewContext({
              unique: true,
              multiple: true,
              viewAlias: 'Users',
              idColumn: 'UserID',
              nameColumn: 'UserName',
              parameterAlias: 'Name'
            }),
            onInitialized: async ({ control }) => {
              control.maxVisibleRecords = 10;
              control.menu.openAction.action = async () => {
                const cardId = control.selectedRecord?.model.id as string;
                if (Guid.isValid(cardId)) {
                  if (!control.menu.openAction.autoclose) {
                    control.menu.close();
                  }
                  await UIHost.showLoadingOverlay(
                    async () => await AdvancedCardDialogManager.instance.openCard({ cardId })
                  );
                }
              };
            }
          })
          .onGridCreated(grid => {
            grid.descriptionVisibility = true;
            grid.getProperties().forEach((property: AutocompleteProperty) => {
              ShadowPropsHelper.add(property.tooltip, 'text', () => {
                const table = dataProvider.getTable<IAutocompleteRecord>(property.alias);
                return JSON.stringify(table.getPropertyRows(), undefined, 2);
              });
            });
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
const systemUser = { id: PlatformHelper.systemUserId, name: PlatformHelper.systemUserName };
PropertyGridBuilder.createAutocompleteProperty({
  data: new PropertyGridDataProvider({ autocomplete: [systemUser] }, true),
  alias: 'autocomplete',
  caption: 'Autocomplete property',
  dataConverter: new AutocompleteDataConverter(),
  dataContext: new AutocompleteDataViewContext({
    unique: true,
    multiple: true,
    viewAlias: 'Users',
    idColumn: 'UserID',
    nameColumn: 'UserName',
    parameterAlias: 'Name'
  }),
  onInitialized: async ({ control }) => {
    control.maxVisibleRecords = 10;
    // open user card in dialog
    control.menu.openAction.action = async () => {
      const cardId = control.selectedRecord?.model.id as string;
      if (Guid.isValid(cardId)) {
        if (!control.menu.openAction.autoclose) {
          control.menu.close();
        }
        await UIHost.showLoadingOverlay(
          async () => await AdvancedCardDialogManager.instance.openCard({ cardId })
        );
      }
    };
  }
});
~~~`
    });
  }

  private selectorProperty(): void {
    this.addBlock({
      caption: 'Selector',
      description: `This article shows how to work with selector property.`,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider({ selector: null, state: null }, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addSelectorProperty({
            data: dataProvider,
            alias: 'selector',
            caption: 'Selector property',
            tooltipVisibility: true,
            items: new Map([
              ['Success', Guid.newGuid()],
              ['Warning', Guid.newGuid()],
              ['Error', Guid.newGuid()]
            ]),
            displayField: 'state'
          })
          .onGridCreated(grid => {
            grid.descriptionVisibility = true;
            grid.getProperties().forEach((property: SelectorProperty) => {
              ShadowPropsHelper.add(property.tooltip, 'text', () => {
                const entry = dataProvider.getComplexEntry<IAutocompleteRecord>(property.alias);
                return JSON.stringify(entry.getPropertyValue());
              });
            });
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
PropertyGridBuilder.createSelectorProperty({
  data: new PropertyGridDataProvider({ selector: null, state: null }, true),
  alias: 'selector',
  caption: 'Selector property',
  items: new Map([
    ['Success', Guid.newGuid()],
    ['Warning', Guid.newGuid()],
    ['Error', Guid.newGuid()]
  ]),
  displayField: 'state'
});
~~~`
    });
  }

  private tableProperty(): void {
    this.addBlock({
      caption: 'Table',
      description: `This article shows how to work with table property.`,
      props: async () => {
        const data = {
          table: [
            { id: 1, name: 'User 1' },
            { id: 2, name: 'User 2' }
          ]
        };
        const dataProvider = new PropertyGridDataProvider(data, true);
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addTableProperty({
            data: dataProvider,
            alias: 'table',
            caption: 'Table property',
            tableOptions: {
              multiselect: true,
              editMode: 'cell',
              columnsMetadata: GridHelper.ensureTypedMetadata<IAutocompleteRecord>([
                {
                  id: 'ID',
                  caption: 'ID',
                  dataSourceKey: 'id',
                  dataType: FieldType.Int,
                  editorSettings: { editorType: 'integer' }
                },
                {
                  id: 'Name',
                  caption: 'Name',
                  dataSourceKey: 'name',
                  dataType: FieldType.String,
                  editorSettings: { editorType: 'string' }
                }
              ])
            }
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
const data = { table: [{ id: 1, name: 'User 1' }, { id: 2, name: 'User 2' }] };
PropertyGridBuilder.createTableProperty({
  data: new PropertyGridDataProvider({ table: data }, true),
  alias: 'table',
  caption: 'Table property',
  tableOptions: {
    multiselect: true,
    editMode: 'cell',
    columnsMetadata: GridHelper.ensureTypedMetadata<IAutocompleteRecord>([
      { id: 'ID', caption: 'ID', dataSourceKey: 'id', dataType: FieldType.Int, editorSettings: { editorType: 'integer' } },
      { id: 'Name', caption: 'Name', dataSourceKey: 'name', dataType: FieldType.String, editorSettings: { editorType: 'string' } }
    ])
  }
});
~~~`
    });
  }

  //#endregion
}
