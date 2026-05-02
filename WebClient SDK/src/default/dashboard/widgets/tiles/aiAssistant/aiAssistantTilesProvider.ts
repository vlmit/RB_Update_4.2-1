import { inject, injectable } from '@tessa/application';
import {
  IDashboardWidgetTypeFactory$,
  IDashboardWidgetTile,
  IDashboardWidgetTilesProvider,
  IDashboardWidgetTypeFactory,
  IDashboardWidgetTilesProviderType,
  DashboardWidgetTilesProviderArgs
} from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../../widgetNames';
import { AiAssistantTile } from './aiAssistaintTile';
import { IAiOptions, IAiOptions$ } from 'tessa/ui/ai';

@injectable()
export class AiAssistantTilesProvider implements IDashboardWidgetTilesProvider {
  // #region ctor

  constructor(
    @inject(IDashboardWidgetTypeFactory$)
    private readonly _widgetTypeFactory: IDashboardWidgetTypeFactory,
    @IAiOptions$() private readonly _aiOptions: IAiOptions
  ) {}

  //#endregion

  //#region props

  readonly type: IDashboardWidgetTilesProviderType = 'static';

  //#endregion

  //#region methods

  async getTiles(_args: DashboardWidgetTilesProviderArgs): Promise<IDashboardWidgetTile[]> {
    const aiAssistantWidgetType = this._widgetTypeFactory.getType(DefaultWidgetNames.AiAssistant);

    return [new AiAssistantTile(aiAssistantWidgetType, this._aiOptions)];
  }

  //#endregion
}
