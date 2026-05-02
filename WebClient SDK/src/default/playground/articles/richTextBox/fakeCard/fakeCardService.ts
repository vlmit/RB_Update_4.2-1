import { Guid } from '@tessa/core';
import { Card, FileContentResolver, CardFileState } from '@tessa/platform';

export class FakeCardService {
  //#region ctor

  constructor() {
    this._card = new Card();
    this._card.id = Guid.newGuid();
    const contentSection = this._card.sections.add('section');
    contentSection.fields.set('field', null);
  }

  //#endregion

  //#region fields

  private _card: Card;

  private _content = new Map<string, () => Promise<File | null>>();

  //#endregion

  //#region methods

  async storeCardWithContent(card: Card, contentResolver: FileContentResolver): Promise<void> {
    this._card = card.clone();
    const clone = new Map([...this._content]);
    this._content.clear();

    for (let i = this._card.files.length - 1; i >= 0; i--) {
      const file = this._card.files[i];
      if (file.state === CardFileState.Deleted) {
        this._card.files.remove(file);
        continue;
      } else if (file.state === CardFileState.None) {
        this._content.set(file.rowId, clone.get(file.rowId)!);
        continue;
      }

      file.state = CardFileState.None;
      const content = await contentResolver(file.rowId);
      this._content.set(file.rowId, async () => content);
    }
  }

  async getCard(): Promise<Card> {
    return this._card.clone();
  }

  async getFileContent(fileId: string): Promise<File | null> {
    const getContent = this._content.get(fileId);
    return getContent ? await getContent() : null;
  }

  //#endregion
}
