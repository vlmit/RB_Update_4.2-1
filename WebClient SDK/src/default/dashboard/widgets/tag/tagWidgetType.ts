import { inject, injectable } from '@tessa/application';
import { ITagManager, ITagManager$ } from '@tessa/platform';
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
import { ITagDataManager$ } from '../widgetInjects';
import { DefaultWidgetNames } from '../widgetNames';
import { TagWidget } from './tagWidget';
import { ITagDataManager } from './tagTypes';
import { TagWidgetSettings } from './tagWidgetSettings';
import { TagWidgetSettingsEditorProvider } from './tagWidgetSettingsEditorProvider';

/** Тип виджета {@link TagWidget}. */
@injectable()
export class TagWidgetType extends DashboardWidgetTypeBase<TagWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TagWidgetType}.
   * @param _tagDataManager Менеджер для получения и отображения данных о теге.
   * @param _tagManager Менеджер для работы с тегами.
   */
  constructor(
    @inject(ITagDataManager$) private readonly _tagDataManager: ITagDataManager,
    @inject(ITagManager$) private readonly _tagManager: ITagManager
  ) {
    super(TagWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.Tag,
    DefaultWidgetNames.TagTitle,
    {
      image: 'images-emoji-label',
      title: '$Dashboard_Widget_Tag_Preview_Title',
      description: '$Dashboard_Widget_Tag_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<TagWidgetSettings>
  ): TagWidget {
    return new TagWidget(this._tagDataManager, dashboard, args?.id, args?.settings);
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): TagWidget {
    const widget = new TagWidget(this._tagDataManager, dashboard, storage.id);
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
    if (!(widget instanceof TagWidget)) {
      throw new Error('Widget must be of type TagWidget');
    }

    return new TagWidgetSettingsEditorProvider(widget, this._tagManager);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
