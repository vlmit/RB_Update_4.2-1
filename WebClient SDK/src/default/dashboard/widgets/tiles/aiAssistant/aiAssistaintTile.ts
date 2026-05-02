import { localize } from '@tessa/application';
import { IDashboardWidgetTile, DashboardViewModel, IDashboardWidgetType } from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../../widgetNames';
import { assertNotNull } from '@tessa/core';
import { AiHelper, IAiOptions } from 'tessa/ui/ai';
import { ShadowPropsContainer } from '@tessa/ui';
import { observable, runInAction } from 'mobx';
import './aiAssistantTile.scss';
import { Alert } from 'tessa/ui/alerts';

export class AiAssistantTile implements IDashboardWidgetTile {
  //#region fields

  @observable.ref
  protected _visibility = true;

  private readonly _widgetType: IDashboardWidgetType;

  private readonly _aiOptions: IAiOptions;

  protected readonly _shadow: ShadowPropsContainer<this>;

  private readonly _localizedTitle: string;

  //#endregion

  //#region ctor

  constructor(widgetType: IDashboardWidgetType, aiOptions: IAiOptions) {
    this._widgetType = widgetType;
    this._aiOptions = aiOptions;
    this._shadow = new ShadowPropsContainer(this);
    this._localizedTitle = localize(DefaultWidgetNames.AiAssistantTitle).toLowerCase();
  }

  //#endregion

  //#region props

  readonly order = 0;

  readonly name = 'AiAssistantTile';

  readonly isTemplate = false;

  readonly isShared = false;

  /**
   * @default true
   * @shadow-available
   */
  get visibility(): boolean {
    return this._shadow.get('visibility', this._visibility);
  }
  set visibility(value: boolean) {
    runInAction(() => {
      this._visibility = value;
    });
  }

  //#endregion

  //#region methods

  getViewModel(): object {
    return assertNotNull(this._widgetType.getWidgetPreview());
  }

  async handleClick(_e: React.MouseEvent, dashboard: DashboardViewModel): Promise<void> {
    const hasLicense = AiHelper.hasAiLicense();
    if (!hasLicense || !this._aiOptions.isEnabled) {
      Alert.info(
        !hasLicense
          ? localize('$LicenseRequired_AiModule')
          : localize('$Dashboard_AiAssistantWidget_Unavailable_Message')
      );
      return;
    }

    const widget = this._widgetType.createNewWidget(dashboard, {
      id: dashboard.generateWidgetIdentifier()
    });

    await dashboard.addWidget(widget);
  }

  filter(search: string): boolean {
    return this._localizedTitle.includes(search.toLowerCase());
  }

  dispose(): void {}

  //#endregion
}
