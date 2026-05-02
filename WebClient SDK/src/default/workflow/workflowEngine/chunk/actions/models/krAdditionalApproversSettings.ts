import { FieldType, IStorage, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { WorkflowActionSettingsRowStorageBase } from 'tessa/ui/workflow/chunk';

/** Параметры действия, содержащие информацию по дополнительным согласующим. */
export class KrAdditionalApproversSettings extends WorkflowActionSettingsRowStorageBase<KrAdditionalApproversSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly mainApproverKey = 'MainApprover';

  /** @category Static Keys */
  static readonly mainApproverRowIdKey = 'RowID';

  /** @category Static Keys */
  static readonly isResponsibleKey = 'IsResponsible';

  /** @category Static Keys */
  static readonly orderKey = 'Order';

  /** @category Static Keys */
  static readonly roleKey = 'Role';

  /** @category Static Keys */
  static readonly roleIdKey = 'ID';

  /** @category Static Keys */
  static readonly roleNameKey = 'Name';

  //#endregion

  //#region props

  get mainApproverRowId(): string | null {
    return (
      this.tryGetSubObject(KrAdditionalApproversSettings.mainApproverKey)?.getValue(
        KrAdditionalApproversSettings.mainApproverRowIdKey
      ) ?? null
    );
  }
  @undoredo()
  set mainApproverRowId(value: string | null) {
    this.set(
      KrAdditionalApproversSettings.mainApproverKey,
      value
        ? {
            [KrAdditionalApproversSettings.mainApproverRowIdKey]: TypedField.createGuid(value)
          }
        : null
    );
  }

  get isResponsible(): boolean {
    return this.tryGetValue(KrAdditionalApproversSettings.isResponsibleKey) ?? false;
  }
  @undoredo()
  set isResponsible(value: boolean) {
    this.setField(KrAdditionalApproversSettings.isResponsibleKey, value, FieldType.Boolean);
  }

  get order(): number {
    return this.getValue(KrAdditionalApproversSettings.orderKey);
  }
  @undoredo()
  set order(value: number) {
    this.setField(KrAdditionalApproversSettings.orderKey, value, FieldType.Int);
  }

  get role(): IReadOnlyKeyValuePair<string, string | null> | null {
    return this.tryGetKeyPair(
      KrAdditionalApproversSettings.roleKey,
      KrAdditionalApproversSettings.roleIdKey,
      KrAdditionalApproversSettings.roleNameKey
    );
  }
  @undoredo()
  set role(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.setKeyPair(
      KrAdditionalApproversSettings.roleKey,
      KrAdditionalApproversSettings.roleIdKey,
      KrAdditionalApproversSettings.roleNameKey,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrAdditionalApproversSettings {
    return new KrAdditionalApproversSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    super.initialize();
    this.init(KrAdditionalApproversSettings.orderKey, TypedField.zeroNumber);
    this.init(KrAdditionalApproversSettings.isResponsibleKey, TypedField.falseBoolean);
  }

  //#endregion
}
