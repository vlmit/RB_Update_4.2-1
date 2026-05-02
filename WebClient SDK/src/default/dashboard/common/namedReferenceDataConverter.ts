import { localize } from '@tessa/application';
import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { NamedReference } from './namedReference';

export class NamedReferenceDataConverter implements IAutocompleteDataConverter<NamedReference> {
  //#region constructors

  constructor(private readonly _localizable = true) {}

  //#endregion

  //#region IAutocompleteDataConverter

  readonly toRecord = (value: NamedReference): IAutocompleteRecord => {
    const name = this._localizable ? localize(value.name) : value.name;
    return { id: value.id, name };
  };

  readonly fromRecord = (record: IAutocompleteRecord): NamedReference => {
    return new NamedReference(record.id as string, record.name!);
  };

  //#endregion
}
