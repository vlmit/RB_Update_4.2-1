import { CardTypeEntryControl, SchemeDbType } from '@tessa/platform';
import { IStorage } from '@tessa/core';
import { IPropertySettings } from 'tessa/ui/propertyGrid';

/** Settings for a property control. */
export interface IOcrControlSettings extends IStorage {
  /** The alias of the property. */
  readonly alias: string;
  /** The data type of the control. */
  readonly schemeType: SchemeDbType;
  /**
   * An object describing the control's layout and properties,
   * used for binding to the fields of the card's string section.
   */
  readonly control: CardTypeEntryControl;
}

/** Settings for a recognized text property. */
export interface IOcrPropertySettings extends IPropertySettings, IOcrControlSettings {
  /**
   * The text displayed when the control requires a value but none is provided.
   * If `null` or an empty string, the default string from the validator will be used.
   */
  readonly requiredText?: string;
}
