import { assertNotNull } from '@tessa/core';
import { localize } from '@tessa/application';
import { DashboardViewModel, IDashboardWidgetTile, IDashboardWidgetType } from 'tessa/ui/dashboard';
import { IViewWidgetDashboardClipboard } from '../../view/viewWidgetTypes';
import { ViewWidgetSettings } from '../../view/viewWidgetSettings';
import { DefaultWidgetNames } from '../../widgetNames';
import './viewClipboardTile.scss';

export class ViewClipboardTile implements IDashboardWidgetTile {
  //#region fields

  private readonly _widgetType: IDashboardWidgetType;

  private readonly _clipboard: IViewWidgetDashboardClipboard;

  private readonly _localizedTitle: string;

  //#endregion

  //#region ctor

  constructor(widgetType: IDashboardWidgetType, clipboard: IViewWidgetDashboardClipboard) {
    this._widgetType = widgetType;
    this._clipboard = clipboard;
    this._localizedTitle = localize(this._widgetType.descriptor.title).toLowerCase();
  }

  //#endregion

  //#region props

  get order(): number {
    return -1;
  }

  get name(): string {
    return this._widgetType.descriptor.id;
  }

  readonly isTemplate = false;

  readonly isShared = false;

  get visibility(): boolean {
    return this._clipboard.hasSavedSettings;
  }

  //#endregion

  //#region  methods

  getViewModel(): object {
    return assertNotNull(this._widgetType.getWidgetPreview());
  }

  async handleClick(_e: React.MouseEvent, dashboard: DashboardViewModel): Promise<void> {
    const clipboardSettings = await this._clipboard.getSettings();
    if (!clipboardSettings) {
      return;
    }

    const viewSettings = new ViewWidgetSettings();
    viewSettings.headerCaption =
      localize(clipboardSettings.viewCaption) ?? DefaultWidgetNames.ViewTitle;
    viewSettings.viewAlias = clipboardSettings.viewAlias;
    viewSettings.parameters = clipboardSettings.parameters;

    const widget = this._widgetType.createNewWidget(dashboard, {
      id: dashboard.generateWidgetIdentifier(),
      settings: viewSettings,
      content: clipboardSettings.columnSettings
    });

    await dashboard.addWidget(widget);
    await this._clipboard.removeSettings();
  }

  filter(search: string): boolean {
    return this._localizedTitle.includes(search.toLowerCase());
  }

  dispose(): void {}

  //#endregion
}
