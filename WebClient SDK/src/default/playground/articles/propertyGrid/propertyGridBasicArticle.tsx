import { Guid } from '@tessa/core';
import {
  PropertyGrid,
  PropertyGridBuilder,
  PropertyGridDataProvider,
  PropertyGroup,
  BooleanProperty,
  BooleanPropertyDataSource,
  DateTimeProperty,
  DateTimePropertyDataSource,
  TextProperty,
  TextPropertyDataSource,
  MultiplePropertyGrid
} from 'tessa/ui/propertyGrid';
import { PropertyGridArticle } from './propertyGridArticle';
import { PropertyGridDemoView, PropertyGridMultipleDemoView } from './propertyGridDemoForm';

export class PropertyGridBasicArticle extends PropertyGridArticle {
  //#region base overrides

  protected override readonly name = 'Basic';
  protected override readonly description = `This article shows the basics of creating, initializing, displaying property grid.`;
  protected override readonly keywords = new Set(['basic', 'create', 'initialize', 'display']);

  override async initialize(): Promise<void> {
    this.keywords.add('manual');
    this.manualInstantiation();

    this.keywords.add('builder');
    this.constructingByBuilder();

    this.keywords.add('multiple');
    this.multipleManualInstantiation();
  }

  //#endregion

  //#region private methods

  private manualInstantiation(): void {
    this.addBlock({
      caption: 'Manual instantiation',
      description: `Creation and initialization through manual instantiation of objects.`,
      props: async () => {
        const data = { string: Guid.newGuid(), date: null, boolean: true };
        const dataProvider = new PropertyGridDataProvider(data, true);

        const textDataSource = new TextPropertyDataSource('string', dataProvider);
        const textProperty = new TextProperty(textDataSource, {
          alias: textDataSource.key,
          caption: 'String property',
          tooltip: 'Property for editing string value',
          required: true
        });

        const dateDataSource = new DateTimePropertyDataSource('date', dataProvider);
        const dateProperty = new DateTimeProperty(dateDataSource, {
          alias: dateDataSource.key,
          caption: 'Date property',
          tooltip: 'Property for editing date and time value',
          placeholder: 'Input date and time...'
        });

        const booleanDataSource = new BooleanPropertyDataSource('boolean', dataProvider);
        const booleanProperty = new BooleanProperty(booleanDataSource, {
          alias: booleanDataSource.key,
          caption: 'Boolean property',
          tooltip: 'Property for editing boolean value',
          disabled: true
        });

        const mainGroup = new PropertyGroup({ caption: 'Main group' }, textProperty, dateProperty);
        const additionalGroup = new PropertyGroup({ caption: 'Additional group' }, booleanProperty);

        const propertyGrid = new PropertyGrid(dataProvider, mainGroup, additionalGroup);
        propertyGrid.toolbarVisibility = true;
        propertyGrid.descriptionVisibility = true;
        await propertyGrid.initialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
// 1. Initialize data storage
const data = { boolean: true };
const dataProvider = new PropertyGridDataProvider(data, true);

// 2. Initialize properties
const booleanDataSource = new BooleanPropertyDataSource('boolean', dataProvider);
const booleanProperty = new BooleanProperty(booleanDataSource, {
  alias: booleanDataSource.key,
  caption: 'Boolean property',
  tooltip: 'Property for editing boolean value'
});

// 3. Initialize property groups
const additionalGroup = new PropertyGroup({ caption: 'Additional group' }, booleanProperty);

// 4. Initialize property grid
const propertyGrid = new PropertyGrid(dataProvider, additionalGroup);
propertyGrid.toolbarVisibility = true;
propertyGrid.descriptionVisibility = true;
await propertyGrid.initialize();

// 5. Display property grid
<PropertyGridComponent viewModel={propertyGrid}/>
~~~`
    });
  }

  private constructingByBuilder(): void {
    this.addBlock({
      caption: 'Constructing by builder',
      description: `Creation and initialization using a specialized builder.`,
      props: async () => {
        const data = { string: Guid.newGuid(), date: null, boolean: true };
        const dataProvider = new PropertyGridDataProvider(data, true);

        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .startGroup({ caption: 'Main group' })
          .addTextProperty({
            data: dataProvider,
            alias: 'string',
            caption: 'String property',
            tooltip: 'Property for editing string value'
          })
          .addDateTimeProperty({
            data: dataProvider,
            alias: 'date',
            caption: 'Date property',
            tooltip: 'Property for editing date and time value'
          })
          .startGroup({ caption: 'Additional group' })
          .addBooleanProperty({
            alias: 'boolean',
            caption: 'Boolean property',
            tooltip: 'Property for editing boolean value'
          })
          .onGridCreated(grid => {
            grid.toolbarVisibility = true;
            grid.descriptionVisibility = true;
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
// 1. Initialize data storage
const data = { boolean: true };
const dataProvider = new PropertyGridDataProvider(data, true);

// 2. Initialize property grid with builder
const propertyGrid = await PropertyGridBuilder.create(dataProvider)
  .startGroup({ caption: 'Additional group' })
  .addBooleanProperty({
    alias: 'boolean',
    caption: 'Boolean property',
    tooltip: 'Property for editing boolean value'
  })
  .onGridCreated(grid => {
    grid.toolbarVisibility = true;
    grid.descriptionVisibility = true;
  })
  .buildWithInitialize();

// 3. Display property grid
<PropertyGridComponent viewModel={propertyGrid}/>
~~~`
    });
  }

  private multipleManualInstantiation(): void {
    this.addBlock({
      caption: 'Multiple manual instantiation',
      description: `Creation and initialization through manual instantiation of multiple property grid.`,
      props: async () => {
        const multiplePropertyGrid = new MultiplePropertyGrid({ caption: 'Multiple grid' });
        multiplePropertyGrid.addGrid(this.createPropertyGrid('First Property Grid'), true);
        multiplePropertyGrid.addGrid(this.createPropertyGrid('Second Property Grid'), false);
        await multiplePropertyGrid.initialize();

        return { viewModel: multiplePropertyGrid };
      },
      view: PropertyGridMultipleDemoView,
      code: `
~~~jsx
// 1. Initialize multiple property grid
const multiplePropertyGrid = new MultiplePropertyGrid({ caption: 'Multiple grid' });
multiplePropertyGrid.addGrid(new PropertyGrid(...), true);
multiplePropertyGrid.addGrid(new PropertyGrid(...), false);
await multiplePropertyGrid.initialize();

// 2. Display multiple property grid
<PropertyGridMultipleComponent viewModel={multiplePropertyGrid} />
~~~`
    });
  }

  //#endregion
}
