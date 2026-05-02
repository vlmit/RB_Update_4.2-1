import { observable, runInAction } from 'mobx';
import { promiseNoRace } from '@tessa/core';
import { localize } from '@tessa/application';
import { TagClickMode, TagInfo } from '@tessa/platform';
import {
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetBase,
  DashboardWidgetDisplayType,
  DashboardWidgetHeader,
  DashboardWidgetPeriodicRefresher,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits,
  DashboardWidgetConfigurationStorage
} from 'tessa/ui/dashboard';
import { Alert, AlertSize, AlertType } from 'tessa/ui/alerts';
import { DefaultWidgetNames } from '../widgetNames';
import { ITagData, ITagDataManager } from './tagTypes';
import { TagWidgetSettings } from './tagWidgetSettings';

/** Виджет "Тег". */
export class TagWidget extends DashboardWidgetBase<TagWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TagWidget}.
   * @param _tagDataManager Менеджер для получения и отображения данных о теге.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   */
  constructor(
    private readonly _tagDataManager: ITagDataManager,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new TagWidgetSettings()
  ) {
    super({
      type: DefaultWidgetNames.Tag,
      settings,
      dashboard,
      id,
      initialSize: TagWidget.initialWidgetSize,
      sizeLimits: TagWidget.widgetSizeLimits
    });

    this.periodicRefresher = new DashboardWidgetPeriodicRefresher(
      this.dashboard,
      TagWidget._refreshPeriod,
      async () => await this.refresh()
    );
  }

  //#endregion

  //#region fields

  @observable.ref
  private _tagData: ITagData | null = null;

  private static _refreshPeriod = 3_600_000; // 1 час

  //#endregion

  //#region properties

  readonly periodicRefresher: DashboardWidgetPeriodicRefresher;

  get tagData(): ITagData | null {
    return this._tagData;
  }
  protected set tagData(value: ITagData | null) {
    runInAction(() => (this._tagData = value));
  }

  get caption(): string {
    return this.settings.caption ?? this.settings.tag?.name ?? '';
  }

  get displayType(): DashboardWidgetDisplayType {
    return 'full-space';
  }

  get splashVisibility(): boolean {
    return false;
  }

  get background(): number | null {
    return null;
  }

  get header(): DashboardWidgetHeader | null {
    return null;
  }

  //#endregion

  //#region static fields

  static readonly unknownTagData: ITagData = TagWidget.createUnknownTagData();

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: { columnsCount: 2, rowsCount: 1 },
    md: { columnsCount: 3, rowsCount: 1 }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: { minColumnsCount: 1, minRowsCount: 1 }
  };

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    this.periodicRefresher.start(true);
  }

  protected override disposeCore(): void {
    super.disposeCore();
    this.periodicRefresher.dispose();
    this.tagData = null;
  }

  protected override async afterApplyConfiguration(
    _configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    await this.refresh();
    this.periodicRefresher.setLastRefresh(Date.now());
  }

  //#endregion

  //#region public methods

  /** Выполняет обновление отображаемых данных в виджете. */
  async refresh(): Promise<void> {
    await this.refreshInternal();
  }

  /** Действие, выполняемое при клике по виджету. */
  async action(): Promise<void> {
    await this.actionInternal();
  }

  //#endregion

  //#region private methods

  private readonly actionInternal = promiseNoRace(async () => {
    if (!this.tagData) {
      await Alert.show({
        duration: 3000,
        size: AlertSize.Standard,
        type: AlertType.Warning,
        text: localize('$Dashboard_Widget_Tag_Messages_NotFound', this.caption)
      });
    } else if (this.settings.tag) {
      await this._tagDataManager.showTagData(this.settings.tag, this.settings.displayTypes);
    }
  });

  private readonly refreshInternal = promiseNoRace(async () => {
    if (!this.settings.tag) {
      this.tagData = null;
      return;
    }

    this.tagData = await this._tagDataManager.getTagData(
      this.settings.tag,
      this.settings.displayTypes
    );
  });

  private static createUnknownTagData(): ITagData {
    const tagInfo = new TagInfo();
    tagInfo.clickMode = TagClickMode.No;
    tagInfo.background = -8355712; // gray
    return { tagInfo, recordsCount: 0 };
  }

  //#endregion
}
