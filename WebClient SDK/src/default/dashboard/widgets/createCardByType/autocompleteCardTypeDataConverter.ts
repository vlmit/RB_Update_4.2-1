import { IStorage } from '@tessa/core';
import { localize } from '@tessa/application';
import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { CardTypeReference } from './cardTypeReference';

export class AutocompleteCardTypeDataConverter implements IAutocompleteDataConverter<CardTypeReference> {
  toRecord(value: CardTypeReference): IAutocompleteRecord {
    const data: IStorage = {
      cardTypeName: value.cardTypeName,
      cardTypeCaption: value.cardTypeCaption
    };

    if (value.documentTypeId) {
      data.cardTypeId = value.cardTypeId;
      data.documentTypeTitle = value.documentTypeTitle;
    }

    return {
      id: value.documentTypeId ?? value.cardTypeId,
      name: localize(value.documentTypeTitle ?? value.cardTypeCaption),
      data
    };
  }

  fromRecord(record: IAutocompleteRecord): CardTypeReference {
    const cardType = new CardTypeReference();

    const cardTypeId = record.data?.cardTypeId;
    if (cardTypeId) {
      cardType.cardTypeId = cardTypeId as string;
      cardType.cardTypeName = record.data.cardTypeName as string;
      cardType.cardTypeCaption = record.data.cardTypeCaption as string;
      cardType.documentTypeId = record.id as string;
      cardType.documentTypeTitle = record.data?.documentTypeTitle as string;
    } else {
      cardType.cardTypeId = record.id as string;
      cardType.cardTypeName = record.data?.cardTypeName as string;
      cardType.cardTypeCaption = record.data?.cardTypeCaption as string;
    }

    return cardType;
  }
}
