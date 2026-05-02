import { observable, runInAction } from 'mobx';
import { property, ShadowPropsHelper } from '@tessa/ui';
import {
  Guid,
  List,
  IStorage,
  Primitive,
  FieldType,
  TypedField,
  StorageHelper,
  DefaultValues
} from '@tessa/core';
import {
  StorageAny,
  PropertyGridBuilder,
  PropertyGridDataProvider,
  PropertyGridHelper,
  ITablePropertyRowMetadata,
  TablePropertyDataProvider,
  EntryPropertyDataProvider,
  ComplexEntryPropertyDataProvider
} from 'tessa/ui/propertyGrid';
import { AutocompleteDataConverter, AutocompleteDataViewContext } from 'ui/autocomplete';
import { PropertyGridArticle } from './propertyGridArticle';
import { PropertyGridDemoView } from './propertyGridDemoForm';

export class PropertyGridProvidersArticle extends PropertyGridArticle {
  //#region base overrides

  protected override readonly name = 'Providers';
  protected override readonly description =
    'Shows how to override default data provider behavior. ' +
    'Default data provider only knows how to work with `IStorage` object, ' +
    'and you may need to change the logic for `StorageObject`, `Card` or some custom object. ' +
    '\n\n' +
    '**To implement card data providers, please refer to example #36 in the developer documentation.**';
  protected override readonly keywords = new Set(['data', 'provider', 'storage', 'object']);

  override async initialize(): Promise<void> {
    this.keywords.add('entry');
    this.entryProvider();

    this.keywords.add('complex');
    this.complexProvider();

    this.keywords.add('table');
    this.tableProvider();
  }

  //#endregion

  //#region private methods

  private entryProvider(): void {
    this.addBlock({
      caption: 'Entry',
      description: 'Override any typed entry values for string value',
      props: async () => {
        const data: StorageAny = observable.object({ field1: true, field2: 5, field3: 'text' });
        const dataProvider = new PropertyGridDataProvider(data, false, {
          entry: (key, provider) => new CustomEntryPropertyDataProvider(provider, data, key)
        });
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addTextProperty({ data: dataProvider, alias: 'field1', caption: 'Field 1' })
          .addTextProperty({ data: dataProvider, alias: 'field2', caption: 'Field 3' })
          .addTextProperty({ data: dataProvider, alias: 'field3', caption: 'Field 4' })
          .onGridCreated(grid => {
            grid.descriptionVisibility = true;
            grid.getProperties().forEach(property => {
              property.tooltip.visibility = true;
              ShadowPropsHelper.add(property.tooltip, 'text', () => {
                const value = StorageHelper.tryGet<Primitive>(data, property.alias!);
                const defaultType = TypedField.tryGetDefaultType(value);
                return `Type is '${defaultType}'`;
              });
            });
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
class CustomEntryPropertyDataProvider extends EntryPropertyDataProvider<Primitive> {
  override getPropertyValue(): string {
    const value = StorageHelper.tryGet<Primitive | null>(this._data, this.key);
    return value?.toString() ?? '';
  }

  override setPropertyValue(value: string): void {
    if (value === 'true' || value === 'false') {
      super.setPropertyValue(Boolean(value));
    } else if (!isNaN(+value) && value.trim() !== '') {
      super.setPropertyValue(Number(value));
    } else {
      super.setPropertyValue(value);
    }
  }
}

const data: StorageAny = observable.object({ field1: true, field2: 5, field3: 'text' });
const dataProvider = new PropertyGridDataProvider(data, false, {
  entry: (key, provider) => new CustomEntryPropertyDataProvider(provider, data, key)
});
const propertyGrid = await PropertyGridBuilder.create(dataProvider)
  .addTextProperty({ data: dataProvider, alias: 'field1', caption: 'Field 1' })
  .addTextProperty({ data: dataProvider, alias: 'field2', caption: 'Field 3' })
  .addTextProperty({ data: dataProvider, alias: 'field3', caption: 'Field 4' })
  .buildWithInitialize();
~~~`
    });
  }

  private complexProvider(): void {
    this.addBlock({
      caption: 'Complex',
      description: 'Override complex entry for user autocomplete record',
      props: async () => {
        const data: StorageAny = observable.object({ userID: null, userName: null });
        const dataProvider = new PropertyGridDataProvider(data, false, {
          complexEntry: (key, provider) =>
            key === 'user'
              ? new CustomComplexEntryPropertyDataProvider(['ID', 'Name'], provider, data, key)
              : null
        });
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addAutocompleteProperty({
            data: dataProvider,
            alias: 'user',
            caption: 'User',
            dataConverter: new AutocompleteDataConverter(),
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Users',
              idColumn: 'UserID',
              nameColumn: 'UserName',
              parameterAlias: 'Name'
            })
          })
          .addTextProperty({
            data: dataProvider,
            alias: 'userID',
            caption: 'User ID',
            disabled: true
          })
          .addTextProperty({
            data: dataProvider,
            alias: 'userName',
            caption: 'User Name',
            disabled: true
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
class CustomComplexEntryPropertyDataProvider extends ComplexEntryPropertyDataProvider<IStorage> {
  constructor(
    private readonly _fields: Array<string>,
    ...args: ConstructorParameters<typeof ComplexEntryPropertyDataProvider>
  ) {
    super(...args);
  }

  override getPropertyValue(): IStorage {
    return this._fields.reduce((object, field) => {
      object[field.toLowerCase()] = StorageHelper.tryGet(this._data, this.key + field);
      return object;
    }, {});
  }

  override setPropertyValue(value: IStorage): void {
    runInAction(() => {
      this._fields.forEach(field => (this._data[this.key + field] = StorageHelper.tryGet(value, field.toLowerCase())) );
      this._gridDataProvider.reportChanged(this.key, true);
    });
  }
}

const data: StorageAny = observable.object({ userID: null, userName: null });
const dataProvider = new PropertyGridDataProvider(data, false, {
  complexEntry: (key, provider) => key === 'user' ? new CustomComplexEntryPropertyDataProvider(['ID', 'Name'], provider, data, key) : null
});
const propertyGrid = await PropertyGridBuilder.create(dataProvider)
  .addAutocompleteProperty({
    data: dataProvider,
    alias: 'user',
    caption: 'User',
    dataConverter: new AutocompleteDataConverter(),
    dataContext: new AutocompleteDataViewContext({ viewAlias: 'Users', idColumn: 'UserID', nameColumn: 'UserName', parameterAlias: 'Name' })
  })
  .addTextProperty({ data: dataProvider, alias: 'userID', caption: 'User ID', disabled: true })
  .addTextProperty({ data: dataProvider, alias: 'userName', caption: 'User Name', disabled: true })
  .buildWithInitialize();
~~~`
    });
  }

  private tableProvider(): void {
    this.addBlock({
      caption: 'Table',
      description: `Default logic for table properties is unsuitable for custom classes, so we need to override it.`,
      props: async () => {
        const data = new TestData();
        data.users.push(
          new TestUser(Guid.newGuid(), 'User 1'),
          new TestUser(Guid.newGuid(), 'User 2')
        );
        const dataProvider = new PropertyGridDataProvider(data, false, {
          table: (key, provider) =>
            key === 'users' ? new CustomTablePropertyDataProvider(provider, data, key) : null
        });
        const propertyGrid = await PropertyGridBuilder.create(dataProvider)
          .addTextProperty({ data: dataProvider, alias: 'description', caption: 'Description' })
          .addTableProperty({
            data: dataProvider,
            alias: 'users',
            caption: 'Users',
            tableOptions: {
              columnsMetadata: [
                { id: 'ID', caption: 'ID', dataSourceKey: 'id', dataType: FieldType.Guid },
                { id: 'Name', caption: 'Name', dataSourceKey: 'name', dataType: FieldType.String }
              ]
            },
            hooks: [
              {
                rowDoubleClick: async context => {
                  const sourceUser = context.row.getSource<TestUser>();
                  const userCopy = new TestUser(sourceUser.id, sourceUser.name);
                  const userDataProvider = new PropertyGridDataProvider(userCopy, false);
                  const userPropertyGrid = await PropertyGridBuilder.create(userDataProvider)
                    .addTextProperty({ data: userDataProvider, alias: 'id', caption: 'Identifier' })
                    .addTextProperty({ data: userDataProvider, alias: 'name', caption: 'Name' })
                    .onGridCreated(grid => (grid.title = 'User editor'))
                    .buildWithInitialize();
                  if (await PropertyGridHelper.showDialog(userPropertyGrid)) {
                    runInAction(() => {
                      sourceUser.id = userCopy.id;
                      sourceUser.name = userCopy.name;
                    });
                  }
                }
              }
            ]
          })
          .buildWithInitialize();

        return { viewModel: propertyGrid };
      },
      view: PropertyGridDemoView,
      code: `
~~~jsx
class TestUser {
  constructor(id = DefaultValues.guid, name = DefaultValues.str) {
    this.id = id;
    this.name = name;
  }

  @property() id = DefaultValues.guid;
  @property() name = DefaultValues.str;
}

class TestData {
  @property() description = DefaultValues.str;
  readonly users = List.create({ observable: true });
}

class CustomTablePropertyDataProvider extends TablePropertyDataProvider<TestUser> {
  override createPropertyRow(): TestUser {
    return new TestUser();
  }

  override getPropertyRowMetadata(row: TestUser): ITablePropertyRowMetadata | null {
    return { id: row.id };
  }
}

const data = new TestData();
data.users.push(new TestUser(Guid.newGuid(), 'User 1'), new TestUser(Guid.newGuid(), 'User 2'));
const dataProvider = new PropertyGridDataProvider(data, false, {
  table: (key, provider) => key === 'users' ? new CustomTablePropertyDataProvider(provider, data, key) : null
});

const propertyGrid = await PropertyGridBuilder.create(dataProvider)
  .addTextProperty({ data: dataProvider, alias: 'description', caption: 'Description' })
  .addTableProperty({
    data: dataProvider,
    alias: 'users',
    caption: 'Users',
    tableOptions: {
      columnsMetadata: [
        { id: 'ID', caption: 'ID', dataSourceKey: 'id', dataType: FieldType.Guid },
        { id: 'Name', caption: 'Name', dataSourceKey: 'name', dataType: FieldType.String }
      ]
    },
    hooks: [
      {
        rowDoubleClick: async context => {
          const sourceUser = context.row.getSource<TestUser>();
          const userCopy = new TestUser(sourceUser.id, sourceUser.name);
          const userDataProvider = new PropertyGridDataProvider(userCopy, false);
          const userPropertyGrid = await PropertyGridBuilder.create(userDataProvider)
            .addTextProperty({ data: userDataProvider, alias: 'id', caption: 'Identifier' })
            .addTextProperty({ data: userDataProvider, alias: 'name', caption: 'Name' })
            .onGridCreated(grid => (grid.title = 'User editor'))
            .buildWithInitialize();
          if (await PropertyGridHelper.showDialog(userPropertyGrid)) {
            runInAction(() => {
              sourceUser.id = userCopy.id;
              sourceUser.name = userCopy.name;
            });
          }
        }
      }
    ]
  })
  .buildWithInitialize();
~~~`
    });
  }

  //#endregion
}

class CustomEntryPropertyDataProvider extends EntryPropertyDataProvider<Primitive> {
  override getPropertyValue(): string {
    const value = StorageHelper.tryGet<Primitive | null>(this._data, this.key);
    return value?.toString() ?? '';
  }

  override setPropertyValue(value: string): void {
    if (value === 'true' || value === 'false') {
      super.setPropertyValue(Boolean(value));
    } else if (!isNaN(+value) && value.trim() !== '') {
      super.setPropertyValue(Number(value));
    } else {
      super.setPropertyValue(value);
    }
  }
}

class CustomComplexEntryPropertyDataProvider extends ComplexEntryPropertyDataProvider<IStorage> {
  constructor(
    private readonly _fields: Array<string>,
    ...args: ConstructorParameters<typeof ComplexEntryPropertyDataProvider>
  ) {
    super(...args);
  }

  override getPropertyValue(): IStorage {
    return this._fields.reduce((object, field) => {
      object[field.toLowerCase()] = StorageHelper.tryGet(this._data, this.key + field);
      return object;
    }, {});
  }

  override setPropertyValue(value: IStorage): void {
    runInAction(() => {
      this._fields.forEach(
        field => (this._data[this.key + field] = StorageHelper.tryGet(value, field.toLowerCase()))
      );
      this._gridDataProvider.reportChanged(this.key, true);
    });
  }
}

class CustomTablePropertyDataProvider extends TablePropertyDataProvider<TestUser> {
  override createPropertyRow(): TestUser {
    return new TestUser();
  }

  override getPropertyRowMetadata(row: TestUser): ITablePropertyRowMetadata | null {
    return { id: row.id };
  }
}

class TestUser {
  constructor(id = DefaultValues.guid, name = DefaultValues.str) {
    this.id = id;
    this.name = name;
  }

  @property() id = DefaultValues.guid;
  @property() name = DefaultValues.str;
}

class TestData {
  @property() description = DefaultValues.str;
  readonly users = List.create({ observable: true });
}
