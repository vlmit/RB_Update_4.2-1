import { FieldType, IStorage, StorageArray } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import {
  WorkflowActionSettingsStorageBase,
  WorkflowActionRoleRowSettings
} from 'tessa/ui/workflow/chunk';

/** Параметры действия "Типовая задача". */
export class KrResolutionActionSettings extends WorkflowActionSettingsStorageBase<KrResolutionActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krResolutionActionVirtualSectionName = 'KrResolutionActionVirtual';

  /** @category Static Keys */
  static readonly krWeRolesVirtualSectionName = 'KrWeRolesVirtual';

  /** @category Static Keys */
  static readonly authorKey = 'Author';

  /** @category Static Keys */
  static readonly authorIdKey = 'ID';

  /** @category Static Keys */
  static readonly authorNameKey = 'Name';

  /** @category Static Keys */
  static readonly controllerKey = 'Controller';

  /** @category Static Keys */
  static readonly controllerIdKey = 'ID';

  /** @category Static Keys */
  static readonly controllerNameKey = 'Name';

  /** @category Static Keys */
  static readonly senderKey = 'Sender';

  /** @category Static Keys */
  static readonly senderIdKey = 'ID';

  /** @category Static Keys */
  static readonly senderNameKey = 'Name';

  /** @category Static Keys */
  static readonly kindKey = 'Kind';

  /** @category Static Keys */
  static readonly kindIdKey = 'ID';

  /** @category Static Keys */
  static readonly kindNameKey = 'Caption';

  /** @category Static Keys */
  static readonly digestKey = 'Digest';

  /** @category Static Keys */
  static readonly isMajorPerformerKey = 'IsMajorPerformer';

  /** @category Static Keys */
  static readonly isMassCreationKey = 'IsMassCreation';

  /** @category Static Keys */
  static readonly periodKey = 'Period';

  /** @category Static Keys */
  static readonly plannedKey = 'Planned';

  /** @category Static Keys */
  static readonly sqlPerformersScriptKey = 'SqlPerformersScript';

  /** @category Static Keys */
  static readonly withControlKey = 'WithControl';

  //#endregion

  //#region props

  get author(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetKeyPair(
        KrResolutionActionSettings.authorKey,
        KrResolutionActionSettings.authorIdKey,
        KrResolutionActionSettings.authorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set author(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingKeyPair(
      KrResolutionActionSettings.authorKey,
      KrResolutionActionSettings.authorIdKey,
      KrResolutionActionSettings.authorNameKey,
      value
    );
  }

  get controller(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetKeyPair(
        KrResolutionActionSettings.controllerKey,
        KrResolutionActionSettings.controllerIdKey,
        KrResolutionActionSettings.controllerNameKey
      ) ?? null
    );
  }
  @undoredo()
  set controller(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingKeyPair(
      KrResolutionActionSettings.controllerKey,
      KrResolutionActionSettings.controllerIdKey,
      KrResolutionActionSettings.controllerNameKey,
      value
    );
  }

  get sender(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetKeyPair(
        KrResolutionActionSettings.senderKey,
        KrResolutionActionSettings.senderIdKey,
        KrResolutionActionSettings.senderNameKey
      ) ?? null
    );
  }
  @undoredo()
  set sender(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingKeyPair(
      KrResolutionActionSettings.senderKey,
      KrResolutionActionSettings.senderIdKey,
      KrResolutionActionSettings.senderNameKey,
      value
    );
  }

  get kind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetKeyPair(
        KrResolutionActionSettings.kindKey,
        KrResolutionActionSettings.kindIdKey,
        KrResolutionActionSettings.kindNameKey
      ) ?? null
    );
  }
  @undoredo()
  set kind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingKeyPair(
      KrResolutionActionSettings.kindKey,
      KrResolutionActionSettings.kindIdKey,
      KrResolutionActionSettings.kindNameKey,
      value
    );
  }

  get digest(): string | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetValue(KrResolutionActionSettings.digestKey) ?? null
    );
  }
  @undoredo()
  set digest(value: string | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingField(KrResolutionActionSettings.digestKey, value, FieldType.String);
  }

  get isMajorPerformer(): boolean | string {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetValue(KrResolutionActionSettings.isMajorPerformerKey) ?? false
    );
  }
  @undoredo()
  set isMajorPerformer(value: boolean | string) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingField(KrResolutionActionSettings.isMajorPerformerKey, value, FieldType.Boolean);
  }

  get isMassCreation(): boolean | string {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetValue(KrResolutionActionSettings.isMassCreationKey) ?? false
    );
  }
  @undoredo()
  set isMassCreation(value: boolean | string) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingField(KrResolutionActionSettings.isMassCreationKey, value, FieldType.Boolean);
  }

  get period(): number | string | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetValue(KrResolutionActionSettings.periodKey) ?? null
    );
  }
  @undoredo()
  set period(value: number | string | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingField(KrResolutionActionSettings.periodKey, value, FieldType.Double);
  }

  get planned(): string | null {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetValue(KrResolutionActionSettings.plannedKey) ?? null
    );
  }
  @undoredo()
  set planned(value: string | null) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingField(KrResolutionActionSettings.plannedKey, value, FieldType.DateTime);
  }

  get sqlPerformersScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrResolutionActionSettings.krResolutionActionVirtualSectionName)
        ?.tryGetValue(KrResolutionActionSettings.sqlPerformersScriptKey) ?? null
    );
  }
  @undoredo()
  set sqlPerformersScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrResolutionActionSettings.krResolutionActionVirtualSectionName)
      .setBindingField(KrResolutionActionSettings.sqlPerformersScriptKey, value, FieldType.String);
  }

  get withControl(): boolean | string {
    return (
      this.tryGetSubObject(
        KrResolutionActionSettings.krResolutionActionVirtualSectionName
      )?.tryGetValue(KrResolutionActionSettings.withControlKey) ?? false
    );
  }
  @undoredo()
  set withControl(value: boolean | string) {
    this.getSubObject(
      KrResolutionActionSettings.krResolutionActionVirtualSectionName
    ).setBindingField(KrResolutionActionSettings.withControlKey, value, FieldType.Boolean);
  }

  get performers(): StorageArray<WorkflowActionRoleRowSettings> | string {
    return this.getBindingArray(KrResolutionActionSettings.krWeRolesVirtualSectionName, x =>
      WorkflowActionRoleRowSettings.factory(
        WorkflowActionRoleRowSettings,
        this.action,
        this.actionState,
        x
      )
    );
  }
  @undoredo.array(true)
  set performers(value: StorageArray<WorkflowActionRoleRowSettings> | string) {
    this.setBindingArray(KrResolutionActionSettings.krWeRolesVirtualSectionName, value);
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrResolutionActionSettings {
    return new KrResolutionActionSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    this.init(KrResolutionActionSettings.krWeRolesVirtualSectionName, null);
    if (!this.planned) {
      this.getSubObject(KrResolutionActionSettings.krResolutionActionVirtualSectionName).initField(
        KrResolutionActionSettings.periodKey,
        1,
        FieldType.Double
      );
    }
  }

  //#endregion
}
