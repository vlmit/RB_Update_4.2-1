import { IStorage, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { WorkflowActionLinksSettingsBase } from 'tessa/ui/workflow/chunk';

/** Параметры действия "Настраиваемое задание", расположенные в таблице "Варианты завершения" в списке "Переходы". */
export class KrUniversalTaskActionOptionRowLinksSettings extends WorkflowActionLinksSettingsBase<KrUniversalTaskActionOptionRowLinksSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly buttonKey = 'Button';

  /** @category Static Keys */
  static readonly buttonRowIdKey = 'RowID';

  //#endregion

  //#region props

  get buttonRowId(): string | null {
    return (
      this.tryGetSubObject(KrUniversalTaskActionOptionRowLinksSettings.buttonKey)?.getValue(
        KrUniversalTaskActionOptionRowLinksSettings.buttonRowIdKey
      ) ?? null
    );
  }
  @undoredo()
  set buttonRowId(value: string | null) {
    this.set(
      KrUniversalTaskActionOptionRowLinksSettings.buttonKey,
      value
        ? {
            [KrUniversalTaskActionOptionRowLinksSettings.buttonRowIdKey]:
              TypedField.createGuid(value)
          }
        : null
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(
    storage: IStorage
  ): KrUniversalTaskActionOptionRowLinksSettings {
    return new KrUniversalTaskActionOptionRowLinksSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
