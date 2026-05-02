import { FieldType, IStorage, StorageArray } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import {
  WorkflowActionSettingsStorageBase,
  WorkflowTaskActionEventRowSettings,
  WorkflowTaskActionOptionRowLinksSettings
} from 'tessa/ui/workflow/chunk';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { KrTaskRegistrationActionOptionRowSettings } from './krTaskRegistrationActionOptionRowSettings';
import { KrActionNotificationRowRolesSettings } from './krActionNotificationRowRolesSettings';

/** Параметры действия "Задание регистрации". */
export class KrTaskRegistrationActionSettings extends WorkflowActionSettingsStorageBase<KrTaskRegistrationActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krTaskRegistrationActionVirtualSectionName = 'KrTaskRegistrationActionVirtual';

  /** @category Static Keys */
  static readonly krTaskRegistrationActionOptionsVirtualSectionName =
    'KrTaskRegistrationActionOptionsVirtual';

  /** @category Static Keys */
  static readonly krTaskRegistrationActionOptionLinksVirtualSectionName =
    'KrTaskRegistrationActionOptionLinksVirtual';

  /** @category Static Keys */
  // Опечатка присутствует в схеме.
  static readonly krTaskRegistrationActionNotificationRolesVitrualSectionName =
    'KrTaskRegistrationActionNotificationRolesVitrual';

  /** @category Static Keys */
  static readonly weTaskActionEventsSectionName = 'WeTaskActionEvents';

  /** @category Static Keys */
  static readonly performerKey = 'Performer';

  /** @category Static Keys */
  static readonly performerIdKey = 'ID';

  /** @category Static Keys */
  static readonly performerNameKey = 'Name';

  /** @category Static Keys */
  static readonly authorKey = 'Author';

  /** @category Static Keys */
  static readonly authorIdKey = 'ID';

  /** @category Static Keys */
  static readonly authorNameKey = 'Name';

  /** @category Static Keys */
  static readonly digestKey = 'Digest';

  /** @category Static Keys */
  static readonly kindKey = 'Kind';

  /** @category Static Keys */
  static readonly kindIdKey = 'ID';

  /** @category Static Keys */
  static readonly kindCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly resultKey = 'Result';

  /** @category Static Keys */
  static readonly periodKey = 'Period';

  /** @category Static Keys */
  static readonly plannedKey = 'Planned';

  /** @category Static Keys */
  static readonly canEditCardKey = 'CanEditCard';

  /** @category Static Keys */
  static readonly canEditAnyFilesKey = 'CanEditAnyFiles';

  /** @category Static Keys */
  static readonly initTaskScriptKey = 'InitTaskScript';

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

  //#region props

  get performer(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetKeyPair(
        KrTaskRegistrationActionSettings.performerKey,
        KrTaskRegistrationActionSettings.performerIdKey,
        KrTaskRegistrationActionSettings.performerNameKey
      ) ?? null
    );
  }
  @undoredo()
  set performer(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingKeyPair(
      KrTaskRegistrationActionSettings.performerKey,
      KrTaskRegistrationActionSettings.performerIdKey,
      KrTaskRegistrationActionSettings.performerNameKey,
      value
    );
  }

  get author(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetKeyPair(
        KrTaskRegistrationActionSettings.authorKey,
        KrTaskRegistrationActionSettings.authorIdKey,
        KrTaskRegistrationActionSettings.authorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set author(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingKeyPair(
      KrTaskRegistrationActionSettings.authorKey,
      KrTaskRegistrationActionSettings.authorIdKey,
      KrTaskRegistrationActionSettings.authorNameKey,
      value
    );
  }

  get digest(): string | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.digestKey) ?? null
    );
  }
  @undoredo()
  set digest(value: string | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(KrTaskRegistrationActionSettings.digestKey, value, FieldType.String);
  }

  get kind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetKeyPair(
        KrTaskRegistrationActionSettings.kindKey,
        KrTaskRegistrationActionSettings.kindIdKey,
        KrTaskRegistrationActionSettings.kindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set kind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingKeyPair(
      KrTaskRegistrationActionSettings.kindKey,
      KrTaskRegistrationActionSettings.kindIdKey,
      KrTaskRegistrationActionSettings.kindCaptionKey,
      value
    );
  }

  get result(): string | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.resultKey) ?? null
    );
  }
  @undoredo()
  set result(value: string | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(KrTaskRegistrationActionSettings.resultKey, value, FieldType.String);
  }

  get period(): number | string | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.periodKey) ?? null
    );
  }
  @undoredo()
  set period(value: number | string | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(KrTaskRegistrationActionSettings.periodKey, value, FieldType.Double);
  }

  get planned(): string | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.plannedKey) ?? null
    );
  }
  @undoredo()
  set planned(value: string | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(KrTaskRegistrationActionSettings.plannedKey, value, FieldType.DateTime);
  }

  get canEditCard(): boolean | string {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.canEditCardKey) ?? false
    );
  }
  @undoredo()
  set canEditCard(value: boolean | string) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(KrTaskRegistrationActionSettings.canEditCardKey, value, FieldType.Boolean);
  }

  get canEditAnyFiles(): boolean | string {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.canEditAnyFilesKey) ?? false
    );
  }
  @undoredo()
  set canEditAnyFiles(value: boolean | string) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(
      KrTaskRegistrationActionSettings.canEditAnyFilesKey,
      value,
      FieldType.Boolean
    );
  }

  get initTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(
          KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
        )
        ?.tryGetValue(KrTaskRegistrationActionSettings.initTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set initTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName)
      .setBindingField(KrTaskRegistrationActionSettings.initTaskScriptKey, value, FieldType.String);
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetKeyPair(
        KrTaskRegistrationActionSettings.notificationKey,
        KrTaskRegistrationActionSettings.notificationIdKey,
        KrTaskRegistrationActionSettings.notificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingKeyPair(
      KrTaskRegistrationActionSettings.notificationKey,
      KrTaskRegistrationActionSettings.notificationIdKey,
      KrTaskRegistrationActionSettings.notificationNameKey,
      value
    );
  }

  get excludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.excludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(
      KrTaskRegistrationActionSettings.excludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get excludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      )?.tryGetValue(KrTaskRegistrationActionSettings.excludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
    ).setBindingField(
      KrTaskRegistrationActionSettings.excludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get notificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(
          KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
        )
        ?.tryGetValue(KrTaskRegistrationActionSettings.notificationScriptKey) ?? null
    );
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName)
      .setBindingField(
        KrTaskRegistrationActionSettings.notificationScriptKey,
        value,
        FieldType.String
      );
  }

  get completeOptions(): StorageArray<KrTaskRegistrationActionOptionRowSettings> {
    return this.getTemplateSettingsOrThis().getArray(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionOptionsVirtualSectionName,
      x =>
        KrTaskRegistrationActionOptionRowSettings.factory(
          KrTaskRegistrationActionOptionRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptions(value: StorageArray<KrTaskRegistrationActionOptionRowSettings>) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionOptionsVirtualSectionName,
      value
    );
  }

  get completeOptionsLinks(): StorageArray<WorkflowTaskActionOptionRowLinksSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionOptionLinksVirtualSectionName,
      x =>
        WorkflowTaskActionOptionRowLinksSettings.factory(
          WorkflowTaskActionOptionRowLinksSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptionsLinks(value: Array<WorkflowTaskActionOptionRowLinksSettings>) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionOptionLinksVirtualSectionName,
      value
    );
  }

  get completeOptionsNotificationsRecipients(): StorageArray<KrActionNotificationRowRolesSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionNotificationRolesVitrualSectionName,
      x =>
        KrActionNotificationRowRolesSettings.factory(
          KrActionNotificationRowRolesSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptionsNotificationsRecipients(
    value: StorageArray<KrActionNotificationRowRolesSettings> | null
  ) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionNotificationRolesVitrualSectionName,
      value
    );
  }

  get events(): StorageArray<WorkflowTaskActionEventRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrTaskRegistrationActionSettings.weTaskActionEventsSectionName,
      x =>
        WorkflowTaskActionEventRowSettings.factory(
          WorkflowTaskActionEventRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set events(value: StorageArray<WorkflowTaskActionEventRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrTaskRegistrationActionSettings.weTaskActionEventsSectionName,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrTaskRegistrationActionSettings {
    return new KrTaskRegistrationActionSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    this.getTemplateSettingsOrThis().init(
      KrTaskRegistrationActionSettings.krTaskRegistrationActionOptionsVirtualSectionName,
      null
    );
    if (!this.planned) {
      this.getSubObject(
        KrTaskRegistrationActionSettings.krTaskRegistrationActionVirtualSectionName
      ).initField(KrTaskRegistrationActionSettings.periodKey, 1, FieldType.Double);
    }
  }

  //#endregion
}
