import { computed, runInAction } from 'mobx';
import { IStorage, Primitive, StorageHelper } from '@tessa/core';
import { IViewModelDataSource } from '@tessa/ui';
import {
  IPropertyGridDataProvider,
  IPropertyDataSource,
  IComplexEntryPropertyDataProvider
} from 'tessa/ui/propertyGrid';
import { IOcrTextFieldDataSource } from '../control/ocrTextFieldTypes';
import { OcrGridData } from '../ocrGridTypes';
import { IOcrPropertySettings } from './ocrPropertySettings';

/** Data source for the recognized text property. */
export class OcrPropertyDataSource
  implements IPropertyDataSource<IViewModelDataSource<string | null>>, IOcrTextFieldDataSource
{
  //#region constructors

  /**
   * Creates an instance of the {@link OcrPropertyDataSource} class.
   * @param dataProvider The data provider for getting/setting the property value.
   * @param settings The settings for the property.
   */
  constructor(dataProvider: IPropertyGridDataProvider, settings: IOcrPropertySettings) {
    this.key = settings.alias;
    this._dataProvider = dataProvider;
    this._propertyDataProvider = dataProvider.getComplexEntry(this.key);
  }

  //#endregion

  //#region fields

  protected readonly _dataProvider: IPropertyGridDataProvider;

  protected readonly _propertyDataProvider: IComplexEntryPropertyDataProvider<OcrGridData>;

  //#endregion

  //#region properties

  nullable = true;

  readonly key: string;

  @computed
  get isChanged(): boolean {
    return this._propertyDataProvider.isChanged || !!this.getData().modified;
  }

  //#endregion

  //#region IOcrTextFieldDataSource implementation

  getValue(): string | null {
    return this.convertToString(this.getData().value);
  }

  setValue(value: string | null): void {
    this.modifyData(undefined, this.convertNullableValue(value));
  }

  getDisplayed(): string | null {
    return this.getData().displayed;
  }

  setDisplayed(value: string | null): void {
    this.modifyData(value, undefined);
  }

  getRef(): number[] | number | null {
    return this.getData().refs;
  }

  setRef(value: number[] | number | null): void {
    runInAction(() => {
      const data = this.getData();
      data.refs = value;
    });
  }

  get(): { displayed: string | null; value: string | null } {
    const data = this.getData();
    const value = this.convertNullableValue(this.convertToString(data.value));
    return { displayed: data.displayed, value };
  }

  set(displayed: string | null, value: string | null): void {
    this.modifyData(displayed, this.convertNullableValue(value));
  }

  clear(): void {
    this.modifyData(null, this.convertNullableValue(null));
  }

  dispose(): void {}

  //#endregion

  //#region protected methods

  protected getData(): OcrGridData {
    return this._propertyDataProvider.getPropertyValue();
  }

  protected modifyData(displayed?: string | null, value?: IStorage | Primitive | null): void {
    runInAction(() => {
      const data = this.getData();
      data.modified = true;
      displayed !== undefined && (data.displayed = displayed);
      value !== undefined && (data.value = value);
      this._propertyDataProvider.setPropertyValue(data);
    });
  }

  protected convertNullableValue(value: string | null): string | null {
    return this.nullable ? (value === '' ? null : value) : value === null ? '' : value;
  }

  private convertToString(value: IStorage | Primitive | null): string | null {
    return value !== null && StorageHelper.isPrimitiveType(value) ? value.toString() : null;
  }

  //#endregion
}
