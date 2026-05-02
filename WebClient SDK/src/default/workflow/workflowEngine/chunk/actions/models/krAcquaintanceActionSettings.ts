import { FieldType, IStorage, StorageArray } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import {
  WorkflowActionRoleRowSettings,
  WorkflowActionSettingsRowStorageBase,
  WorkflowActionSettingsStorageBase
} from 'tessa/ui/workflow/chunk';

/** Параметры действия "Ознакомление". */
export class KrAcquaintanceActionSettings extends WorkflowActionSettingsStorageBase<KrAcquaintanceActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krAcquaintanceActionSectionName = 'KrAcquaintanceAction';

  /** @category Static Keys */
  static readonly senderKey = 'Sender';

  /** @category Static Keys */
  static readonly senderIdKey = 'ID';

  /** @category Static Keys */
  static readonly senderNameKey = 'Name';

  /** @category Static Keys */
  static readonly commentKey = 'Comment';

  /** @category Static Keys */
  static readonly aliasMetadataKey = 'AliasMetadata';

  /** @category Static Keys */
  static readonly notificationKey = 'Notification';

  /** @category Static Keys */
  static readonly notificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly notificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly excludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly scriptKey = 'Script';

  /** @category Static Keys */
  static readonly notificationTypeKey = 'NotificationType';

  /** @category Static Keys */
  static readonly notificationTypeIdKey = 'ID';

  /** @category Static Keys */
  static readonly notificationTypeNameKey = 'Name';

  /** @category Static Keys */
  static readonly excludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly krAcquaintanceActionRolesName = 'KrAcquaintanceActionRoles';

  //#endregion

  //#region props

  get recipients(): StorageArray<WorkflowActionRoleRowSettings> | string | null {
    return this.tryGetBindingArray(KrAcquaintanceActionSettings.krAcquaintanceActionRolesName, x =>
      WorkflowActionSettingsRowStorageBase.factory(
        WorkflowActionRoleRowSettings,
        this.action,
        this.actionState,
        x
      )
    );
  }
  @undoredo.array(true)
  set recipients(value: Array<WorkflowActionRoleRowSettings> | string) {
    this.setBindingArray(KrAcquaintanceActionSettings.krAcquaintanceActionRolesName, value);
  }

  get sender(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
      )?.tryGetKeyPair(
        KrAcquaintanceActionSettings.senderKey,
        KrAcquaintanceActionSettings.senderIdKey,
        KrAcquaintanceActionSettings.senderNameKey
      ) ?? null
    );
  }
  @undoredo()
  set sender(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
    ).setBindingKeyPair(
      KrAcquaintanceActionSettings.senderKey,
      KrAcquaintanceActionSettings.senderIdKey,
      KrAcquaintanceActionSettings.senderNameKey,
      value
    );
  }

  get comment(): string | null {
    return (
      this.tryGetSubObject(
        KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
      )?.tryGetValue(KrAcquaintanceActionSettings.commentKey) ?? null
    );
  }
  @undoredo()
  set comment(value: string | null) {
    this.getSubObject(KrAcquaintanceActionSettings.krAcquaintanceActionSectionName).setBindingField(
      KrAcquaintanceActionSettings.commentKey,
      value,
      FieldType.String
    );
  }

  get aliasMetadata(): string | null {
    return (
      this.tryGetSubObject(
        KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
      )?.tryGetValue(KrAcquaintanceActionSettings.aliasMetadataKey) ?? null
    );
  }
  @undoredo()
  set aliasMetadata(value: string | null) {
    this.getSubObject(KrAcquaintanceActionSettings.krAcquaintanceActionSectionName).setBindingField(
      KrAcquaintanceActionSettings.aliasMetadataKey,
      value,
      FieldType.String
    );
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
      )?.tryGetKeyPair(
        KrAcquaintanceActionSettings.notificationKey,
        KrAcquaintanceActionSettings.notificationIdKey,
        KrAcquaintanceActionSettings.notificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
    ).setBindingKeyPair(
      KrAcquaintanceActionSettings.notificationKey,
      KrAcquaintanceActionSettings.notificationIdKey,
      KrAcquaintanceActionSettings.notificationNameKey,
      value
    );
  }

  get excludeDeputies(): boolean {
    return (
      this.tryGetSubObject(
        KrAcquaintanceActionSettings.krAcquaintanceActionSectionName
      )?.tryGetValue(KrAcquaintanceActionSettings.excludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set excludeDeputies(value: boolean) {
    this.getSubObject(KrAcquaintanceActionSettings.krAcquaintanceActionSectionName).setBindingField(
      KrAcquaintanceActionSettings.excludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrAcquaintanceActionSettings {
    return new KrAcquaintanceActionSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
