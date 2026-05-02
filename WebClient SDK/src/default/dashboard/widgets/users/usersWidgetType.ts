import { inject, injectable, localize } from '@tessa/application';
import { IUserInfoRepository, IUserInfoRepository$ } from '@tessa/platform';
import {
  PreviewWidgetTileViewModel,
  ContainerDashboardWidgetSettingsBase,
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettingsEditorProvider,
  DashboardWidgetTemplateOptions
} from 'tessa/ui/dashboard';
import { IUserInfoProvider$ } from '../widgetInjects';
import { DefaultWidgetNames } from '../widgetNames';
import { IUserInfoProvider } from './usersTypes';
import { UsersWidget } from './usersWidget';
import { UsersWidgetSettingsEditorProvider } from './usersWidgetSettingsEditorProvider';

/** Тип виджета {@link UsersWidget}. */
@injectable()
export class UsersWidgetType extends DashboardWidgetTypeBase<ContainerDashboardWidgetSettingsBase> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link UsersWidgetType}.
   * @param _userInfoProvider Провайдер для получения информации о пользователях.
   * @param _userInfoRepository Репозиторий для получения информации о пользователях.
   */
  constructor(
    @inject(IUserInfoProvider$) private readonly _userInfoProvider: IUserInfoProvider,
    @inject(IUserInfoRepository$) private readonly _userInfoRepository: IUserInfoRepository
  ) {
    super(UsersWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.Users,
    DefaultWidgetNames.UsersTitle,
    {
      image: 'images-emoji-ledger',
      title: '$Dashboard_Widget_Users_Preview_Title',
      description: '$Dashboard_Widget_Users_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<ContainerDashboardWidgetSettingsBase>
  ): UsersWidget {
    const settings = args?.settings ?? new ContainerDashboardWidgetSettingsBase();
    settings.headerCaption ??= localize(this.descriptor.title);

    return new UsersWidget(
      this._userInfoProvider,
      this._userInfoRepository,
      dashboard,
      args?.id,
      settings
    );
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): UsersWidget {
    const widget = new UsersWidget(
      this._userInfoProvider,
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
    if (!(widget instanceof UsersWidget)) {
      throw new Error('Widget must be of type UsersWidget');
    }

    return new UsersWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
