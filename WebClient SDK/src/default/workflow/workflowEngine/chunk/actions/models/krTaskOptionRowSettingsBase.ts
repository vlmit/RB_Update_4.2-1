import { FieldType, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValueCaptionTriple, IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { WorkflowActionSettingsRowStorageBase } from 'tessa/ui/workflow/chunk';

/** Базовые параметры действия, расположенные в строках таблицы "Варианты завершения". */
export abstract class KrTaskOptionRowSettingsBase<
  T extends WorkflowActionSettingsRowStorageBase<T>
> extends WorkflowActionSettingsRowStorageBase<T> {
  //#region keys

  /** @category Static Keys */
  static readonly optionKey = 'Option';

  /** @category Static Keys */
  static readonly optionIdKey = 'ID';

  /** @category Static Keys */
  static readonly optionCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly orderKey = 'Order';

  /** @category Static Keys */
  static readonly taskTypeKey = 'TaskType';

  /** @category Static Keys */
  static readonly taskTypeIdKey = 'ID';

  /** @category Static Keys */
  static readonly taskTypeCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly taskTypeNameKey = 'Name';

  //#endregion

  //#region props

  get option(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrTaskOptionRowSettingsBase.optionKey,
      KrTaskOptionRowSettingsBase.optionIdKey,
      KrTaskOptionRowSettingsBase.optionCaptionKey
    );
  }
  @undoredo()
  set option(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setKeyPair(
      KrTaskOptionRowSettingsBase.optionKey,
      KrTaskOptionRowSettingsBase.optionIdKey,
      KrTaskOptionRowSettingsBase.optionCaptionKey,
      value
    );
  }

  get order(): number {
    return this.getValue(KrTaskOptionRowSettingsBase.orderKey);
  }
  @undoredo()
  set order(value: number) {
    this.setField(KrTaskOptionRowSettingsBase.orderKey, value, FieldType.Int);
  }

  get taskType(): IReadOnlyKeyValueCaptionTriple<string, string | null, string | null> | null {
    return this.tryGetKeyTriple(
      KrTaskOptionRowSettingsBase.taskTypeKey,
      KrTaskOptionRowSettingsBase.taskTypeIdKey,
      KrTaskOptionRowSettingsBase.taskTypeNameKey,
      KrTaskOptionRowSettingsBase.taskTypeCaptionKey
    );
  }
  @undoredo()
  set taskType(value: IReadOnlyKeyValueCaptionTriple<string, string | null, string | null> | null) {
    this.setKeyTriple(
      KrTaskOptionRowSettingsBase.taskTypeKey,
      KrTaskOptionRowSettingsBase.taskTypeIdKey,
      KrTaskOptionRowSettingsBase.taskTypeNameKey,
      KrTaskOptionRowSettingsBase.taskTypeCaptionKey,
      value
    );
  }

  //#region props

  //#region base overrides

  protected override initialize(): void {
    super.initialize();
    this.init(KrTaskOptionRowSettingsBase.orderKey, TypedField.zeroNumber);
  }

  //#endregion
}
