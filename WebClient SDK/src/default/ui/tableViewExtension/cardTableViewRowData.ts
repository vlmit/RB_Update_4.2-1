import { computed } from 'mobx';
import { TypedField } from '@tessa/core';
import { Card, CardRow } from '@tessa/platform';
import { GridColumnInfo } from 'tessa/ui/cards/controls';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CardTableViewRowData implements ReadonlyMap<string, any> {
  //#region ctor

  constructor(
    readonly card: Card,
    readonly cardRow: CardRow,
    readonly columnInfos: Map<string, GridColumnInfo>,
    readonly orderColumnName: string | null,
    readonly flagColumnName: string,
    readonly sectionName: string
  ) {}

  //#endregion

  //#region props

  @computed
  get rowId(): string {
    return this.cardRow.rowId;
  }

  @computed
  get order(): number {
    return this.orderColumnName ? this.cardRow.get<number>(this.orderColumnName)! : 0;
  }

  @computed
  get flag(): boolean {
    return this.flagColumnName ? !!this.cardRow.get(this.flagColumnName) : false;
  }
  set flag(value: boolean) {
    if (this.flagColumnName) {
      this.cardRow.set(this.flagColumnName, TypedField.createBoolean(value));
    }
  }

  //#endregion

  //#region Map members

  readonly [Symbol.toStringTag]: 'Map';

  forEach(
    callbackfn: (value: any, key: string, map: ReadonlyMap<string, any>) => void,
    thisArg?: any
  ): void {
    this.columnInfos.forEach(callbackfn, thisArg);
  }

  get(key: string): any | undefined {
    if (key === 'RowID') {
      return this.rowId;
    }

    if (this.orderColumnName && key === this.orderColumnName) {
      return this.order;
    }

    const columnInfo = this.columnInfos.get(key);
    if (columnInfo) {
      return columnInfo.getValue(this.cardRow, this.card).value;
    }
    return undefined;
  }

  has(key: string): boolean {
    return this.columnInfos.has(key);
  }

  get size(): number {
    return this.columnInfos.size;
  }

  [Symbol.iterator](): MapIterator<[string, any]> {
    return this.entries();
  }

  entries(): MapIterator<[string, any]> {
    return Iterator.from(this.columnInfos.entries()).map(([key, value]) => [
      key,
      value.getValue(this.cardRow, this.card).value
    ]);
  }

  keys(): MapIterator<string> {
    return this.columnInfos.keys();
  }

  values(): MapIterator<any> {
    return Iterator.from(this.columnInfos.values()).map(
      value => value.getValue(this.cardRow, this.card).value
    );
  }

  //#endregion
}
