import { IStorage, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { WorkflowActionSettingsRowStorageBase } from 'tessa/ui/workflow/chunk';

/**
 * Параметры действия "Настраиваемое задание", расположенные в таблице "Варианты завершения" в списке "Функциональные роли".
 */
export class KrUniversalTaskActionButtonTaskRoleRowSettings extends WorkflowActionSettingsRowStorageBase<KrUniversalTaskActionButtonTaskRoleRowSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly taskRoleKey = 'TaskRole';

  /** @category Static Keys */
  static readonly taskRoleIdKey = 'ID';

  /** @category Static Keys */
  static readonly taskRoleCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly taskButtonKey = 'TaskButton';

  /** @category Static Keys */
  static readonly taskButtonRowIdKey = 'RowID';

  //#endregion

  //#region props

  get taskRole(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskRoleKey,
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskRoleIdKey,
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskRoleCaptionKey
    );
  }
  @undoredo()
  set taskRole(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setKeyPair(
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskRoleKey,
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskRoleIdKey,
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskRoleCaptionKey,
      value
    );
  }

  get taskButtonRowId(): string | null {
    return (
      this.tryGetSubObject(KrUniversalTaskActionButtonTaskRoleRowSettings.taskButtonKey)?.getValue(
        KrUniversalTaskActionButtonTaskRoleRowSettings.taskButtonRowIdKey
      ) ?? null
    );
  }
  set taskButtonRowId(value: string | null) {
    this.set(
      KrUniversalTaskActionButtonTaskRoleRowSettings.taskButtonKey,
      value
        ? {
            [KrUniversalTaskActionButtonTaskRoleRowSettings.taskButtonRowIdKey]:
              TypedField.createGuid(value)
          }
        : null
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(
    storage: IStorage
  ): KrUniversalTaskActionButtonTaskRoleRowSettings {
    return new KrUniversalTaskActionButtonTaskRoleRowSettings(
      this.action,
      this.actionState,
      storage
    );
  }

  //#endregion
}
