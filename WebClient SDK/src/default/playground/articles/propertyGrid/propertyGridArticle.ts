import { FieldType, Guid } from '@tessa/core';
import { injectable } from '@tessa/application';
import { DateTimeTypeFormat } from '@tessa/platform';
import { SyntaxHighlighting } from 'ui/uiEnums';
import { BackgroundPaletteAlias } from 'ui/colorPicker';
import { AutocompleteDataConverter, AutocompleteDataViewContext } from 'ui/autocomplete';
import { IconSelector$ } from 'tessa/ui/iconSelector';
import { PlaygroundArticle } from 'tessa/ui/playground/chunk';
import type { PlaygroundArticleSettings, PlaygroundBlockFactory } from 'tessa/ui/playground';
import { PropertyGrid, PropertyGridBuilder, PropertyGridDataProvider } from 'tessa/ui/propertyGrid';

@injectable()
export abstract class PropertyGridArticle extends PlaygroundArticle {
  //#region properties

  protected abstract readonly name: string;
  protected abstract readonly description: string;
  protected abstract readonly keywords: Set<string>;

  //#endregion

  //#region base overrides

  abstract initialize(): Promise<void>;

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: `PropertyGrid/${this.name}`,
      description:
        'UI component designed for viewing and editing object properties in a name-value mode. ' +
        'It is commonly used in configuration tools, editors, properties or details panels.' +
        '\n\n' +
        this.description,
      keywords: ['property', 'properties', 'props', 'grid', 'editor', 'browser', ...this.keywords]
    };
  }

  protected override addBlock<T extends Record<string, unknown>>(
    props: PlaygroundBlockFactory<T>
  ): void {
    const sourceProps = props.props;
    if (sourceProps) {
      props.props = context => {
        context.block.codeIsCollapsed = true;
        return sourceProps(context);
      };
    }

    super.addBlock(props);
  }

  //#endregion

  //#region protected methods

  protected createPropertyGrid(alias?: string): PropertyGrid {
    const data = {
      integer: Math.round(Math.random() * 100),
      float: Math.random() * 100,
      decimal: Math.random() * 100,
      label: `{"id": "${Guid.newGuid()}"}`,
      message: `{"id": "${Guid.newGuid()}"}`,
      hyperlink: `{"id": "${Guid.newGuid()}"}`,
      string: `{"id": "${Guid.newGuid()}"}`,
      code: `{"id": "${Guid.newGuid()}"}`,
      date: new Date().toISOString(),
      interval: new Date().toISOString(),
      time: new Date().toISOString(),
      datetime: new Date().toISOString(),
      boolean: true,
      icon: null,
      color: null,
      slider: Math.round(Math.random() * 100),
      autocomplete: { id: Guid.newGuid(), name: 'User' },
      table: [{ id: Guid.newGuid(), name: 'User' }]
    };
    const dataProvider = new PropertyGridDataProvider(data, true);
    const propertyGrid = PropertyGridBuilder.create(dataProvider)
      .startGroup({ caption: 'Text group', order: 1, collapsed: true })
      .addTextProperty({
        data: dataProvider,
        alias: 'string',
        caption: 'String property',
        tooltip: 'Property for editing string value'
      })
      .addCodeProperty({
        data: dataProvider,
        alias: 'code',
        caption: 'Code property',
        tooltip: 'Property for editing code value',
        highlightingMode: SyntaxHighlighting.JSON
      })
      .startGroup({ caption: 'Label group', order: 2, collapsed: true })
      .addLabelProperty({
        data: dataProvider,
        alias: 'label',
        caption: 'Label property',
        tooltip: 'Property for displaying label value',
        type: 'normal'
      })
      .addLabelProperty({
        data: dataProvider,
        alias: 'message',
        caption: 'Message property',
        tooltip: 'Property for displaying message value',
        type: 'message'
      })
      .addLabelProperty({
        data: dataProvider,
        alias: 'hyperlink',
        caption: 'Hyperlink property',
        tooltip: 'Property for displaying hyperlink value',
        type: 'hyperlink',
        href: 'https://tessa.ru'
      })
      .startGroup({ caption: 'Number group', order: 3, collapsed: true })
      .addNumericProperty({
        data: dataProvider,
        alias: 'integer',
        caption: 'Integer property',
        controlType: 'Integer',
        tooltip: 'Property for editing integer value'
      })
      .addNumericProperty({
        data: dataProvider,
        alias: 'float',
        caption: 'Float property',
        controlType: 'Float',
        tooltip: 'Property for editing float value'
      })
      .addNumericProperty({
        data: dataProvider,
        alias: 'decimal',
        caption: 'Decimal property',
        controlType: 'Decimal',
        tooltip: 'Property for editing decimal value'
      })
      .startGroup({ caption: 'Date group', order: 4, collapsed: true })
      .addDateTimeProperty({
        data: dataProvider,
        alias: 'date',
        caption: 'Date property',
        tooltip: 'Property for editing date value',
        formatType: DateTimeTypeFormat.Date
      })
      .addDateTimeProperty({
        data: dataProvider,
        alias: 'interval',
        caption: 'Interval property',
        tooltip: 'Property for editing interval value',
        formatType: DateTimeTypeFormat.Interval
      })
      .addDateTimeProperty({
        data: dataProvider,
        alias: 'time',
        caption: 'Time property',
        tooltip: 'Property for editing time value',
        formatType: DateTimeTypeFormat.Time
      })
      .addDateTimeProperty({
        data: dataProvider,
        alias: 'datetime',
        caption: 'DateTime property',
        tooltip: 'Property for editing date and time value',
        formatType: DateTimeTypeFormat.DateTime
      })
      .startGroup({ caption: 'Misc group', order: 5, collapsed: true })
      .addBooleanProperty({
        data: dataProvider,
        alias: 'boolean',
        caption: 'Boolean property',
        tooltip: 'Property for editing boolean value'
      })
      .addIconProperty({
        data: dataProvider,
        alias: 'icon',
        caption: 'Icon property',
        tooltip: 'Property for editing icon value',
        iconSelector: window.tessa.diContainer.get(IconSelector$)
      })
      .addColorProperty({
        data: dataProvider,
        alias: 'color',
        caption: 'Color property',
        tooltip: 'Property for editing color value',
        palette: BackgroundPaletteAlias
      })
      .addAutocompleteProperty({
        data: dataProvider,
        alias: 'autocomplete',
        caption: 'Autocomplete property',
        tooltip: 'Property for editing reference value',
        dataConverter: new AutocompleteDataConverter(),
        dataContext: new AutocompleteDataViewContext({
          viewAlias: 'Users',
          idColumn: 'UserID',
          nameColumn: 'UserName',
          parameterAlias: 'Name'
        })
      })
      .addTableProperty({
        data: dataProvider,
        alias: 'table',
        caption: 'Table property',
        tableOptions: {
          editMode: 'cell',
          columnsMetadata: [
            {
              id: 'ID',
              caption: 'ID',
              dataSourceKey: 'id',
              dataType: FieldType.Guid,
              editorSettings: { editorType: 'string' }
            },
            {
              id: 'Name',
              caption: 'Name',
              dataSourceKey: 'name',
              dataType: FieldType.String,
              editorSettings: { editorType: 'string' }
            }
          ]
        }
      })
      .onGridCreated(grid => {
        grid.toolbarVisibility = true;
        grid.descriptionVisibility = true;
      })
      .build();

    if (alias) {
      propertyGrid.alias = alias;
    }

    return propertyGrid;
  }

  //#endregion
}
