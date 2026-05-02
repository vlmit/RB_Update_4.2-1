import { FieldType, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { WorkflowActionSettingsRowStorageBase } from 'tessa/ui/workflow/chunk';

/** Базовые параметры действия, расположенные в строках таблицы "Варианты завершения действия". */
export abstract class KrActionOptionRowSettingsBase<
  T extends WorkflowActionSettingsRowStorageBase<T>
> extends WorkflowActionSettingsRowStorageBase<T> {
  //#region keys

  /** @category Static Keys */
  static readonly actionOptionKey = 'ActionOption';

  /** @category Static Keys */
  static readonly actionOptionIdKey = 'ID';

  /** @category Static Keys */
  static readonly actionOptionCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly orderKey = 'Order';

  //#endregion

  //#region props

  get actionOption(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrActionOptionRowSettingsBase.actionOptionKey,
      KrActionOptionRowSettingsBase.actionOptionIdKey,
      KrActionOptionRowSettingsBase.actionOptionCaptionKey
    );
  }
  @undoredo()
  set actionOption(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setKeyPair(
      KrActionOptionRowSettingsBase.actionOptionKey,
      KrActionOptionRowSettingsBase.actionOptionIdKey,
      KrActionOptionRowSettingsBase.actionOptionCaptionKey,
      value
    );
  }

  get order(): number {
    return this.getValue(KrActionOptionRowSettingsBase.orderKey);
  }
  @undoredo()
  set order(value: number) {
    this.setField(KrActionOptionRowSettingsBase.orderKey, value, FieldType.Int);
  }

  //#endregion

  //#region base overrides

  protected override initialize(): void {
    super.initialize();
    this.init(KrActionOptionRowSettingsBase.orderKey, TypedField.zeroNumber);
  }

  //#endregion
}
