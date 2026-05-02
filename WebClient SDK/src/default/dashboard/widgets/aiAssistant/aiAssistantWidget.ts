import {
  ContainerDashboardWidgetBase,
  DashboardLayoutValue,
  DashboardViewModel,
  DashboardWidgetHeader,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits
} from 'tessa/ui/dashboard';
import { AiAssistantViewModel, IAiAssistantFactory } from 'tessa/ui/ai';
import { DefaultWidgetNames } from '../widgetNames';
import { AiAssistantWidgetSettings } from './aiAssistantWidgetSettings';

/** Виджет "ИИ ассистент". */
export class AiAssistantWidget extends ContainerDashboardWidgetBase<AiAssistantWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link AiAssistantWidget}.
   * @param aiAssistantFactory Фабрика для создания ИИ ассистента.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   */
  constructor(
    aiAssistantFactory: IAiAssistantFactory,
    dashboard: DashboardViewModel,
    id?: string,
    settings = new AiAssistantWidgetSettings()
  ) {
    super({
      dashboard,
      id,
      settings,
      type: DefaultWidgetNames.AiAssistant,
      header: new DashboardWidgetHeader(settings),
      initialSize: AiAssistantWidget.initialWidgetSize,
      sizeLimits: AiAssistantWidget.widgetSizeLimits
    });

    this.aiAssistant = aiAssistantFactory({ settings });
  }

  //#endregion

  //#region properties

  /** Модель представления контрола "ИИ ассистент". */
  readonly aiAssistant: AiAssistantViewModel;

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    await this.aiAssistant.initialize();
  }

  protected override disposeCore(): void {
    this.aiAssistant.dispose();
    super.disposeCore();
  }

  //#endregion

  //#region static

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: { columnsCount: 5, rowsCount: 6 },
    md: { columnsCount: 6, rowsCount: 7 }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: { minColumnsCount: 3, minRowsCount: 4 },
    md: { minColumnsCount: 4, minRowsCount: 5 }
  };

  //#endregion
}
