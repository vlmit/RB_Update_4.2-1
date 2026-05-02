import React from 'react';
import { ICardTypeSelectorNodeBase } from './cardTypeSelectorTypes';
import { CardTypeSelectorViewModel } from './cardTypeSelectorViewModel';

export class CardTypeSelectorNodeBaseViewModel implements ICardTypeSelectorNodeBase {
  //#region fields

  protected readonly _parent: CardTypeSelectorViewModel;
  protected readonly _id: string;
  protected readonly _caption: string;

  //#endregion

  //#region ctor

  constructor(parent: CardTypeSelectorViewModel, id: string, caption: string) {
    this._parent = parent;
    this._id = id;
    this._caption = caption;
  }

  //#endregion

  //#region props

  get id(): string {
    return this._id;
  }

  get caption(): string {
    return this._caption;
  }

  //#endregion

  //#region methods

  handleClick(_e: React.MouseEvent): void {
    this._parent.handleNodeClick(this);
  }

  handleDoubleClick(_e: React.MouseEvent): void {
    this._parent.handleNodeDoubleClick(this);
  }

  //#endregion
}
