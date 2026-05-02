import { inject, injectable } from '@tessa/application';
import {
  IDashboardWidgetTypeFactory$,
  IDashboardWidgetTile,
  IDashboardWidgetTilesProvider,
  IDashboardWidgetTypeFactory,
  IDashboardWidgetTilesProviderType,
  DashboardWidgetTilesProviderArgs
} from 'tessa/ui/dashboard';
import { IViewWidgetDashboardClipboard } from '../../view/viewWidgetTypes';
import { IViewWidgetDashboardClipboard$ } from '../../widgetInjects';
import { DefaultWidgetNames } from '../../widgetNames';
import { ViewClipboardTile } from './viewClipboardTile';
import { ViewInfoTile } from './viewInfoTile';

@injectable()
export class ViewWidgetTilesProvider implements IDashboardWidgetTilesProvider {
  // #region ctor

  constructor(
    @inject(IDashboardWidgetTypeFactory$)
    private readonly _widgetTypeFactory: IDashboardWidgetTypeFactory,
    @inject(IViewWidgetDashboardClipboard$)
    private readonly _clipboard: IViewWidgetDashboardClipboard
  ) {}

  //#endregion

  //#region props

  readonly type: IDashboardWidgetTilesProviderType = 'static';

  //#endregion

  //#region methods

  async getTiles(_args: DashboardWidgetTilesProviderArgs): Promise<IDashboardWidgetTile[]> {
    const viewWidgetType = this._widgetTypeFactory.getType(DefaultWidgetNames.View);

    return [
      new ViewClipboardTile(viewWidgetType, this._clipboard),
      new ViewInfoTile(this._clipboard)
    ];
  }

  //#endregion
}
