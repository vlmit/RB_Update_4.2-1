import { observable, reaction, runInAction } from 'mobx';
import { IExtensionContainer } from '@tessa/application';
import { Guid, StorageSerializable } from '@tessa/core';
import { ICardService } from '@tessa/platform';
import { Visibility } from 'tessa/platform';
import { CustomStyleFunc, getRgbaFromDecimal, UIButton } from 'tessa/ui';
import {
  ContainerDashboardWidgetBase,
  DashboardLayoutValue,
  DashboardState,
  DashboardViewModel,
  DashboardWidgetConfigurationStorage,
  DashboardWidgetDisplayType,
  DashboardWidgetFileStorage,
  DashboardWidgetHeader,
  DashboardWidgetSize,
  DashboardWidgetSizeLimits,
  DashboardWidgetStorage,
  WidgetUpdateInfo
} from 'tessa/ui/dashboard';
import { Themes } from 'tessa/ui/themes/themesHandler';
import {
  IRichModuleSettingsProvider,
  IRichTextBoxDependenciesFactory,
  RichStorage,
  RichTextBoxDataSource,
  RichTextBoxViewModel
} from 'ui/richTextBox';
import { group } from 'ui/toolbar/helpers';
import { DefaultWidgetNames } from '../widgetNames';
import { NotesRichAttachmentContainer } from './editor/notesRichAttachmentContainer';
import { NotesWidgetSettings } from './notesWidgetSettings';

export interface NotesWidgetCreateArgs {
  cardService: ICardService;
  richDependenciesFactory: IRichTextBoxDependenciesFactory;
  extensionsContainer: IExtensionContainer;
  moduleSettingsProvider: IRichModuleSettingsProvider;
  dashboard: DashboardViewModel;
  id?: string;
  settings?: NotesWidgetSettings;
  originId?: string | null;
  templateId?: string | null;
  originVersion?: number | null;
  shared?: boolean;
  canEditShared?: boolean;
}

/** Виджет "Заметки". */
export class NotesWidget extends ContainerDashboardWidgetBase<NotesWidgetSettings> {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link NotesWidget}.
   * @param _cardService Сервис для управления карточками.
   * @param _richDependenciesFactory Зависимости для {@link RichTextBoxViewModel}.
   * @param dashboard Модель представления дашборда.
   * @param id Уникальный идентификатор виджета. Если не задан, то будет сгенерирован новый идентификатор.
   * @param settings Настройки виджета. Если не заданы, то будут созданы новые настройки со значением по умолчанию.
   * @param [shared=false] Признак того, что виджет общий
   * @param [canEditShared=false] Есть ли права на редактирование контента общего виджета
   */
  constructor(args: NotesWidgetCreateArgs) {
    const settings = args.settings ?? new NotesWidgetSettings();
    super({
      type: args.shared ? DefaultWidgetNames.SharedNotes : DefaultWidgetNames.Notes,
      settings,
      dashboard: args.dashboard,
      id: args.id,
      shared: args.shared,
      canEditShared: args.canEditShared,
      header: new DashboardWidgetHeader(settings),
      initialSize: NotesWidget.initialWidgetSize,
      sizeLimits: NotesWidget.widgetSizeLimits,
      origin: args.originId ?? undefined,
      originVersion: args.originVersion ?? undefined
    });

    this._cardService = args.cardService;
    this._richDependenciesFactory = args.richDependenciesFactory;
    this._extensionsContainer = args.extensionsContainer;
    this._moduleSettingsProvider = args.moduleSettingsProvider;
    this._templateId = args.templateId ?? null;
  }

  //#endregion

  //#region fields

  protected readonly _cardService: ICardService;
  protected readonly _richDependenciesFactory: IRichTextBoxDependenciesFactory;
  protected readonly _extensionsContainer: IExtensionContainer;
  protected readonly _moduleSettingsProvider: IRichModuleSettingsProvider;

  protected _templateId: string | null;
  protected _filesIdsMap: Map<string, string> | null = null;
  protected _copiedContent: RichStorage | null = null;

  @observable.ref
  protected _richEditor: RichTextBoxViewModel | null;

  //#endregion

  //#region properties

  /** Редактор для форматирования текста. */
  get richEditor(): RichTextBoxViewModel | null {
    return this._richEditor;
  }
  protected set richEditor(value: RichTextBoxViewModel | null) {
    runInAction(() => (this._richEditor = value));
  }

  get contentEditingInProcess(): boolean {
    return this._richEditor?.editMode === 'edit';
  }

  get displayType(): DashboardWidgetDisplayType {
    return this.settings.noPadding ? 'full-space' : 'normal';
  }

  //#endregion

  //#region sizes

  static initialWidgetSize: DashboardLayoutValue<DashboardWidgetSize> = {
    xxs: { columnsCount: 4, rowsCount: 3 },
    md: { columnsCount: 5, rowsCount: 4 }
  };

  static widgetSizeLimits: DashboardLayoutValue<DashboardWidgetSizeLimits> = {
    xxs: { minColumnsCount: 2, minRowsCount: 1 },
    md: { minColumnsCount: 4, minRowsCount: 2 }
  };

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();

    const content = this._initialStorage?.content
      ? StorageSerializable.deserialize(RichStorage, this._initialStorage.content)
      : null;

    this.richEditor = await this.createEditor(content, this._initialStorage?.copyFiles());

    if (this._templateId) {
      this.hasChanges = true;
    }
  }

  protected override disposeCore(): void {
    super.disposeCore();
    this.richEditor?.dispose();
  }

  override async beforeCommitChanges(): Promise<void> {
    await super.beforeCommitChanges();

    if (this._templateId && this.richEditor) {
      const attachments = this.richEditor.getAttachments().map(a => a.id);
      this._filesIdsMap = new Map(attachments.map(id => [id, Guid.newGuid()]));

      const attachmentsContainer = this.richEditor.dataSource.attachmentsContainer;
      if (attachmentsContainer instanceof NotesRichAttachmentContainer) {
        attachmentsContainer.setCardId(this._dashboard.id);
      }

      this._copiedContent = (
        await this.richEditor.copyContent({
          filesMap: this._filesIdsMap
        })
      ).storage;
    }
  }

  override async commitChanges(): Promise<void> {
    await super.commitChanges();
    await this.richEditor?.commit();
  }

  override async afterSaveChanges(updateInfo?: WidgetUpdateInfo): Promise<void> {
    await super.afterSaveChanges(updateInfo);

    if (this._templateId && this._copiedContent) {
      // Сбрасываем ид шаблона после первого сохранения
      this._templateId = null;
      this._filesIdsMap = null;

      this.richEditor = null;
      this.richEditor = await this.createEditor(this._copiedContent);
      this._copiedContent = null;
      return;
    }

    const attachmentsContainer = this.richEditor?.dataSource.attachmentsContainer;
    if (attachmentsContainer instanceof NotesRichAttachmentContainer) {
      if (updateInfo?.origin) {
        attachmentsContainer.setCardId(updateInfo.origin);
      }
      await attachmentsContainer.reset();
    }
  }

  override async applyChanges(changes: DashboardWidgetStorage): Promise<void> {
    super.applyChanges(changes);

    if (this._hasChanges || !this.richEditor) {
      return;
    }

    if (changes.content) {
      const storage = StorageSerializable.deserialize(RichStorage, changes.content);
      this.richEditor.dataSource.setValue(storage);
    }

    const attachmentsContainer = this.richEditor.dataSource.attachmentsContainer;
    if (attachmentsContainer instanceof NotesRichAttachmentContainer) {
      await attachmentsContainer.reset();
    }

    this.richEditor.readOnly = this.shared && !this._canEditShared;
    this.hasChanges = false;
  }

  protected override getStorageCore(properties: DashboardWidgetStorage): DashboardWidgetStorage {
    if (this.richEditor) {
      properties.content =
        this._copiedContent?.serializeToStorage() ??
        this.richEditor.dataSource.getValue().serializeToStorage();
      const attachmentsContainer = this.richEditor.dataSource.attachmentsContainer;
      if (attachmentsContainer instanceof NotesRichAttachmentContainer) {
        properties.files = [...attachmentsContainer.files];
      }
      properties.filesSource = this._templateId;
      if (this._filesIdsMap) {
        properties.filesIdsMap = this._filesIdsMap;
      }
    }

    return properties;
  }

  protected override async afterApplyConfiguration(
    configuration: DashboardWidgetConfigurationStorage
  ): Promise<void> {
    await super.afterApplyConfiguration(configuration);

    if (this.richEditor) {
      this.setEditorStyle(this.richEditor);
    }
  }

  //#endregion

  //#region private methods

  protected async createEditor(
    content?: RichStorage | null,
    files?: DashboardWidgetFileStorage[]
  ): Promise<RichTextBoxViewModel> {
    const editor = await this.initializeEditor(content, files);
    this.initializeEditorContainer(editor);
    this.initializeEditorEvents(editor);

    return editor;
  }

  protected async initializeEditor(
    content?: RichStorage | null,
    files?: DashboardWidgetFileStorage[]
  ): Promise<RichTextBoxViewModel> {
    const cardId = this._templateId ?? this._origin ?? this._dashboard.id;

    const attachmentsContainer = new NotesRichAttachmentContainer(this._cardService, cardId, files);

    const dataSource = new RichTextBoxDataSource({ attachmentsContainer });
    if (content) {
      dataSource.setValue(content);
    }

    const editor = new RichTextBoxViewModel({
      dataSource,
      extensionContainer: this._extensionsContainer,
      dependenciesFactory: this._richDependenciesFactory,
      moduleSettingsProvider: this._moduleSettingsProvider
    });
    editor.modulesContainer.withDefaultModules();
    editor.info = { ['CardID']: cardId };
    editor.readOnly = this.shared && !this._canEditShared;
    editor.toolbar.addGroups(group('save', ['SaveNotesButton']));
    editor.toolbar.addButtons(
      UIButton.create({
        name: 'SaveNotesButton',
        icon: 'm-save',
        tooltip: '$UI_Common_Save',
        theme: 'primary',
        type: 'toolbar',
        isEnabled: () => this.hasChanges && this.dashboard.state !== DashboardState.InProgress,
        visibility: () =>
          this.contentEditingInProcess ? Visibility.Visible : Visibility.Collapsed,
        buttonAction: async () => {
          await this.saveChanges();

          if (this.dashboard.state === DashboardState.Ready) {
            editor.editMode = 'read';
          }
        }
      })
    );

    editor.classNames.add('notes-rich-editor');
    this.setEditorStyle(editor);

    await editor.initialize();

    return editor;
  }

  protected initializeEditorContainer(editor: RichTextBoxViewModel): void {
    editor.dimensions.stretchVertically = true;
    editor.themeOptions.border = 'none';
    editor.themeOptions.controlTheme.setParent(Themes.current);
    editor.themeOptions.controlTheme.build();
  }

  protected initializeEditorEvents(editor: RichTextBoxViewModel): void {
    editor.onContentChanged.add(() => {
      if (this.contentEditingInProcess) {
        this.hasChanges = true;
      }
    });
    editor.disposeList.add(
      reaction(
        () => editor.getAttachments(),
        () => {
          if (this.contentEditingInProcess) {
            this.hasChanges = true;
          }
        },
        { equals: () => false }
      )
    );
  }

  protected _setBackground: CustomStyleFunc = css => css`
    :has(.notes-rich-editor) {
      background-color: ${getRgbaFromDecimal(
        this.settings.editorBackground ?? undefined
      )} !important;
    }
  `;

  protected _setBorder: CustomStyleFunc = css => css`
    :has(.notes-rich-editor) {
      border: var(--1) solid ${getRgbaFromDecimal(this.settings.editorBorder ?? undefined)} !important;
    }
  `;

  protected setEditorStyle({ themeOptions }: RichTextBoxViewModel): void {
    if (this.settings.editorBackground != null) {
      themeOptions.background = 'none';
      themeOptions.controlStyle.add(this._setBackground);
    } else {
      themeOptions.background = 'default';
      themeOptions.controlStyle.remove(this._setBackground);
    }

    if (this.settings.editorBorder != null) {
      themeOptions.controlStyle.add(this._setBorder);
    } else {
      themeOptions.controlStyle.remove(this._setBorder);
    }
  }

  //#endregion
}
