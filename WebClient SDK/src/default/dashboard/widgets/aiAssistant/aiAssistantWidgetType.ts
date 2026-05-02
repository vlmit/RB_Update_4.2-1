import { injectable, localize } from '@tessa/application';
import { getTessaIcon } from 'common/utility/uiHelpers';
import {
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTemplateOptions,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettings,
  IDashboardWidgetSettingsEditorProvider,
  PreviewWidgetTileViewModel,
  PlaceholderDashboardWidget
} from 'tessa/ui/dashboard';
import {
  AiHelper,
  IAiAssistantFactory,
  IAiAssistantFactory$,
  IAiOptions,
  IAiOptions$
} from 'tessa/ui/ai';
import { DefaultWidgetNames } from '../widgetNames';
import { AiAssistantWidget } from './aiAssistantWidget';
import { AiAssistantWidgetSettings } from './aiAssistantWidgetSettings';
import { AiAssistantWidgetSettingsEditorProvider } from './aiAssistantWidgetSettingsEditorProvider';

/** Тип для описания виджета {@link AiAssistantWidget}. */
@injectable()
export class AiAssistantWidgetType extends DashboardWidgetTypeBase<AiAssistantWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link AiAssistantWidgetType}.
   * @param _aiOptions Глобальные опции для конфигурации ИИ.
   * @param _aiAssistantFactory Предоставляет доступ к представлениям доступным в системе.
   */
  constructor(
    @IAiOptions$() private readonly _aiOptions: IAiOptions,
    @IAiAssistantFactory$() private readonly _aiAssistantFactory: IAiAssistantFactory
  ) {
    let descriptor: DashboardWidgetTypeDescriptor;

    if (!AiHelper.hasAiLicense()) {
      descriptor = AiAssistantWidgetType._noModuleDescriptor;
    } else if (!_aiOptions.isEnabled) {
      descriptor = AiAssistantWidgetType._disabledDescriptor;
    } else {
      descriptor = AiAssistantWidgetType._defaultDescriptor;
    }

    super(descriptor);
  }
  //#endregion

  //#region static

  private static readonly _defaultDescriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.AiAssistant,
    DefaultWidgetNames.AiAssistantTitle,
    {
      hidden: true,
      icon: getTessaIcon('Thin218'),
      title: '$Dashboard_Widget_AiAssistant_Preview',
      description: '$Dashboard_Widget_AiAssistant_Description'
    }
  );

  private static readonly _disabledDescriptor = new DashboardWidgetTypeDescriptor(
    AiAssistantWidgetType._defaultDescriptor.id,
    AiAssistantWidgetType._defaultDescriptor.title,
    {
      hidden: true,
      icon: getTessaIcon('Thin218'),
      title: '$Dashboard_Widget_AiAssistant_Preview',
      description: '$Dashboard_AiAssistantWidget_Unavailable_Message'
    }
  );

  private static readonly _noModuleDescriptor = new DashboardWidgetTypeDescriptor(
    AiAssistantWidgetType._defaultDescriptor.id,
    AiAssistantWidgetType._defaultDescriptor.title,
    {
      hidden: true,
      icon: getTessaIcon('Thin218'),
      title: '$Dashboard_Widget_AiAssistant_Preview',
      description: '$LicenseRequired_AiModule'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<AiAssistantWidgetSettings>
  ): IDashboardWidget {
    if (!this._aiOptions.isEnabled || !AiHelper.hasAiLicense()) {
      throw new Error('Cannot create unavailable widget.');
    }

    const settings = args?.settings ?? new AiAssistantWidgetSettings();
    settings.headerCaption ??= localize(this.descriptor.title);
    return new AiAssistantWidget(this._aiAssistantFactory, dashboard, args?.id, settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): IDashboardWidget {
    const hasLicense = AiHelper.hasAiLicense();
    if (!this._aiOptions.isEnabled || !hasLicense) {
      const type = AiAssistantWidgetType._defaultDescriptor.id;
      const text = !hasLicense
        ? localize('$LicenseRequired_AiModule')
        : localize('$Dashboard_AiAssistantWidget_Unavailable_Message');
      const widget = new PlaceholderDashboardWidget(dashboard, type, text, storage.id);
      widget.header.caption = localize(AiAssistantWidgetType._defaultDescriptor.title);
      return widget;
    }

    const widget = new AiAssistantWidget(this._aiAssistantFactory, dashboard, storage.id);
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
    widget: IDashboardWidget<IDashboardWidgetSettings>
  ): IDashboardWidgetSettingsEditorProvider {
    if (!this._aiOptions.isEnabled || !AiHelper.hasAiLicense()) {
      return { hasSettingsEditor: false, createEditor: async () => null };
    }

    if (!(widget instanceof AiAssistantWidget)) {
      throw new Error(`Widget must be of type ${DefaultWidgetNames.AiAssistant}`);
    }
    return new AiAssistantWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    const widgetPreview = new PreviewWidgetTileViewModel(this.descriptor);
    if (!AiHelper.hasAiLicense || !this._aiOptions.isEnabled) {
      widgetPreview.className.add('ai-assistant-disabled');
    }
    return widgetPreview;
  }

  //#endregion
}
