import { ViewRequestParameter } from '@tessa/platform';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { ColumnsSettings } from 'tessa/ui/views/settings/columnSettings';

export interface IViewWidgetDashboardClipboardSettings {
  viewAlias: string;
  viewCaption?: string;
  parameters: ViewRequestParameter[];
  columnSettings: ColumnsSettings | null;
}

export interface IViewWidgetDashboardClipboard {
  readonly hasSavedSettings: boolean;
  saveSettings(settings: IViewWidgetDashboardClipboardSettings): Promise<void>;
  getSettings(): Promise<IViewWidgetDashboardClipboardSettings | null>;
  removeSettings(): Promise<void>;
}

export interface IViewWidgetClipboardFilter {
  filter(viewComponent: IWorkplaceViewComponent): boolean;
}
