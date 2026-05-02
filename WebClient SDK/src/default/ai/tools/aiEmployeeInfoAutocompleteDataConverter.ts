import { AiEmployeeInfo } from 'tessa/ui/ai';
import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';

/**
 * A converter that performs the conversion between {@link AiEmployeeInfo} and {@link IAutocompleteRecord}.
 */
export class AiEmployeeInfoAutocompleteDataConverter implements IAutocompleteDataConverter<AiEmployeeInfo> {
  //#region IAutocompleteDataConverter<AiEmployeeInfo> members

  toRecord(value: AiEmployeeInfo): IAutocompleteRecord {
    return {
      id: value.id,
      name: value.displayName,
      data: {
        FullName: value.name,
        Email: value.email,
        Position: value.position,
        Departments: value.departments
      }
    };
  }

  fromRecord(record: IAutocompleteRecord): AiEmployeeInfo {
    const partner = new AiEmployeeInfo();
    partner.id = record.id as string;
    partner.displayName = record.name!;
    partner.name = record.data?.FullName as string;
    partner.email = record.data?.Email as string;
    partner.position = record.data?.Position as string;
    partner.departments = record.data?.Departments as string;

    return partner;
  }

  //#endregion
}
