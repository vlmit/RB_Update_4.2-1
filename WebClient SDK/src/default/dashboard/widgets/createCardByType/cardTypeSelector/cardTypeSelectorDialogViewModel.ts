import { computed } from 'mobx';
import { CardTypeSelectorViewModel } from './cardTypeSelectorViewModel';
import { isNodeLeafNode } from './cardTypeSelectorTypes';

export class CardTypeSelectorDialogViewModel {
  //#region fields

  private _cardTypeSelector: CardTypeSelectorViewModel;
  private _onClose?: (() => void) | null = null;

  private _canceled: boolean = false;

  //#endregion

  //#region ctor

  constructor(cardTypeSelector: CardTypeSelectorViewModel) {
    this._cardTypeSelector = cardTypeSelector;

    cardTypeSelector.onNodeDoubleClick.add(args => {
      if (isNodeLeafNode(args.node)) {
        this._onClose?.();
      }
    });
  }

  //#endregion

  //#region props

  get cardTypeSelector(): CardTypeSelectorViewModel {
    return this._cardTypeSelector;
  }

  @computed
  get cardTypeIsSelected(): boolean {
    return !!this._cardTypeSelector.selectedNode;
  }

  get canceled(): boolean {
    return this._canceled;
  }

  //#endregion

  //#region methods

  setCloseRequest = (onClose: () => void): void => {
    this._onClose = onClose;
  };

  select(): void {
    this._onClose?.();
  }

  cancel(): void {
    this._canceled = true;

    this._onClose?.();
  }

  //#endregion
}
