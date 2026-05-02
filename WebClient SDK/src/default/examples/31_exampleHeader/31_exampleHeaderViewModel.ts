import { observable, runInAction } from 'mobx';
import { ICardAdditionalContentViewModel } from 'tessa/ui/cards';

// Вью-модель для кастомного хэдера
// Должна реализовывать интерфейс ICardAdditionalContentViewModel
export class ExampleHeaderViewModel implements ICardAdditionalContentViewModel {
  @observable
  private _title: string;

  //#endregion

  //#region ctor

  constructor(title: string) {
    this._title = title;
  }

  //#endregion

  //#region props

  get title(): string {
    return this._title;
  }
  set title(title: string) {
    runInAction(() => {
      this._title = title;
    });
  }

  //#endregion

  //#region methods

  dispose(): void {}

  //#endregion
}
