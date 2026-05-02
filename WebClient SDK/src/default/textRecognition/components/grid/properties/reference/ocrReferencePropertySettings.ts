import { ICardModel } from 'tessa/ui/cards';
import { IOcrPropertySettings } from '../ocrPropertySettings';

/** Settings for the recognized reference property. */
export interface IOcrReferencePropertySettings extends IOcrPropertySettings {
  /** The card model accessible within the UI. */
  readonly model?: ICardModel;
}
