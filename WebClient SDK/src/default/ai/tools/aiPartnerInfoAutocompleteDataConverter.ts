import { AiPartnerInfo } from 'tessa/ui/ai';
import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { AiToolHelper } from '../aiToolHelper';

/**
 * A converter that performs the conversion between {@link AiPartnerInfo} and {@link IAutocompleteRecord}.
 */
export class AiPartnerInfoAutocompleteDataConverter implements IAutocompleteDataConverter<AiPartnerInfo> {
  //#region IAutocompleteDataConverter<AiPartnerInfo> members

  toRecord(value: AiPartnerInfo): IAutocompleteRecord {
    return {
      id: value.id,
      name: AiToolHelper.formatPartnerName(value.shortName, value.fullName),
      data: { FullName: value.fullName }
    };
  }

  fromRecord(record: IAutocompleteRecord): AiPartnerInfo {
    const partner = new AiPartnerInfo();
    partner.id = record.id as string;
    partner.shortName = record.name!;
    partner.fullName = record.data?.FullName as string;

    return partner;
  }

  //#endregion
}
