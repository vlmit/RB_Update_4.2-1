import { FieldType, IStorage, Primitive, TypedJsonConverter } from '@tessa/core';
import { injectable } from '@tessa/application';
import {
  Card,
  CardTypeForm,
  CardTypeBlock,
  CardTypeControl,
  CardTypeCustomControl,
  CardControlType,
  CardControlTypeFlags,
  CardControlTypeUsageMode
} from '@tessa/platform';
import { ICardModel, IControlViewModel } from 'tessa/ui/cards/interfaces';
import { ControlTypeBase } from 'tessa/ui/cards/controls/controlTypeBase';
import { PropertyGridDataProvider } from 'tessa/ui/propertyGrid';
import { OcrGridViewModel } from './ocrGridViewModel';

/** The type for the OCR property grid control. */
@injectable()
export class OcrGridType extends ControlTypeBase {
  /**
   * Creates a view model for the OCR property grid control.
   * @param control The control configuration for the OCR grid.
   * @param _block The block configuration (unused in this implementation).
   * @param _form The form configuration (unused in this implementation).
   * @param _parentControl The parent control (null if none).
   * @param model The card model available within the UI.
   * @returns An instance of {@link OcrGridViewModel} for the OCR property grid.
   */
  protected async createControlCore(
    control: CardTypeControl,
    _block: CardTypeBlock,
    _form: CardTypeForm,
    _parentControl: CardTypeControl | null,
    model: ICardModel
  ): Promise<IControlViewModel> {
    return new OcrGridViewModel(control as CardTypeCustomControl, model);
  }
}

/** An instance of the OCR property grid control type . */
export const OcrGridControlType = new CardControlType(
  '78174631-ace7-48e0-b6c5-4bb99a48edb8',
  'OcrPropertyGrid',
  CardControlTypeUsageMode.Custom,
  CardControlTypeFlags.UseInCards |
    CardControlTypeFlags.SpanByDefault |
    CardControlTypeFlags.HideCaptionByDefault
);

/** Provides data for the OCR property grid control. */
export class OcrGridDataProvider extends PropertyGridDataProvider {
  /**
   * Serializes a section and field name into a key.
   * @param sectionName The name of the section.
   * @param fieldName The name of the field.
   * @returns A string representing the serialized key.
   */
  static serializeKey(sectionName: string, fieldName: string): string {
    return `${sectionName}.${fieldName}`;
  }

  /**
   * Deserializes a key into a section name and field name.
   * @param key The serialized key.
   * @returns A tuple containing the section name and field name.
   */
  static deserializeKey(key: string): [string, string] {
    const [sectionName, fieldName] = key.split('.');
    return [sectionName, fieldName];
  }
}

/** Represents the storage type for OCR grid data. */
export type OcrGridDataStorage = IStorage<OcrGridData>;

/** Defines the structure of OCR grid data. */
export type OcrGridData = {
  /** The displayed value or null if not displayed. */
  displayed: string | null;
  /** The actual value, can be storage, primitive, or null. */
  value: IStorage | Primitive | null;
  /** Indicates whether the data has been modified. */
  modified: boolean;
  /** Reference(-s) on linked item(-s) (unique identifier of item(-s)). */
  refs: number[] | number | null;
};

/**
 * @helper
 */
export namespace OcrGridDataConverter {
  /**
   * Serializes OCR grid data storage into a JSON string.
   * @param storage The OCR grid data storage to serialize.
   * @returns The serialized JSON string.
   */
  export function serialize(storage: OcrGridDataStorage): string {
    return TypedJsonConverter.serialize(storage);
  }

  /**
   * Deserializes a JSON string into OCR grid data storage.
   * @param json The JSON string to deserialize.
   * @returns The deserialized OCR grid data storage.
   */
  export function deserialize(json: string): OcrGridDataStorage {
    return TypedJsonConverter.deserialize<OcrGridDataStorage>(json, { useTypedField: false });
  }

  /**
   * Serializes OCR grid data storage to a card.
   * @param card The card to which the data will be serialized.
   * @param storage The OCR grid data storage to serialize.
   * @param raw Indicates whether to store the data in raw format. Default is `true`.
   * @returns The serialized JSON string.
   */
  export function serializeToCard(card: Card, storage: OcrGridDataStorage, raw = true): string {
    const json = serialize(storage);
    const fields = card.sections.tryGet('OcrOperations')?.fields;
    raw
      ? fields?.rawSet('Mapping', json, FieldType.String)
      : fields?.set('Mapping', json, FieldType.String);

    return json;
  }

  /**
   * Deserializes OCR grid data from a card.
   * @param card The card from which to deserialize the data.
   * @returns The deserialized OCR grid data storage.
   */
  export function deserializeFromCard(card: Card): OcrGridDataStorage {
    const json = card.sections.tryGet('OcrOperations')?.fields.getString('Mapping');
    return !!json ? deserialize(json) : {};
  }
}
