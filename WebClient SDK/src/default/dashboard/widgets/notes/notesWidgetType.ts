import {
  IExtensionContainer,
  IExtensionContainer$,
  inject,
  injectable,
  localize
} from '@tessa/application';
import { ICardService, ICardService$ } from '@tessa/platform';
import {
  ContainerDashboardWidgetSettingsBase,
  DashboardViewModel,
  DashboardWidgetCreateNewParams,
  DashboardWidgetStorage,
  DashboardWidgetTemplateOptions,
  DashboardWidgetTypeBase,
  DashboardWidgetTypeDescriptor,
  IDashboardWidget,
  IDashboardWidgetSettingsEditorProvider,
  PreviewWidgetTileViewModel
} from 'tessa/ui/dashboard';
import {
  IRichModuleSettingsProvider,
  IRichModuleSettingsProvider$,
  IRichTextBoxDependenciesFactory,
  IRichTextBoxDependenciesFactory$
} from 'ui/richTextBox';
import { NotesRichTextBoxDependenciesFactoryName } from '../widgetInjects';
import { DefaultWidgetNames } from '../widgetNames';
import { NotesWidget } from './notesWidget';
import { NotesWidgetSettings } from './notesWidgetSettings';
import { NotesWidgetSettingsEditorProvider } from './notesWidgetSettingsEditorProvider';

/** Тип виджета {@link NotesWidget}. */
@injectable()
export class NotesWidgetType extends DashboardWidgetTypeBase<ContainerDashboardWidgetSettingsBase> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link NotesWidgetType}.
   * @param _cardService Сервис для управления карточками.
   * @param _richDependenciesFactory Зависимости для {@link RichTextBoxViewModel}.
   */
  constructor(
    @inject(ICardService$) private readonly _cardService: ICardService,
    @inject(IRichTextBoxDependenciesFactory$, { name: NotesRichTextBoxDependenciesFactoryName })
    private readonly _richDependenciesFactory: IRichTextBoxDependenciesFactory,
    @inject(IExtensionContainer$)
    private readonly _extensionContainer: IExtensionContainer,
    @inject(IRichModuleSettingsProvider$)
    private readonly _moduleSettingsProvider: IRichModuleSettingsProvider
  ) {
    super(NotesWidgetType._descriptor);
  }

  //#endregion

  //#region static

  private static readonly _descriptor = new DashboardWidgetTypeDescriptor(
    DefaultWidgetNames.Notes,
    DefaultWidgetNames.NotesTitle,
    {
      image: 'images-emoji-memo',
      title: DefaultWidgetNames.NotesTitle,
      description: '$Dashboard_Widget_Notes_Description'
    }
  );

  //#endregion

  //#region base overrides

  override createNewWidget(
    dashboard: DashboardViewModel,
    args?: DashboardWidgetCreateNewParams<NotesWidgetSettings>
  ): NotesWidget {
    const settings = args?.settings ?? new NotesWidgetSettings();
    settings.headerCaption ??= localize(this.descriptor.title);

    return new NotesWidget({
      cardService: this._cardService,
      richDependenciesFactory: this._richDependenciesFactory,
      extensionsContainer: this._extensionContainer,
      moduleSettingsProvider: this._moduleSettingsProvider,
      dashboard,
      id: args?.id,
      settings
    });
  }

  override deserializeWidget(
    storage: DashboardWidgetStorage,
    dashboard: DashboardViewModel
  ): NotesWidget {
    const widget = new NotesWidget({
      cardService: this._cardService,
      richDependenciesFactory: this._richDependenciesFactory,
      extensionsContainer: this._extensionContainer,
      moduleSettingsProvider: this._moduleSettingsProvider,
      dashboard,
      id: storage.id,
      originId: storage.origin
    });
    widget.setStorage(storage);

    return widget;
  }

  override createWidgetByTemplate(
    templateStorage: DashboardWidgetStorage,
    dashboard: DashboardViewModel,
    templateOptions: DashboardWidgetTemplateOptions
  ): IDashboardWidget {
    const widget = new NotesWidget({
      cardService: this._cardService,
      richDependenciesFactory: this._richDependenciesFactory,
      extensionsContainer: this._extensionContainer,
      moduleSettingsProvider: this._moduleSettingsProvider,
      dashboard,
      id: templateStorage.id,
      templateId: templateOptions.templateId
    });
    widget.setStorage(templateStorage);

    return widget;
  }

  override getSettingsEditorProvider(
    widget: IDashboardWidget
  ): IDashboardWidgetSettingsEditorProvider {
    if (!(widget instanceof NotesWidget)) {
      throw new Error('Widget must be of type NotesWidget');
    }

    return new NotesWidgetSettingsEditorProvider(widget);
  }

  override getWidgetPreview(): object | null {
    return new PreviewWidgetTileViewModel(this.descriptor);
  }

  //#endregion
}
