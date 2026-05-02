import { observable } from 'mobx';
import { FieldType, IStorage } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { WorkflowTaskActionOptionRowLinksSettings } from 'tessa/ui/workflow/chunk';
import { KrActionNotificationRowRolesSettings } from './krActionNotificationRowRolesSettings';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { KrTaskOptionRowSettingsBase } from './krTaskOptionRowSettingsBase';

/** Строка таблицы "Варианты завершения" из действия "Задание регистрации". */
export class KrTaskRegistrationActionOptionRowSettings extends KrTaskOptionRowSettingsBase<KrTaskRegistrationActionOptionRowSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly resultKey = 'Result';

  /** @category Static Keys */
  static readonly scriptKey = 'Script';

  /** @category Static Keys */
  static readonly notificationKey = 'Notification';

  /** @category Static Keys */
  static readonly notificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly notificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly sendToPerformerKey = 'SendToPerformer';

  /** @category Static Keys */
  static readonly sendToAuthorKey = 'SendToAuthor';

  /** @category Static Keys */
  static readonly excludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly excludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly notificationScriptKey = 'NotificationScript';

  //#endregion

  //#region fields

  private readonly _links = observable.array<WorkflowTaskActionOptionRowLinksSettings>([]);

  private readonly _recipients = observable.array<KrActionNotificationRowRolesSettings>([]);

  //#endregion

  //#region props

  get result(): string | null {
    return this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.resultKey);
  }
  @undoredo()
  set result(value: string | null) {
    this.setBindingField(
      KrTaskRegistrationActionOptionRowSettings.resultKey,
      value,
      FieldType.String
    );
  }

  get links(): Array<WorkflowTaskActionOptionRowLinksSettings> {
    return this._links;
  }

  get script(): string | null {
    return this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.scriptKey);
  }
  @undoredo()
  set script(value: string | null) {
    this.setField(KrTaskRegistrationActionOptionRowSettings.scriptKey, value, FieldType.String);
  }

  get recipients(): Array<KrActionNotificationRowRolesSettings> {
    return this._recipients;
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrTaskRegistrationActionOptionRowSettings.notificationKey,
      KrTaskRegistrationActionOptionRowSettings.notificationIdKey,
      KrTaskRegistrationActionOptionRowSettings.notificationNameKey
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setBindingKeyPair(
      KrTaskRegistrationActionOptionRowSettings.notificationKey,
      KrTaskRegistrationActionOptionRowSettings.notificationIdKey,
      KrTaskRegistrationActionOptionRowSettings.notificationNameKey,
      value
    );
  }

  get sendToPerformer(): boolean | string {
    return this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.sendToPerformerKey) ?? false;
  }
  @undoredo()
  set sendToPerformer(value: boolean | string) {
    this.setBindingField(
      KrTaskRegistrationActionOptionRowSettings.sendToPerformerKey,
      value,
      FieldType.Boolean
    );
  }

  get sendToAuthor(): boolean | string {
    return this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.sendToAuthorKey) ?? false;
  }
  @undoredo()
  set sendToAuthor(value: boolean | string) {
    this.setBindingField(
      KrTaskRegistrationActionOptionRowSettings.sendToAuthorKey,
      value,
      FieldType.Boolean
    );
  }

  get excludeDeputies(): boolean | string {
    return this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.excludeDeputiesKey) ?? false;
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.setBindingField(
      KrTaskRegistrationActionOptionRowSettings.excludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get excludeSubscribers(): boolean | string {
    return (
      this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.excludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.setBindingField(
      KrTaskRegistrationActionOptionRowSettings.excludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get notificationScript(): string | null {
    return this.tryGetValue(KrTaskRegistrationActionOptionRowSettings.notificationScriptKey);
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.setField(
      KrTaskRegistrationActionOptionRowSettings.notificationScriptKey,
      value,
      FieldType.String
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(
    storage: IStorage
  ): KrTaskRegistrationActionOptionRowSettings {
    return new KrTaskRegistrationActionOptionRowSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
