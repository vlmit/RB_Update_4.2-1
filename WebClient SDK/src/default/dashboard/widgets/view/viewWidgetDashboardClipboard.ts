import { computed, observable, runInAction } from 'mobx';
import { injectable } from '@tessa/application';
import {
  IViewWidgetDashboardClipboard,
  IViewWidgetDashboardClipboardSettings
} from './viewWidgetTypes';

@injectable()
export class ViewWidgetDashboardClipboard implements IViewWidgetDashboardClipboard {
  //#region fields

  @observable.ref
  private _settings: IViewWidgetDashboardClipboardSettings | null;

  //#endregion

  //#region props

  @computed
  get hasSavedSettings(): boolean {
    return !!this._settings;
  }

  //#endregion

  //#region methods

  async saveSettings(settings: IViewWidgetDashboardClipboardSettings): Promise<void> {
    runInAction(() => {
      this._settings = settings;
    });
  }

  async getSettings(): Promise<IViewWidgetDashboardClipboardSettings | null> {
    return this._settings;
  }

  async removeSettings(): Promise<void> {
    runInAction(() => {
      this._settings = null;
    });
  }

  //#endregion
}
