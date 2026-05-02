import { createAtom, observable, runInAction } from 'mobx';
import { FieldType, StorageSerializable, TypedJsonConverter } from '@tessa/core';
import { Card } from '@tessa/platform';
import {
  IRichTextBoxAttachmentContainer,
  IRichTextBoxDataSource,
  RichStorage
} from 'ui/richTextBox';
import { FakeCardArticleAttachmentContainer } from './fakeCardArticleAttachmentContainer';
import { FakeCardService } from './fakeCardService';

/** Fake card data source for richTextBox. */
export class FakeCardArticleDataSource implements IRichTextBoxDataSource {
  //#region ctor

  constructor(
    private _card: Card,
    private readonly _cardService: FakeCardService
  ) {
    this.attachmentsContainer = new FakeCardArticleAttachmentContainer(
      () => this._card,
      this._cardService
    );
  }

  //#endregion

  //#region fields

  @observable.ref
  private _possiblyChanged = false;

  private _atom = createAtom('fakeCardAtom');

  //#endregion

  //#region props

  readonly attachmentsContainer: IRichTextBoxAttachmentContainer;

  get card(): Card {
    return this._card;
  }
  set card(value: Card) {
    this._card = value;
    this._atom.reportChanged();
  }

  //#endregion

  //#region IRichTextBoxDataSource implementation

  get possiblyChanged(): boolean {
    return this._possiblyChanged;
  }
  set possiblyChanged(value: boolean) {
    runInAction(() => (this._possiblyChanged = value));
  }

  getValue(): RichStorage {
    this._atom.reportObserved();

    const json =
      this.card.sections.get('section').fields.getString('field') ?? '{"Text": "<p></p>"}';
    return StorageSerializable.deserialize(RichStorage, TypedJsonConverter.deserialize(json));
  }

  setValue(value: RichStorage): void {
    const contentSection = this.card.sections.get('section');
    contentSection.fields.set(
      'field',
      TypedJsonConverter.serialize(value.serializeToStorage()),
      FieldType.String
    );

    this._atom.reportChanged();
  }

  //#endregion
}
