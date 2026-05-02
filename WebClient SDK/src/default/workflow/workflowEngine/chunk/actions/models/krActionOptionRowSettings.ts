import { observable } from 'mobx';
import { FieldType, IStorage } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { KrActionNotificationRowRolesSettings } from './krActionNotificationRowRolesSettings';
import { KrActionOptionLinksSettings } from './krActionOptionLinksSettings';
import { KrActionOptionRowSettingsBase } from './krActionOptionRowSettingsBase';

/** Параметры действия, расположенные в строках таблицы "Варианты завершения действия". */
export class KrActionOptionRowSettings extends KrActionOptionRowSettingsBase<KrActionOptionRowSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly scriptKey = 'Script';

  /** @category Static Keys */
  static readonly notificationKey = 'Notification';

  /** @category Static Keys */
  static readonly notificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly notificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly excludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly excludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly notificationScriptKey = 'NotificationScript';

  //#endregion

  //#region fields

  private readonly _links = observable.array<KrActionOptionLinksSettings>([]);

  private readonly _recipients = observable.array<KrActionNotificationRowRolesSettings>([]);

  //#endregion

  //#region props

  get links(): Array<KrActionOptionLinksSettings> {
    return this._links;
  }

  get script(): string | null {
    return this.tryGetValue(KrActionOptionRowSettings.scriptKey);
  }
  @undoredo()
  set script(value: string | null) {
    this.setField(KrActionOptionRowSettings.scriptKey, value, FieldType.String);
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrActionOptionRowSettings.notificationKey,
      KrActionOptionRowSettings.notificationIdKey,
      KrActionOptionRowSettings.notificationNameKey
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setBindingKeyPair(
      KrActionOptionRowSettings.notificationKey,
      KrActionOptionRowSettings.notificationIdKey,
      KrActionOptionRowSettings.notificationNameKey,
      value
    );
  }

  get recipients(): Array<KrActionNotificationRowRolesSettings> {
    return this._recipients;
  }

  get excludeDeputies(): boolean | string {
    return this.tryGetValue(KrActionOptionRowSettings.excludeDeputiesKey) ?? false;
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.setBindingField(KrActionOptionRowSettings.excludeDeputiesKey, value, FieldType.Boolean);
  }

  get excludeSubscribers(): boolean | string {
    return this.tryGetValue(KrActionOptionRowSettings.excludeSubscribersKey) ?? false;
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.setBindingField(KrActionOptionRowSettings.excludeSubscribersKey, value, FieldType.Boolean);
  }

  get notificationScript(): string | null {
    return this.getTemplateSettingsOrThis().tryGetValue(
      KrActionOptionRowSettings.notificationScriptKey
    );
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.getTemplateSettingsOrThis().setField(
      KrActionOptionRowSettings.notificationScriptKey,
      value,
      FieldType.String
    );
  }

  //#region props

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrActionOptionRowSettings {
    return new KrActionOptionRowSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
