import { observable, runInAction } from 'mobx';
import { Visibility } from 'tessa/platform';
import { CardTypeBlock, CardTypeEntryControl } from 'tessa/cards/types';
import { CardHelpMode } from 'tessa/ui/cards';
import {
  IViewControlInitializationStrategy,
  ViewControlInitializationStrategy,
  ViewControlViewModelBase
} from 'tessa/ui/cards/controls';
import { UnknownBlockViewModel } from 'tessa/ui/cards/blocks';
import {
  ContainerDashboardWidgetBase,
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetHeader,
  DashboardWidgetPeriodicRefresher,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits,
  DashboardWidgetStorage,
  IDashboardWidgetHeader,
  DashboardWidgetConfigurationStorage
} from 'tessa/ui/dashboard';
import { ChangedSettings } from 'tessa/ui/views/settings/changedSettings';
import { ViewWidgetTableColumnSettingsProvider } from './viewWidgetTableColumnSettingsProvider';
import { ViewWidgetSettingsBase } from './viewWidgetSettingsBase';

export abstract class ViewWidgetBase<
  TSettings extends ViewWidgetSettingsBase = ViewWidgetSettingsBase,
  THeader extends IDashboardWidgetHeader = DashboardWidgetHeader
> extends ContainerDashboardWidgetBase<TSettings, THeader> {
  //#region ctor

  constructor(
    name: string,
    settings: TSettings,
    header: THeader,
    dashboard: DashboardViewModel,
    id?: string,
    initialSize: DashboardLayoutValue<DashboardWidgetSize> | null = ViewWidgetBase.initialWidgetSize,
    sizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> | null = ViewWidgetBase.widgetSizeLimits
  ) {
    super({
      type: name,
      settings,
      dashboard,
      header,
      id,
      initialSize,
      sizeLimits
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
  protected _control: ViewControlViewModelBase | null = null;

  protected _settingsProvider: ViewWidgetTableColumnSettingsProvider | null;

  @observable.ref
  protected _error: string | null;

  protected _refreshPeriod = 15 * 60 * 1000;

  //#endregion

  //#region props

  get control(): ViewControlViewModelBase | null {
    return this._control;
  }

  get error(): string | null {
    return this._error;
  }
  protected set error(value: string | null) {
    runInAction(() => {
      this._error = value;
    });
  }

  readonly periodicRefresher: DashboardWidgetPeriodicRefresher;

  //#endregion

  //#region static fields

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: {
      columnsCount: 4,
      rowsCount: 3
    },
    xs: {
      columnsCount: 4,
      rowsCount: 4
    },
    md: {
      columnsCount: 5,
      rowsCount: 4
    },
    xxxl: {
      columnsCount: 8,
      rowsCount: 7
    }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: {
      minColumnsCount: 2,
      minRowsCount: 2
    },
    md: {
      minColumnsCount: 3,
      minRowsCount: 3
    }
  };

  //#endregion

  //#region public methods

  override async applyChanges(changes: DashboardWidgetStorage): Promise<void> {
    super.applyChanges(changes);

    if (!this._control?.table) {
      return;
    }

    const settings = new ViewWidgetTableColumnSettingsProvider(
      () => this._control!,
      changes.content ?? {}
    ).getSettings();

    const currentSettings = this._control.columnSettings.getSettings();

    const changedSettings = ChangedSettings.getChanges(currentSettings, settings);
    this._control.columnSettings.setSettings(settings);

    if (!changedSettings.groupingChanged && !changedSettings.sortingChanged) {
      this._control.table.rebuild();
      return;
    }

    this._control.table.groupingColumn = null;
    this._control.table.rebuild();
    await this._control.table.viewComponent.refresh();
  }

  protected override async afterApplyConfiguration(
    configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    await super.afterApplyConfiguration(configuration);

    if (this.control?.table) {
      this.control.table.horizontalScroll = !this.settings.disableHorizontalScroll;
    }
  }

  async refresh(): Promise<void> {
    await this._control?.refresh();
  }

  //#endregion

  //#region protected methods

  protected override getStorageCore(properties: DashboardWidgetStorage): DashboardWidgetStorage {
    const columnSettings = this._settingsProvider?.widgetContent;
    properties.content = columnSettings ?? null;

    return properties;
  }

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();

    const fakeControlType = this.getFakeCardTypeEntryControl();
    if (!fakeControlType) {
      return;
    }

    const cardTypeBlock = new CardTypeBlock();
    const block = new UnknownBlockViewModel(
      cardTypeBlock,
      'View',
      Visibility.Collapsed,
      false,
      false,
      false
    );
    block.initialize();

    this._settingsProvider = new ViewWidgetTableColumnSettingsProvider(
      () => control,
      this._initialStorage?.content ?? {},
      async () => {
        this.hasChanges = true;
        if (this._attached) {
          await this.saveChanges();
        }
      }
    );

    const control = new ViewControlViewModelBase(
      fakeControlType,
      undefined,
      this._settingsProvider
    );
    control.helpMode = CardHelpMode.None;
    control.captionVisibility = Visibility.Collapsed;
    control.useSubgrid = false;
    control.setBlock(block);

    control.initializeStrategy(this.getViewControlInitializationStrategy());
    control.initializeDefaultDoubleClickAction(null);

    if (this.control?.error) {
      this.error = this.control?.error;
    } else if (!control.viewMetadata) {
      this.error = '$Views_NotAvailable_ErrorMessage';
    } else if (control.table) {
      control.table.horizontalScroll = !this.settings.disableHorizontalScroll;
    }

    await this.modifyViewControl(control);

    runInAction(() => (this._control = control));

    if (!this.error) {
      control.onRefreshed.add(() => {
        this.periodicRefresher?.setLastRefresh(Date.now());
      });
      this.periodicRefresher?.start();
    }
  }

  protected override disposeCore(): void {
    super.disposeCore();
    this._control?.dispose();
    this.periodicRefresher?.dispose();
  }

  protected abstract getFakeCardTypeEntryControl(): CardTypeEntryControl | null;

  protected getViewControlInitializationStrategy(): IViewControlInitializationStrategy {
    return new ViewControlInitializationStrategy();
  }

  protected async modifyViewControl(_viewControl: ViewControlViewModelBase): Promise<void> {}

  //#endregion
}
