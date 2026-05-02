import { observable } from 'mobx';
import { Guid } from '@tessa/core';
import { IDataProvider } from 'tessa/ui/formEditor/data/definitions';
import { FormDataSource } from 'tessa/ui/formEditor/data/formDataSource';
import {
  AutocompleteDataSource,
  IAutocompleteDataContext,
  IAutocompleteDataConverter,
  IAutocompleteItem,
  IAutocompleteRecord
} from 'ui/autocomplete';

export class ApiAccessTokensScopeAutocompleteDataSource extends AutocompleteDataSource<IAutocompleteItem> {
  //#region constructors

  constructor(key: string, provider?: IDataProvider) {
    super(
      ApiAccessTokensScopeAutocompleteDataSource.createContext(),
      ApiAccessTokensScopeAutocompleteDataSource.createConverter(),
      ApiAccessTokensScopeAutocompleteDataSource.createSource(),
      ApiAccessTokensScopeAutocompleteDataSource.createMasterSource(key, provider)
    );
  }

  //#endregion

  override createDefaultRecord(options?: Partial<IAutocompleteRecord>): IAutocompleteRecord {
    return { id: options?.name?.toLocaleLowerCase() || Guid.empty, name: options?.name || '' };
  }

  //#region private methods

  private static createSource(): IAutocompleteItem[] {
    return observable.array([], { deep: false });
  }

  private static createMasterSource(
    key: string,
    provider?: IDataProvider
  ): FormDataSource<IAutocompleteItem[]> | undefined {
    if (!provider || !key) {
      return;
    }

    const master = new FormDataSource<IAutocompleteItem[]>(key, provider);
    // master.setValue(ApiAccessTokensScopeAutocompleteDataSource.createSource());
    return master;
  }

  private static createContext(): IAutocompleteDataContext {
    return {
      unique: true,
      multiple: true,
      layoutFactory: () => Promise.resolve([]),
      itemsFactory: () => Promise.resolve([])
    };
  }

  private static createConverter(): IAutocompleteDataConverter<IAutocompleteItem> {
    return {
      toRecord(value: IAutocompleteItem): IAutocompleteRecord {
        return value as IAutocompleteRecord;
      },
      fromRecord(record: IAutocompleteRecord): IAutocompleteItem {
        return { id: record.name?.toLocaleLowerCase(), name: record.name };
      }
    };
  }

  //#endregion
}
