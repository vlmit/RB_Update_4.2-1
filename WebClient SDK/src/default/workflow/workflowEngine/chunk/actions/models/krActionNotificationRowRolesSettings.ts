import { IStorage, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { WorkflowActionSettingsRowStorageBase } from 'tessa/ui/workflow/chunk';

/** Параметры действия, расположенные в таблицах "Варианты завершения" и "Варианты завершения действия" в списке "Получатели". */
export class KrActionNotificationRowRolesSettings extends WorkflowActionSettingsRowStorageBase<KrActionNotificationRowRolesSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly roleKey = 'Role';

  /** @category Static Keys */
  static readonly roleIdKey = 'ID';

  /** @category Static Keys */
  static readonly roleNameKey = 'Name';

  /** @category Static Keys */
  static readonly optionKey = 'Option';

  /** @category Static Keys */
  static readonly optionRowIdKey = 'RowID';

  //#endregion

  //#region props

  get role(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrActionNotificationRowRolesSettings.roleKey,
      KrActionNotificationRowRolesSettings.roleIdKey,
      KrActionNotificationRowRolesSettings.roleNameKey
    );
  }
  @undoredo()
  set role(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setKeyPair(
      KrActionNotificationRowRolesSettings.roleKey,
      KrActionNotificationRowRolesSettings.roleIdKey,
      KrActionNotificationRowRolesSettings.roleNameKey,
      value
    );
  }

  get optionRowId(): string | null {
    return (
      this.tryGetSubObject(KrActionNotificationRowRolesSettings.optionKey)?.getValue(
        KrActionNotificationRowRolesSettings.optionRowIdKey
      ) ?? null
    );
  }
  set optionRowId(value: string | null) {
    this.set(
      KrActionNotificationRowRolesSettings.optionKey,
      value
        ? {
            [KrActionNotificationRowRolesSettings.optionRowIdKey]: TypedField.createGuid(value)
          }
        : null
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrActionNotificationRowRolesSettings {
    return new KrActionNotificationRowRolesSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
