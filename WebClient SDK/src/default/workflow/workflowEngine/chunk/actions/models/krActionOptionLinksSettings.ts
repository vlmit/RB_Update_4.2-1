import { IStorage, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { WorkflowActionLinksSettingsBase } from 'tessa/ui/workflow/chunk';

/** Параметры действия, расположенные в таблице "Варианты завершения действия" в списке "Переходы". */
export class KrActionOptionLinksSettings extends WorkflowActionLinksSettingsBase<KrActionOptionLinksSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly actionOptionKey = 'ActionOption';

  /** @category Static Keys */
  static readonly actionOptionRowIdKey = 'RowID';

  //#endregion

  //#region props

  get actionOptionRowId(): string | null {
    return (
      this.tryGetSubObject(KrActionOptionLinksSettings.actionOptionKey)?.getValue(
        KrActionOptionLinksSettings.actionOptionRowIdKey
      ) ?? null
    );
  }
  @undoredo()
  set actionOptionRowId(value: string | null) {
    this.set(
      KrActionOptionLinksSettings.actionOptionKey,
      value
        ? {
            [KrActionOptionLinksSettings.actionOptionRowIdKey]: TypedField.createGuid(value)
          }
        : null
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrActionOptionLinksSettings {
    return new KrActionOptionLinksSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
