import { FieldType, IStorage, StorageArray, TypedField } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import {
  WorkflowActionSettingsStorageBase,
  WorkflowTaskActionEventRowSettings,
  WorkflowActionRoleRowSettings
} from 'tessa/ui/workflow/chunk';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';

/** Параметры действия "Доработка". */
export class KrAmendingActionSettings extends WorkflowActionSettingsStorageBase<KrAmendingActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krAmendingActionVirtualSectionName = 'KrAmendingActionVirtual';

  /** @category Static Keys */
  static readonly weTaskActionNotificationRolesSectionName = 'WeTaskActionNotificationRoles';

  /** @category Static Keys */
  static readonly weTaskActionEventsSectionName = 'WeTaskActionEvents';

  /** @category Static Keys */
  static readonly roleKey = 'Role';

  /** @category Static Keys */
  static readonly roleIdKey = 'ID';

  /** @category Static Keys */
  static readonly roleNameKey = 'Name';

  /** @category Static Keys */
  static readonly authorKey = 'Author';

  /** @category Static Keys */
  static readonly authorIdKey = 'ID';

  /** @category Static Keys */
  static readonly authorNameKey = 'Name';

  /** @category Static Keys */
  static readonly digestKey = 'Digest';

  /** @category Static Keys */
  static readonly kindKey = 'Kind';

  /** @category Static Keys */
  static readonly kindIdKey = 'ID';

  /** @category Static Keys */
  static readonly kindCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly resultKey = 'Result';

  /** @category Static Keys */
  static readonly periodKey = 'Period';

  /** @category Static Keys */
  static readonly plannedKey = 'Planned';

  /** @category Static Keys */
  static readonly isIncrementCycleKey = 'IsIncrementCycle';

  /** @category Static Keys */
  static readonly isChangeStateKey = 'IsChangeState';

  /** @category Static Keys */
  static readonly hasEditApprovalSchemeAccessKey = 'HasEditApprovalSchemeAccess';

  /** @category Static Keys */
  static readonly initTaskScriptKey = 'InitTaskScript';

  /** @category Static Keys */
  static readonly completeOptionTaskScriptKey = 'CompleteOptionTaskScript';

  /** @category Static Keys */
  static readonly notificationKey = 'Notification';

  /** @category Static Keys */
  static readonly notificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly notificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly excludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly excludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly notificationScriptKey = 'NotificationScript';

  /** @category Static Keys */
  static readonly completeOptionNotificationKey = 'CompleteOptionNotification';

  /** @category Static Keys */
  static readonly completeOptionNotificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly completeOptionNotificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly completeOptionSendToPerformerKey = 'CompleteOptionSendToPerformer';

  /** @category Static Keys */
  static readonly completeOptionSendToAuthorKey = 'CompleteOptionSendToAuthor';

  /** @category Static Keys */
  static readonly completeOptionExcludeDeputiesKey = 'CompleteOptionExcludeDeputies';

  /** @category Static Keys */
  static readonly completeOptionExcludeSubscribersKey = 'CompleteOptionExcludeSubscribers';

  /** @category Static Keys */
  static readonly completeOptionNotificationScriptKey = 'CompleteOptionNotificationScript';

  //#endregion

  //#region props

  get role(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetKeyPair(
        KrAmendingActionSettings.roleKey,
        KrAmendingActionSettings.roleIdKey,
        KrAmendingActionSettings.roleNameKey
      ) ?? null
    );
  }
  @undoredo()
  set role(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAmendingActionSettings.krAmendingActionVirtualSectionName
    ).setBindingKeyPair(
      KrAmendingActionSettings.roleKey,
      KrAmendingActionSettings.roleIdKey,
      KrAmendingActionSettings.roleNameKey,
      value
    );
  }

  get author(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetKeyPair(
        KrAmendingActionSettings.authorKey,
        KrAmendingActionSettings.authorIdKey,
        KrAmendingActionSettings.authorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set author(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAmendingActionSettings.krAmendingActionVirtualSectionName
    ).setBindingKeyPair(
      KrAmendingActionSettings.authorKey,
      KrAmendingActionSettings.authorIdKey,
      KrAmendingActionSettings.authorNameKey,
      value
    );
  }

  get digest(): string | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.digestKey) ?? null
    );
  }
  @undoredo()
  set digest(value: string | null) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.digestKey,
      value,
      FieldType.String
    );
  }

  get kind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetKeyPair(
        KrAmendingActionSettings.kindKey,
        KrAmendingActionSettings.kindIdKey,
        KrAmendingActionSettings.kindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set kind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAmendingActionSettings.krAmendingActionVirtualSectionName
    ).setBindingKeyPair(
      KrAmendingActionSettings.kindKey,
      KrAmendingActionSettings.kindIdKey,
      KrAmendingActionSettings.kindCaptionKey,
      value
    );
  }

  get result(): string | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.resultKey) ?? null
    );
  }
  @undoredo()
  set result(value: string | null) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.resultKey,
      value,
      FieldType.String
    );
  }

  get period(): number | string | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.periodKey) ?? null
    );
  }
  @undoredo()
  set period(value: number | string | null) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.periodKey,
      value,
      FieldType.Double
    );
  }

  get planned(): string | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.plannedKey) ?? null
    );
  }
  @undoredo()
  set planned(value: string | null) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.plannedKey,
      value,
      FieldType.DateTime
    );
  }

  get isIncrementCycle(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.isIncrementCycleKey) ?? false
    );
  }
  @undoredo()
  set isIncrementCycle(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.isIncrementCycleKey,
      value,
      FieldType.Boolean
    );
  }

  get isChangeState(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.isChangeStateKey) ?? false
    );
  }
  @undoredo()
  set isChangeState(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.isChangeStateKey,
      value,
      FieldType.Boolean
    );
  }

  get hasEditApprovalSchemeAccess(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.hasEditApprovalSchemeAccessKey) ?? false
    );
  }
  @undoredo()
  set hasEditApprovalSchemeAccess(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.hasEditApprovalSchemeAccessKey,
      value,
      FieldType.Boolean
    );
  }

  get initTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
        ?.tryGetValue(KrAmendingActionSettings.initTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set initTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
      .setBindingField(KrAmendingActionSettings.initTaskScriptKey, value, FieldType.String);
  }

  get completeOptionTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
        ?.tryGetValue(KrAmendingActionSettings.completeOptionTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set completeOptionTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
      .setBindingField(
        KrAmendingActionSettings.completeOptionTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetKeyPair(
        KrAmendingActionSettings.notificationKey,
        KrAmendingActionSettings.notificationIdKey,
        KrAmendingActionSettings.notificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAmendingActionSettings.krAmendingActionVirtualSectionName
    ).setBindingKeyPair(
      KrAmendingActionSettings.notificationKey,
      KrAmendingActionSettings.notificationIdKey,
      KrAmendingActionSettings.notificationNameKey,
      value
    );
  }

  get excludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.excludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.excludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get excludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.excludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.excludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get notificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
        ?.tryGetValue(KrAmendingActionSettings.notificationScriptKey) ?? null
    );
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
      .setBindingField(KrAmendingActionSettings.notificationScriptKey, value, FieldType.String);
  }

  get completeOptionNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetKeyPair(
        KrAmendingActionSettings.completeOptionNotificationKey,
        KrAmendingActionSettings.completeOptionNotificationIdKey,
        KrAmendingActionSettings.completeOptionNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set completeOptionNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrAmendingActionSettings.krAmendingActionVirtualSectionName
    ).setBindingKeyPair(
      KrAmendingActionSettings.completeOptionNotificationKey,
      KrAmendingActionSettings.completeOptionNotificationIdKey,
      KrAmendingActionSettings.completeOptionNotificationNameKey,
      value
    );
  }

  get recipients(): StorageArray<WorkflowActionRoleRowSettings> | string | null {
    return this.tryGetBindingArray(
      KrAmendingActionSettings.weTaskActionNotificationRolesSectionName,
      x =>
        WorkflowActionRoleRowSettings.factory(
          WorkflowActionRoleRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set recipients(value: StorageArray<WorkflowActionRoleRowSettings> | string | null) {
    this.setBindingArray(KrAmendingActionSettings.weTaskActionNotificationRolesSectionName, value);
  }

  get completeOptionSendToPerformer(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.completeOptionSendToPerformerKey) ?? false
    );
  }
  @undoredo()
  set completeOptionSendToPerformer(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.completeOptionSendToPerformerKey,
      value,
      FieldType.Boolean
    );
  }

  get completeOptionSendToAuthor(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.completeOptionSendToAuthorKey) ?? false
    );
  }
  @undoredo()
  set completeOptionSendToAuthor(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.completeOptionSendToAuthorKey,
      value,
      FieldType.Boolean
    );
  }

  get completeOptionExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.completeOptionExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set completeOptionExcludeDeputies(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.completeOptionExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get completeOptionExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrAmendingActionSettings.krAmendingActionVirtualSectionName
      )?.tryGetValue(KrAmendingActionSettings.completeOptionExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set completeOptionExcludeSubscribers(value: boolean | string) {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).setBindingField(
      KrAmendingActionSettings.completeOptionExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get completeOptionNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
        ?.tryGetValue(KrAmendingActionSettings.completeOptionNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set completeOptionNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName)
      .setBindingField(
        KrAmendingActionSettings.completeOptionNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get events(): StorageArray<WorkflowTaskActionEventRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrAmendingActionSettings.weTaskActionEventsSectionName,
      x =>
        WorkflowTaskActionEventRowSettings.factory(
          WorkflowTaskActionEventRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set events(value: StorageArray<WorkflowTaskActionEventRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrAmendingActionSettings.weTaskActionEventsSectionName,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrAmendingActionSettings {
    return new KrAmendingActionSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).init(
      KrAmendingActionSettings.isIncrementCycleKey,
      TypedField.trueBoolean
    );
    this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).init(
      KrAmendingActionSettings.isChangeStateKey,
      TypedField.trueBoolean
    );
    if (!this.planned) {
      this.getSubObject(KrAmendingActionSettings.krAmendingActionVirtualSectionName).initField(
        KrAmendingActionSettings.periodKey,
        1,
        FieldType.Double
      );
    }
  }

  //#endregion
}
