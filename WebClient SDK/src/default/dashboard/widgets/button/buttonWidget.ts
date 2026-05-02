import {
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetBase,
  DashboardWidgetDisplayType,
  DashboardWidgetHeader,
  DashboardWidgetPaletteHelper,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits
} from 'tessa/ui/dashboard';
import { ButtonWidgetSettings } from './buttonWidgetSettings';

export class ButtonWidget<
  TSettings extends ButtonWidgetSettings
> extends DashboardWidgetBase<TSettings> {
  //#region constructors

  constructor(
    name: string,
    settings: TSettings,
    dashboard: DashboardViewModel,
    id?: string,
    initialSize: DashboardLayoutValue<DashboardWidgetSize> | null = ButtonWidget.initialWidgetSize,
    sizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> | null = ButtonWidget.widgetSizeLimits
  ) {
    super({
      type: name,
      settings,
      dashboard,
      id,
      initialSize,
      sizeLimits
    });
  }

  //#endregion

  //#region properties

  get color(): number | null {
    return this.settings.color;
  }

  get caption(): string | null {
    return this.settings.caption;
  }

  get captionHidden(): boolean {
    return this.settings.captionHidden;
  }

  get captionColor(): number | null {
    return this.settings.captionColor;
  }

  get icon(): string | null {
    return this.settings.icon;
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

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: {
      columnsCount: 2,
      rowsCount: 1
    },
    md: {
      columnsCount: 3,
      rowsCount: 1
    }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: {
      minColumnsCount: 1,
      minRowsCount: 1
    }
  };

  //#endregion

  //#region methods

  async handleClick(): Promise<void> {
    await this.handleClickCore();
  }

  protected async handleClickCore(): Promise<void> {}

  override async handleAttaching(): Promise<void> {
    super.handleAttaching();

    if (!this._initialStorage) {
      this.settings.color = DashboardWidgetPaletteHelper.getNextOpaqueColor(
        this.dashboard.widgets.length
      );
      this.settings.captionColor = DashboardWidgetPaletteHelper.getOpaqueFontColor();
    }
  }

  //#endregion
}
