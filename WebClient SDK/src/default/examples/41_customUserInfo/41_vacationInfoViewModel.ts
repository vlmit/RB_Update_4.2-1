import { computed, observable, runInAction } from 'mobx';
import { StorageHelper } from '@tessa/core';
import { IUserInfo } from '@tessa/platform';
import { IUserInfoFooter } from 'ui/userInfo/userInfoType';
import { UserInfoViewModel } from 'ui/userInfo/userInfoViewModel';

export class VacationInfoViewModel implements IUserInfoFooter {
  //#region ctor

  constructor(userInfoViewModel: UserInfoViewModel) {
    this.text = this.getText(userInfoViewModel.userInfo);
  }

  //#endregion

  //#region fields

  @observable.ref
  private _text: string | null;

  //#endregion

  //#region props

  get text(): string | null {
    return this._text;
  }
  private set text(value: string | null) {
    runInAction(() => {
      this._text = value;
    });
  }

  @computed
  get visibility(): boolean {
    // не показываем футер, если текст не заполнен
    return !!this.text;
  }

  //#endregion

  //#region methods

  dispose(): void {}

  async handleUserInfoChanged(userInfo: IUserInfo): Promise<void> {
    // заново вычислим текст, если поменялась модель
    this.text = this.getText(userInfo);
  }

  private getText(userInfo: IUserInfo | null): string | null {
    if (!userInfo) {
      return null;
    }

    // достаем информацию об отпуске из инфо модели IUserInfo, которую мы сами туда положили ранее
    const dateFrom = StorageHelper.tryGet<string>(userInfo?.info, 'vacationFrom');
    const dateTo = StorageHelper.tryGet<string>(userInfo?.info, 'vacationTo');

    if (!dateFrom || !dateTo) {
      return null;
    }

    return `Отпуск с ${dateFrom} по ${dateTo}`;
  }

  //#endregion
}
