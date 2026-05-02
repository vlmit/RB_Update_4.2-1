import { observable } from 'mobx';
import { FieldType, IStorage } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { KrActionNotificationRowRolesSettings } from './krActionNotificationRowRolesSettings';
import { KrTaskOptionRowSettingsBase } from './krTaskOptionRowSettingsBase';

/** Параметры действия, расположенные в строках таблицы "Варианты завершения". */
export class KrTaskOptionRowSettings extends KrTaskOptionRowSettingsBase<KrTaskOptionRowSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly scriptKey = 'Script';

  /** @category Static Keys */
  static readonly resultKey = 'Result';

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

  private readonly _recipients = observable.array<KrActionNotificationRowRolesSettings>([]);

  //#endregion

  //#region props

  get script(): string | null {
    return this.tryGetValue(KrTaskOptionRowSettings.scriptKey);
  }
  @undoredo()
  set script(value: string | null) {
    this.setField(KrTaskOptionRowSettings.scriptKey, value, FieldType.String);
  }

  get result(): string | null {
    return this.tryGetValue(KrTaskOptionRowSettings.resultKey);
  }
  @undoredo()
  set result(value: string | null) {
    this.setBindingField(KrTaskOptionRowSettings.resultKey, value, FieldType.String);
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrTaskOptionRowSettings.notificationKey,
      KrTaskOptionRowSettings.notificationIdKey,
      KrTaskOptionRowSettings.notificationNameKey
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setBindingKeyPair(
      KrTaskOptionRowSettings.notificationKey,
      KrTaskOptionRowSettings.notificationIdKey,
      KrTaskOptionRowSettings.notificationNameKey,
      value
    );
  }

  get recipients(): Array<KrActionNotificationRowRolesSettings> {
    return this._recipients;
  }

  get sendToPerformer(): boolean | string {
    return this.tryGetValue(KrTaskOptionRowSettings.sendToPerformerKey) ?? false;
  }
  @undoredo()
  set sendToPerformer(value: boolean | string) {
    this.setBindingField(KrTaskOptionRowSettings.sendToPerformerKey, value, FieldType.Boolean);
  }

  get sendToAuthor(): boolean | string {
    return this.tryGetValue(KrTaskOptionRowSettings.sendToAuthorKey) ?? false;
  }
  @undoredo()
  set sendToAuthor(value: boolean | string) {
    this.setBindingField(KrTaskOptionRowSettings.sendToAuthorKey, value, FieldType.Boolean);
  }

  get excludeDeputies(): boolean | string {
    return this.tryGetValue(KrTaskOptionRowSettings.excludeDeputiesKey) ?? false;
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.setBindingField(KrTaskOptionRowSettings.excludeDeputiesKey, value, FieldType.Boolean);
  }

  get excludeSubscribers(): boolean | string {
    return this.tryGetValue(KrTaskOptionRowSettings.excludeSubscribersKey) ?? false;
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.setBindingField(KrTaskOptionRowSettings.excludeSubscribersKey, value, FieldType.Boolean);
  }

  get notificationScript(): string | null {
    return this.getTemplateSettingsOrThis().tryGetValue(
      KrTaskOptionRowSettings.notificationScriptKey
    );
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.getTemplateSettingsOrThis().setField(
      KrTaskOptionRowSettings.notificationScriptKey,
      value,
      FieldType.String
    );
  }

  //#region props

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrTaskOptionRowSettings {
    return new KrTaskOptionRowSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
