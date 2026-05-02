import { observable, runInAction } from 'mobx';
import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ContainerDashboardWidgetSettingsBase } from 'tessa/ui/dashboard';
import { NamedReference } from '../../common/namedReference';

/** Объект с настройками виджета {@link BirthdayWidget}. */
export class BirthdayWidgetSettings extends ContainerDashboardWidgetSettingsBase {
  //#region keys

  /** @category Static Keys */
  static readonly departmentsKey = 'Departments';

  /** @category Static Keys */
  static readonly includeSubsidiaryDepartmentsKey = 'IncludeSubsidiaryDepartments';

  /** @category Static Keys */
  static readonly showUpcomingKey = 'ShowUpcoming';

  /** @category Static Keys */
  static readonly daysAfterKey = 'DaysAfter';

  /** @category Static Keys */
  static readonly showPastKey = 'ShowPast';

  /** @category Static Keys */
  static readonly daysBeforeKey = 'DaysBefore';

  //#endregion

  //#region fields

  private _departments: NamedReference[] = observable.array();

  @observable.ref
  private _includeSubsidiaryDepartments = false;

  @observable.ref
  private _showPast = false;

  @observable.ref
  private _daysAfter: number | null = null;

  @observable.ref
  private _showUpcoming = false;

  @observable.ref
  private _daysBefore: number | null = null;

  //#endregion

  //#region properties

  /** Сотрудники из подразделений. */
  get departments(): NamedReference[] {
    return this._departments;
  }
  private set departments(value: NamedReference[]) {
    this._departments = observable.array(value);
  }

  /** Включая дочерние подразделения. */
  get includeSubsidiaryDepartments(): boolean {
    return this._includeSubsidiaryDepartments;
  }
  set includeSubsidiaryDepartments(value: boolean) {
    runInAction(() => {
      this._includeSubsidiaryDepartments = value;
    });
  }

  /** Отображать прошедшие. */
  get showPast(): boolean {
    return this._showPast;
  }
  set showPast(value: boolean) {
    runInAction(() => {
      this._showPast = value;
    });
  }

  /** Дней после дня рождения. */
  get daysAfter(): number | null {
    return this._daysAfter;
  }
  set daysAfter(value: number | null) {
    runInAction(() => {
      this._daysAfter = value;
    });
  }

  /** Отображать предстоящие. */
  get showUpcoming(): boolean {
    return this._showUpcoming;
  }
  set showUpcoming(value: boolean) {
    runInAction(() => {
      this._showUpcoming = value;
    });
  }

  /** Дней до дня рождения. */
  get daysBefore(): number | null {
    return this._daysBefore;
  }
  set daysBefore(value: number | null) {
    runInAction(() => {
      this._daysBefore = value;
    });
  }

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.set(
      BirthdayWidgetSettings.departmentsKey,
      this.departments?.map(x => x.serializeToStorage({}, context))
    )
      .setBoolean(
        BirthdayWidgetSettings.includeSubsidiaryDepartmentsKey,
        this.includeSubsidiaryDepartments
      )
      .setBoolean(BirthdayWidgetSettings.showPastKey, this.showPast)
      .setInt(BirthdayWidgetSettings.daysAfterKey, this.daysAfter)
      .setBoolean(BirthdayWidgetSettings.showUpcomingKey, this.showUpcoming)
      .setInt(BirthdayWidgetSettings.daysBeforeKey, this.daysBefore);

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.departments =
      sa.tryGetList(BirthdayWidgetSettings.departmentsKey, x =>
        new NamedReference().deserializeFromStorage(x, context)
      ) ?? [];
    this.includeSubsidiaryDepartments = sa.tryGetBooleanOrDefault(
      BirthdayWidgetSettings.includeSubsidiaryDepartmentsKey
    );
    this.showPast = sa.tryGetBooleanOrDefault(BirthdayWidgetSettings.showPastKey);
    this.daysAfter = sa.tryGetInt(BirthdayWidgetSettings.daysAfterKey);
    this.showUpcoming = sa.tryGetBooleanOrDefault(BirthdayWidgetSettings.showUpcomingKey);
    this.daysBefore = sa.tryGetInt(BirthdayWidgetSettings.daysBeforeKey);
  }

  //#endregion
}
