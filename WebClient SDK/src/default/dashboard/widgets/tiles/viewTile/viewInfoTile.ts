import { localize } from '@tessa/application';
import { showMessage } from 'tessa/ui';
import {
  IDashboardWidgetTile,
  DashboardViewModel,
  PreviewWidgetTileViewModel
} from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../../widgetNames';
import { IViewWidgetDashboardClipboard } from '../../view/viewWidgetTypes';

export class ViewInfoTile implements IDashboardWidgetTile {
  //#region fields

  private readonly _clipboard: IViewWidgetDashboardClipboard;

  private readonly _localizedTitle: string;

  //#endregion

  //#region ctor

  constructor(clipboard: IViewWidgetDashboardClipboard) {
    this._clipboard = clipboard;
    this._localizedTitle = localize(DefaultWidgetNames.ViewTitle).toLowerCase();
  }

  //#endregion

  //#region props

  readonly order = 1;

  readonly name = 'ViewInfoTile';

  readonly isTemplate = false;

  readonly isShared = false;

  get visibility(): boolean {
    return !this._clipboard.hasSavedSettings;
  }

  //#endregion

  //#region  methods

  getViewModel(): object {
    const preview = new PreviewWidgetTileViewModel({
      image: 'images-emoji-clipboard',
      title: DefaultWidgetNames.ViewTitle,
      description: '$Dashboard_Widget_View_Description'
    });

    return preview;
  }

  async handleClick(_e: React.MouseEvent, _dashboard: DashboardViewModel): Promise<void> {
    await showMessage('$Dashboard_Widget_View_InfoTile_Message');
  }

  filter(search: string): boolean {
    return this._localizedTitle.includes(search.toLowerCase());
  }

  dispose(): void {}

  //#endregion
}
