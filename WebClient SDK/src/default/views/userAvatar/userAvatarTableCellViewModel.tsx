import { ITableCellViewModelCreateOptions, TableCellViewModel } from 'tessa/ui/views/content';
import { UserAvatarCellContent } from './userAvatarCellContent';
import { observable, runInAction } from 'mobx';
import { AvatarViewModel } from 'ui/avatar/avatarViewModel';

export type UserAvatarTableCellViewModelCreateOptions = {
  avatar: AvatarViewModel;
  content?: string;
};

export class UserAvatarTableCellViewModel extends TableCellViewModel {
  //#region fields

  private _avatar: AvatarViewModel;
  @observable.ref
  private _content?: string;

  //#endregion

  //#region ctor

  constructor(args: ITableCellViewModelCreateOptions & UserAvatarTableCellViewModelCreateOptions) {
    super(args);

    this._avatar = args.avatar;
    this._content = args.content;

    this._getContent = () => <UserAvatarCellContent viewModel={this} />;
  }

  //#endregion

  //#region properties

  get avatar(): AvatarViewModel {
    return this._avatar;
  }

  get content(): string | undefined {
    return this._content;
  }
  set content(value: string | undefined) {
    runInAction(() => {
      this._content = value;
    });
  }

  //#endregion
}
