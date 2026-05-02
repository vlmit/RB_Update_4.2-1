import { observable } from 'mobx';
import { FieldType, IStorage, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import {
  WorkflowActionSettingsRowStorageBase,
  WorkflowTaskActionOptionNotificationRowSettings
} from 'tessa/ui/workflow/chunk';
import { KrUniversalTaskActionButtonTaskRoleRowSettings } from './krUniversalTaskActionButtonTaskRoleRowSettings';
import { KrUniversalTaskActionOptionRowLinksSettings } from './krUniversalTaskActionOptionRowLinksSettings';

/** Строка таблицы "Варианты завершения" из действия "Настраиваемое задание". */
export class KrUniversalTaskActionOptionRowSettings extends WorkflowActionSettingsRowStorageBase<KrUniversalTaskActionOptionRowSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly optionIdKey = 'OptionID';

  /** @category Static Keys */
  static readonly captionKey = 'Caption';

  /** @category Static Keys */
  static readonly digestKey = 'Digest';

  /** @category Static Keys */
  static readonly isShowCommentKey = 'IsShowComment';

  /** @category Static Keys */
  static readonly isAdditionalOptionKey = 'IsAdditionalOption';

  /** @category Static Keys */
  static readonly scriptKey = 'Script';

  /** @category Static Keys */
  static readonly orderKey = 'Order';

  //#endregion

  //#region fields

  private readonly _functionRoles =
    observable.array<KrUniversalTaskActionButtonTaskRoleRowSettings>([]);

  private readonly _links = observable.array<KrUniversalTaskActionOptionRowLinksSettings>([]);

  private readonly _optionNotifications =
    observable.array<WorkflowTaskActionOptionNotificationRowSettings>([]);

  //#endregion

  //#region props

  get optionId(): string | null {
    return this.tryGetValue(KrUniversalTaskActionOptionRowSettings.optionIdKey);
  }
  @undoredo()
  set optionId(value: string | null) {
    this.setField(KrUniversalTaskActionOptionRowSettings.optionIdKey, value, FieldType.String);
  }

  get caption(): string | null {
    return this.tryGetValue(KrUniversalTaskActionOptionRowSettings.captionKey);
  }
  @undoredo()
  set caption(value: string | null) {
    this.setField(KrUniversalTaskActionOptionRowSettings.captionKey, value, FieldType.String);
  }

  get functionRoles(): Array<KrUniversalTaskActionButtonTaskRoleRowSettings> {
    return this._functionRoles;
  }

  get digest(): string | null {
    return this.tryGetValue(KrUniversalTaskActionOptionRowSettings.digestKey);
  }
  @undoredo()
  set digest(value: string | null) {
    this.setBindingField(KrUniversalTaskActionOptionRowSettings.digestKey, value, FieldType.String);
  }

  get isShowComment(): boolean | string {
    return this.tryGetValue(KrUniversalTaskActionOptionRowSettings.isShowCommentKey) ?? false;
  }
  @undoredo()
  set isShowComment(value: boolean | string) {
    this.setBindingField(
      KrUniversalTaskActionOptionRowSettings.isShowCommentKey,
      value,
      FieldType.Boolean
    );
  }

  get isAdditionalOption(): boolean | string {
    return this.tryGetValue(KrUniversalTaskActionOptionRowSettings.isAdditionalOptionKey) ?? false;
  }
  @undoredo()
  set isAdditionalOption(value: boolean | string) {
    this.setBindingField(
      KrUniversalTaskActionOptionRowSettings.isAdditionalOptionKey,
      value,
      FieldType.Boolean
    );
  }

  get links(): Array<KrUniversalTaskActionOptionRowLinksSettings> {
    return this._links;
  }

  get script(): string | null {
    return this.tryGetValue(KrUniversalTaskActionOptionRowSettings.scriptKey);
  }
  @undoredo()
  set script(value: string | null) {
    this.setField(KrUniversalTaskActionOptionRowSettings.scriptKey, value, FieldType.String);
  }

  get optionNotifications(): Array<WorkflowTaskActionOptionNotificationRowSettings> {
    return this._optionNotifications;
  }

  get order(): number {
    return this.getValue(KrUniversalTaskActionOptionRowSettings.orderKey);
  }
  @undoredo()
  set order(value: number) {
    this.setField(KrUniversalTaskActionOptionRowSettings.orderKey, value, FieldType.Int);
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(
    storage: IStorage
  ): KrUniversalTaskActionOptionRowSettings {
    return new KrUniversalTaskActionOptionRowSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    super.initialize();
    this.init(KrUniversalTaskActionOptionRowSettings.optionIdKey, TypedField.createNewGuid());
    this.init(KrUniversalTaskActionOptionRowSettings.orderKey, TypedField.zeroNumber);
  }

  //#endregion
}
