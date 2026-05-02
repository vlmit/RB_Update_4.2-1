import moment, { Moment } from 'moment';
import { computed, observable, runInAction } from 'mobx';
import { IBirthdayGroup, IBirthdayGroupDataSource, IUserBirthdayInfo } from './birthdayTypes';

export class BirthdayGroup implements IBirthdayGroup {
  //#region fields

  private readonly _dataSource: IBirthdayGroupDataSource;

  private readonly _name: string;

  private readonly _predicate: (date: Moment) => boolean;

  @observable.ref
  private _title: string;

  @observable.ref
  private _expanded: boolean;

  //#endregion

  //#region ctor

  constructor(
    dataSource: IBirthdayGroupDataSource,
    name: string,
    title: string,
    predicate: (date: Moment) => boolean,
    expanded = false
  ) {
    this._dataSource = dataSource;
    this._name = name;
    this._title = title;
    this._predicate = predicate;
    this._expanded = expanded;
  }

  //#endregion

  //#region props

  get name(): string {
    return this._name;
  }

  get title(): string {
    return this._title;
  }
  set title(value: string) {
    runInAction(() => {
      this._title = value;
    });
  }

  get expanded(): boolean {
    return this._expanded;
  }
  set expanded(value: boolean) {
    runInAction(() => {
      this._expanded = value;
    });
  }

  @computed
  get birthdays(): readonly IUserBirthdayInfo[] {
    const currentYear = moment().year();

    return this._dataSource.birthdays.filter(info => {
      //Проверка для дат на границах года
      const previousYearDate = moment(info.birthday).set('year', currentYear - 1);
      const currentYearDate = moment(info.birthday).set('year', currentYear);
      const nextYearDate = moment(info.birthday).set('year', currentYear + 1);

      return (
        this._predicate(currentYearDate) ||
        this._predicate(previousYearDate) ||
        this._predicate(nextYearDate)
      );
    });
  }

  //#endregion
}
