import { inject, injectable, localize } from '@tessa/application';
import { IUserInfoRepository, IUserInfoRepository$ } from '@tessa/platform';
import {
  PreviewWidgetTileViewModel,
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettingsEditorProvider,
  DashboardWidgetTemplateOptions
} from 'tessa/ui/dashboard';
import { DefaultWidgetNames } from '../widgetNames';
import { IBirthdayInfoProvider$ } from '../widgetInjects';
import { BirthdayWidget } from './birthdayWidget';
import { IBirthdayInfoProvider } from './birthdayTypes';
import { BirthdayWidgetSettings } from './birthdayWidgetSettings';
import { BirthdayWidgetSettingsEditorProvider } from './birthdayWidgetSettingsEditorProvider';

/** Тип виджета {@link BirthdayWidget}. */
@injectable()
export class BirthdayWidgetType extends DashboardWidgetTypeBase<BirthdayWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link BirthdayWidgetType}.
   * @param _viewRepository Предоставляет доступ к представлениям доступным в системе.
   * @param _session Текущая сессия пользователя.
   */
  constructor(
    @inject(IBirthdayInfoProvider$) private readonly _infoProvider: IBirthdayInfoProvider,
    @inject(IUserInfoRepository$) private readonly _userInfoRepository: IUserInfoRepository
  ) {
    super(BirthdayWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.Birthday,
    DefaultWidgetNames.BirthdayTitle,
    {
      image: 'images-emoji-partying-face',
      title: '$Dashboard_Widget_Birthday_Preview_Title',
      description: '$Dashboard_Widget_Birthday_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<BirthdayWidgetSettings>
  ): BirthdayWidget {
    const settings = args?.settings ?? new BirthdayWidgetSettings();
    settings.headerCaption ??= localize(this.descriptor.title);

    return new BirthdayWidget(
      this._infoProvider,
      this._userInfoRepository,
      dashboard,
      args?.id,
      settings
    );
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): BirthdayWidget {
    const widget = new BirthdayWidget(
      this._infoProvider,
      this._userInfoRepository,
      dashboard,
      storage.id
    );
    widget.setStorage(storage);

    return widget;
  }

  override createWidgetByTemplate(
    templateStorage: DashboardWidgetStorage,
    dashboard: DashboardViewModel,
    _templateOptions: DashboardWidgetTemplateOptions
  ): IDashboardWidget {
    return this.deserializeWidget(templateStorage, dashboard);
  }

  override getSettingsEditorProvider(
    widget: IDashboardWidget
  ): IDashboardWidgetSettingsEditorProvider {
    if (!(widget instanceof BirthdayWidget)) {
      throw new Error('Widget must be of type BirthdayWidget');
    }

    return new BirthdayWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
