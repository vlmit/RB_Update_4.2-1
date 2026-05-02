import { localize } from '@tessa/application';
import { ICardTypeSelectorNode, ICardTypeSelectorNodeGroup } from './cardTypeSelectorTypes';
import { CardTypeSelectorViewModel } from './cardTypeSelectorViewModel';
import { CardTypeSelectorNodeBaseViewModel } from './cardTypeSelectorNodeBaseViewModel';

export class CardTypeSelectorNodeGroupViewModel
  extends CardTypeSelectorNodeBaseViewModel
  implements ICardTypeSelectorNodeGroup
{
  //#region fields

  protected _children: ICardTypeSelectorNode[] = [];

  //#endregion

  //#region ctor

  constructor(parent: CardTypeSelectorViewModel, id: string, caption: string) {
    super(parent, id, caption);
  }

  //#endregion

  //#region props

  get children(): ICardTypeSelectorNode[] {
    if (this._parent.search.length === 0) {
      return this._children;
    }

    return this._children.filter(node => {
      return localize(node.caption).toLowerCase().includes(this._parent.search.toLowerCase());
    });
  }
  set children(value: ICardTypeSelectorNode[]) {
    this._children = value;
  }

  //#endregion
}
