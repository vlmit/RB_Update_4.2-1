import { observable, runInAction } from 'mobx';
import { localize } from '@tessa/application';
import { debounce, List, promiseNoRace } from '@tessa/core';
import { IUserInfo, IUserInfoRepository } from '@tessa/platform';
import { Visibility } from 'tessa/platform/visibility';
import {
  ContainerDashboardWidgetBase,
  ContainerDashboardWidgetSettingsBase,
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetHeader,
  DashboardWidgetConfigurationStorage,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits
} from 'tessa/ui/dashboard';
import { UIButton } from 'tessa/ui/uiButton';
import { ISearchBoxViewModel } from 'ui/searchBox/definitions';
import { SearchBoxViewModel } from 'ui/searchBox/searchBoxViewModel';
import { DefaultWidgetNames } from '../widgetNames';
import { IUserInfoProvider } from './usersTypes';

/** Виджет "Справочник сотрудников". */
export class UsersWidget extends ContainerDashboardWidgetBase {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link UsersWidget}.
   * @param _userInfoProvider Провайдер для получения информации о днях пользователях.
   * @param _userInfoRepository Репозиторий для получения информации о пользователях.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   */
  constructor(
    private readonly _userInfoProvider: IUserInfoProvider,
    private readonly _userInfoRepository: IUserInfoRepository,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new ContainerDashboardWidgetSettingsBase()
  ) {
    super({
      type: DefaultWidgetNames.Users,
      settings,
      dashboard,
      id,
      header: new DashboardWidgetHeader(settings),
      initialSize: UsersWidget.initialWidgetSize,
      sizeLimits: UsersWidget.widgetSizeLimits
    });

    this._users = List.create<IUserInfo>({ observable: true });

    this.search = new SearchBoxViewModel('UsersWidget');
    this.search.onChange = this.handleFilterChanged;
    this.search.spyglass = 'icon';
    this.search.placeholder = '$Dashboard_Widget_Users_Filter';

    this.overflow = UIButton.create({
      name: 'Overflow',
      caption: '$Dashboard_Widget_Users_ShowMore',
      type: 'normal',
      theme: 'secondary',
      stretch: true,
      visibility: () => (this._overflowAllow ? Visibility.Visible : Visibility.Collapsed),
      buttonAction: async () => await this.refresh()
    });
  }

  //#endregion

  //#region fields

  private readonly _users: List<IUserInfo>;

  @observable.ref
  private _message: string | null = null;

  @observable.ref
  private _overflowAllow = false;

  @observable.ref
  private _overflowIndex = 1;

  //#endregion

  //#region properties

  get users(): ReadonlyArray<IUserInfo> {
    return this._users;
  }

  get message(): string | null {
    return this._message;
  }

  readonly search: ISearchBoxViewModel;

  readonly overflow: UIButton;

  //#endregion

  //#region static fields

  static readonly minFilterLength = 3;

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: { columnsCount: 4, rowsCount: 3 },
    md: { columnsCount: 5, rowsCount: 5 }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: { minColumnsCount: 2, minRowsCount: 1 },
    md: { minColumnsCount: 4, minRowsCount: 2 }
  };

  //#endregion

  //#region base overrides

  protected override disposeCore(): void {
    super.disposeCore();
    this._users.dispose();
    this.search.dispose();
  }

  protected override async afterApplyConfiguration(
    _configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    await this.refresh();
  }

  //#endregion

  //#region methods

  /** Выполняет обновление отображаемых данных в виджете. */
  async refresh(): Promise<void> {
    await this.refreshInternal();
  }

  //#endregion

  //#region private methods

  private setContext(overflow: boolean, index?: number, users?: ReadonlyArray<IUserInfo>): void {
    runInAction(() => {
      this.setOverflow(overflow, index);
      this.setUsers(users);
      this.setMessage();
    });
  }

  private setUsers(users?: ReadonlyArray<IUserInfo>): void {
    users?.length
      ? this._overflowIndex > 1
        ? this._users.push(...users)
        : this._users.replaceWith(users)
      : this._users.clear();
  }

  private setMessage(): void {
    if (this.search.value.length < UsersWidget.minFilterLength) {
      this._message = localize('$Dashboard_Widget_Users_Filter_Hint');
    } else if (this._users.length === 0) {
      this._message = localize('$Dashboard_Widget_Users_NoDates');
    } else {
      this._message = null;
    }
  }

  private setOverflow(overflow: boolean, index?: number): void {
    this._overflowAllow = overflow;
    this._overflowIndex = index ?? (overflow ? this._overflowIndex + 1 : this._overflowIndex);
  }

  private refreshWithDebounce = debounce(async () => await this.refresh(), 200);

  private refreshInternal = promiseNoRace(async () => {
    const { users, overflow } = await this._userInfoProvider.getInfo({
      filter: this.search.value,
      index: this._overflowIndex
    });

    if (users.length === 0) {
      this.setContext(false, 1);
      return;
    }

    const userIds = users.map(b => b.userId);
    const usersInfo = await this._userInfoRepository.getUsersInfo(userIds);

    this.setContext(overflow, undefined, usersInfo);
  });

  private handleFilterChanged = async (searchText: string): Promise<void> => {
    this.search.value = searchText;
    this.setContext(false, 1);
    await this.refreshWithDebounce();
  };

  //#endregion
}
