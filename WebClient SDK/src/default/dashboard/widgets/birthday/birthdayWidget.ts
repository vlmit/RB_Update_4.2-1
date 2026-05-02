import moment from 'moment';
import { observable, runInAction } from 'mobx';
import { promiseNoRace } from '@tessa/core';
import { IUserInfoRepository } from '@tessa/platform';
import {
  ContainerDashboardWidgetBase,
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetHeader,
  DashboardWidgetPeriodicRefresher,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits,
  DashboardWidgetConfigurationStorage
} from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../widgetNames';
import { BirthdayWidgetSettings } from './birthdayWidgetSettings';
import { BirthdayGroup } from './birthdayGroup';
import {
  IBirthdayGroupDataSource,
  IBirthdayInfoProvider,
  IUserBirthdayInfo
} from './birthdayTypes';

/** Виджет "Дни рождения". */
export class BirthdayWidget
  extends ContainerDashboardWidgetBase<BirthdayWidgetSettings>
  implements IBirthdayGroupDataSource
{
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link BirthdayWidget}.
   * @param _infoProvider Провайдер для получения информации о днях рождения пользователей.
   * @param _userInfoRepository Репозиторий для получения информации о пользователях.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   */
  constructor(
    private readonly _infoProvider: IBirthdayInfoProvider,
    private readonly _userInfoRepository: IUserInfoRepository,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new BirthdayWidgetSettings()
  ) {
    super({
      type: DefaultWidgetNames.Birthday,
      settings,
      dashboard,
      id,
      header: new DashboardWidgetHeader(settings),
      initialSize: BirthdayWidget.initialWidgetSize,
      sizeLimits: BirthdayWidget.widgetSizeLimits
    });

    this.periodicRefresher = new DashboardWidgetPeriodicRefresher(
      this.dashboard,
      this._refreshPeriod,
      async () => await this.refresh()
    );
  }

  //#endregion

  //#region fields

  @observable.ref
  private _birthdays: ReadonlyArray<IUserBirthdayInfo> = [];

  @observable.ref
  private _birthdayGroups: BirthdayGroup[] = [];

  private _refreshPeriod = 6 * 60 * 60 * 1000; // 6 часов

  //#endregion

  //#region properties

  /** Информация о днях рождения пользователей. */
  get birthdays(): ReadonlyArray<IUserBirthdayInfo> {
    return this._birthdays;
  }
  protected set birthdays(value: ReadonlyArray<IUserBirthdayInfo>) {
    runInAction(() => (this._birthdays = value));
  }

  get birthdayGroups(): BirthdayGroup[] {
    return this._birthdayGroups;
  }
  set birthdayGroups(value: BirthdayGroup[]) {
    runInAction(() => (this._birthdayGroups = value));
  }

  readonly periodicRefresher: DashboardWidgetPeriodicRefresher;

  //#endregion

  //#region static fields

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: {
      columnsCount: 4,
      rowsCount: 3
    },
    md: {
      columnsCount: 5,
      rowsCount: 5
    }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: {
      minColumnsCount: 2,
      minRowsCount: 1
    },
    md: {
      minColumnsCount: 4,
      minRowsCount: 2
    }
  };

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();

    this.initBirthdayGroups();

    this.periodicRefresher.start(true);
  }

  protected override disposeCore(): void {
    super.disposeCore();
    this.birthdays = [];
    this.birthdayGroups = [];
    this.periodicRefresher.dispose();
  }

  protected override async afterApplyConfiguration(
    _configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    await this.refresh();
    this.periodicRefresher.setLastRefresh(Date.now());
  }

  protected initBirthdayGroups(): void {
    const today = moment();
    const tomorrow = moment().add(1, 'day');

    this.birthdayGroups = [
      new BirthdayGroup(
        this,
        'today',
        '$Dashboard_Widget_Birthday_Groups_Today',
        date => {
          return date.isSame(today, 'date');
        },
        true
      ),
      new BirthdayGroup(this, 'tomorrow', '$Dashboard_Widget_Birthday_Groups_Tomorrow', date => {
        return date.isSame(tomorrow, 'date');
      })
    ];

    this.birthdayGroups.push(
      new BirthdayGroup(this, 'upcoming', '$Dashboard_Widget_Birthday_Groups_Soon', date => {
        if (!this.settings.showUpcoming) {
          return false;
        }

        const upcomingDate = moment().add(this.settings.daysBefore, 'day');
        return date.isAfter(tomorrow, 'day') && date.isSameOrBefore(upcomingDate, 'day');
      })
    );

    this.birthdayGroups.push(
      new BirthdayGroup(this, 'past', '$Dashboard_Widget_Birthday_Groups_Past', date => {
        if (!this.settings.showPast) {
          return false;
        }

        const previousDate = moment().subtract(this.settings.daysAfter, 'day');
        return date.isSameOrAfter(previousDate, 'day') && date.isBefore(today, 'day');
      })
    );
  }

  //#endregion

  //#region methods

  /** Выполняет обновление отображаемых данных в виджете. */
  async refresh(): Promise<void> {
    await this.refreshInternal();
  }

  //#endregion

  //#region private methods

  private refreshInternal = promiseNoRace(async () => {
    const birthdays = await this._infoProvider.getInfo({
      departments: this.settings.departments.map(d => d.id),
      includeSubsidiaryDepartments: this.settings.includeSubsidiaryDepartments,
      daysAfter: this.settings.daysAfter,
      daysBefore: this.settings.daysBefore
    });

    if (birthdays.length === 0) {
      this.birthdays = [];

      return;
    }

    const userIds = birthdays.map(b => b.userId);
    const usersInfo = await this._userInfoRepository.getUsersInfo(userIds);

    this.birthdays = usersInfo.map<IUserBirthdayInfo>(info => {
      const birthday = birthdays.find(b => b.userId === info.id)!.birthday;

      return {
        user: info,
        birthday: birthday
      };
    });
  });

  //#endregion
}
