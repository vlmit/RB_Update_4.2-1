import { computed } from 'mobx';
import { CardTypeSelectorViewModel } from './cardTypeSelectorViewModel';
import { CardTypeSelectorNodeBaseViewModel } from './cardTypeSelectorNodeBaseViewModel';
import { ICardTypeSelectorCardType } from './cardTypeSelectorTypes';

export class CardTypeSelectorNodeViewModel extends CardTypeSelectorNodeBaseViewModel {
  //#region fields

  private readonly _cardType: ICardTypeSelectorCardType;

  //#endregion

  //#region ctor

  constructor(parent: CardTypeSelectorViewModel, cardType: ICardTypeSelectorCardType) {
    super(
      parent,
      cardType.docTypeId ?? cardType.cardTypeId,
      cardType.docTypeTitle ?? cardType.cardTypeCaption
    );

    this._cardType = cardType;
  }

  //#endregion

  //#region props

  get cardType(): ICardTypeSelectorCardType {
    return this._cardType;
  }

  @computed
  get selected(): boolean {
    return this._parent.selectedNode === this;
  }

  //#endregion
}
