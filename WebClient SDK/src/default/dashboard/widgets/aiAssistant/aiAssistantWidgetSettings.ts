import { IStorage, StorageSerializableContext } from '@tessa/core';
import { ContainerDashboardWidgetSettingsBase } from 'tessa/ui/dashboard';
import { AiAssistantSettings, AiToolInfo, IAiAssistantSettings } from 'tessa/ui/ai';

/** Объект с настройками виджета {@link AiAssistantWidget}. */
export class AiAssistantWidgetSettings
  extends ContainerDashboardWidgetSettingsBase
  implements IAiAssistantSettings
{
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link AiAssistantWidgetSettings}.
   * @param _settings Настройки для контрола ИИ Ассистента.
   */
  constructor(private readonly _settings: IAiAssistantSettings = new AiAssistantSettings()) {
    super();
  }

  //#endregion

  //#region keys

  /** @category Static Keys */
  static readonly chatBackgroundColorKey = 'ChatBackgroundColor';

  /** @category Static Keys */
  static readonly incomingMessageColorKey = 'IncomingMessageColor';

  /** @category Static Keys */
  static readonly outgoingMessageColorKey = 'OutgoingMessageColor';

  /** @category Static Keys */
  static readonly outgoingMessageMarkdownKey = 'OutgoingMessageMarkdown';

  /** @category Static Keys */
  static readonly messageShowHeaderKey = 'MessageShowHeader';

  /** @category Static Keys */
  static readonly messageShowFooterKey = 'MessageShowFooter';

  /** @category Static Keys */
  static readonly incomingMessageTypingSpeedKey = 'IncomingMessageTypingSpeed';

  /** @category Static Keys */
  static readonly predefinedToolKey = 'PredefinedTool';

  //#endregion

  //#region properties

  /**
   * Цвет фона чата.
   * @default null
   */
  get chatBackgroundColor(): number | null {
    return this._settings.chatBackgroundColor;
  }
  set chatBackgroundColor(value: number | null) {
    this._settings.chatBackgroundColor = value;
  }

  /**
   * Цвет входящего сообщения от ИИ-ассистента.
   * @default null
   */
  get incomingMessageColor(): number | null {
    return this._settings.incomingMessageColor;
  }
  set incomingMessageColor(value: number | null) {
    this._settings.incomingMessageColor = value;
  }

  /**
   * Цвет исходящего сообщения от пользователя.
   * @default null
   */
  get outgoingMessageColor(): number | null {
    return this._settings.outgoingMessageColor;
  }
  set outgoingMessageColor(value: number | null) {
    this._settings.outgoingMessageColor = value;
  }

  /**
   * Отображать исходящее сообщение в Markdown.
   * @default false
   */
  get outgoingMessageMarkdown(): boolean {
    return this._settings.outgoingMessageMarkdown;
  }
  set outgoingMessageMarkdown(value: boolean) {
    this._settings.outgoingMessageMarkdown = value;
  }

  /**
   * Показывать верхний колонтитул в сообщении.
   * @default false
   */
  get messageShowHeader(): boolean {
    return this._settings.messageShowHeader;
  }
  set messageShowHeader(value: boolean) {
    this._settings.messageShowHeader = value;
  }

  /**
   * Показывать нижний колонтитул в сообщении.
   * @default true
   */
  get messageShowFooter(): boolean {
    return this._settings.messageShowFooter;
  }
  set messageShowFooter(value: boolean) {
    this._settings.messageShowFooter = value;
  }

  /**
   * Скорость ввода символов (мс).
   * @default 20
   */
  get incomingMessageTypingSpeed(): number {
    return this._settings.incomingMessageTypingSpeed;
  }
  set incomingMessageTypingSpeed(value: number) {
    this._settings.incomingMessageTypingSpeed = value;
  }

  /** Предустановленный инструмент.
   * @default null
   */
  get predefinedTool(): AiToolInfo | null {
    return this._settings.predefinedTool;
  }
  set predefinedTool(value: AiToolInfo | null) {
    this._settings.predefinedTool = value;
  }

  //#endregion

  //#region IStorageSerializable

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setIntIfNotDefault(
      AiAssistantWidgetSettings.chatBackgroundColorKey,
      this.chatBackgroundColor,
      null
    );
    sa.setIntIfNotDefault(
      AiAssistantWidgetSettings.incomingMessageColorKey,
      this.incomingMessageColor,
      null
    );
    sa.setIntIfNotDefault(
      AiAssistantWidgetSettings.outgoingMessageColorKey,
      this.outgoingMessageColor,
      null
    );
    sa.setBooleanIfNotDefault(
      AiAssistantWidgetSettings.outgoingMessageMarkdownKey,
      this.outgoingMessageMarkdown,
      null
    );
    sa.setBooleanIfNotDefault(
      AiAssistantWidgetSettings.messageShowHeaderKey,
      this.messageShowHeader,
      false
    );
    sa.setBooleanIfNotDefault(
      AiAssistantWidgetSettings.messageShowFooterKey,
      this.messageShowFooter,
      true
    );
    sa.setIntIfNotDefault(
      AiAssistantWidgetSettings.incomingMessageTypingSpeedKey,
      this.incomingMessageTypingSpeed,
      20
    );
    sa.setIfNotNull(
      AiAssistantWidgetSettings.predefinedToolKey,
      this.predefinedTool?.serializeToStorage({}, context)
    );

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.chatBackgroundColor = sa.tryGetInt(AiAssistantWidgetSettings.chatBackgroundColorKey);
    this.incomingMessageColor = sa.tryGetInt(AiAssistantWidgetSettings.incomingMessageColorKey);
    this.outgoingMessageColor = sa.tryGetInt(AiAssistantWidgetSettings.outgoingMessageColorKey);
    this.outgoingMessageMarkdown = sa.tryGetBooleanOrDefault(
      AiAssistantWidgetSettings.outgoingMessageMarkdownKey,
      false
    );
    this.messageShowHeader = sa.tryGetBooleanOrDefault(
      AiAssistantWidgetSettings.messageShowHeaderKey,
      false
    );
    this.messageShowFooter = sa.tryGetBooleanOrDefault(
      AiAssistantWidgetSettings.messageShowFooterKey,
      true
    );
    this.incomingMessageTypingSpeed = sa.tryGetIntOrDefault(
      AiAssistantWidgetSettings.incomingMessageTypingSpeedKey,
      20
    );
    this.predefinedTool = sa.tryGetObject(AiAssistantWidgetSettings.predefinedToolKey, s =>
      new AiToolInfo().deserializeFromStorage(s, context)
    );
  }

  //#endregion
}
