import { ITextFieldDataSource } from 'ui/textField/textFieldTypes';

/** The data source for OCR text field. */
export interface IOcrTextFieldDataSource extends ITextFieldDataSource {
  /**
   * Retrieves the displayed value of the text field.
   * @returns The displayed value, or `null` if it is not set.
   */
  getDisplayed(): string | null;
  /**
   * Sets the displayed value of the text field.
   * @param value The new displayed value, or `null` to clear it.
   */
  setDisplayed(value: string | null): void;
  /**
   * Retrieves the reference(-s) on linked item(-s) (unique identifier of item(-s)).
   * @returns The reference(-s) on linked item(-s), or `null` if it is not set.
   */
  getRef(): number[] | number | null;
  /**
   * Sets the reference(-s) on linked item(-s) (unique identifier of item(-s)).
   * @param value The reference(-s) on linked item(-s), or `null` to clear it.
   */
  setRef(value: number[] | number | null): void;
  /**
   * Retrieves both the displayed and actual values of the text field.
   * @returns An object containing the displayed and actual values.
   */
  get(): { displayed: string | null; value: string | null };
  /**
   * Sets both the displayed and actual values of the text field.
   * @param displayed The displayed value to set, or `null` to clear it.
   * @param value The actual value to set, or `null` to clear it.
   */
  set(displayed: string | null, value: string | null): void;
  /** Clears both the displayed and actual values of the text field. */
  clear(): void;
}
