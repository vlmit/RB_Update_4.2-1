import { FieldType, IStorage, StorageArray } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import {
  WorkflowActionStateStorage,
  WorkflowActionStorage,
  WorkflowTaskActionEventRowSettings,
  WorkflowTaskActionFunctionRoleRowSettings,
  WorkflowTaskActionNotificationRowSettings,
  WorkflowTaskActionOptionNotificationRowSettings,
  WorkflowTasksActionSettingsBase
} from 'tessa/ui/workflow/chunk';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { KrUniversalTaskActionOptionRowSettings } from './krUniversalTaskActionOptionRowSettings';
import { KrUniversalTaskActionButtonTaskRoleRowSettings } from './krUniversalTaskActionButtonTaskRoleRowSettings';
import { KrUniversalTaskActionOptionRowLinksSettings } from './krUniversalTaskActionOptionRowLinksSettings';

/** Параметры действия "Настраиваемое задание". */
export class KrUniversalTaskActionSettings extends WorkflowTasksActionSettingsBase<KrUniversalTaskActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krUniversalTaskActionVirtualSectionName = 'KrUniversalTaskActionVirtual';

  /** @category Static Keys */
  static readonly weTaskActionTaskRolesSectionName = 'WeTaskActionTaskRoles';

  /** @category Static Keys */
  static readonly weTaskActionEventsSectionName = 'WeTaskActionEvents';

  /** @category Static Keys */
  static readonly weTaskActionNotificationsSectionName = 'WeTaskActionNotifications';

  /** @category Static Keys */
  static readonly krUniversalTaskActionButtonsVirtualSectionName =
    'KrUniversalTaskActionButtonsVirtual';

  /** @category Static Keys */
  static readonly krUniversalTaskActionButtonTaskRolesVirtualSectionName =
    'KrUniversalTaskActionButtonTaskRolesVirtual';

  /** @category Static Keys */
  static readonly krUniversalTaskActionButtonLinksVirtualSectionName =
    'KrUniversalTaskActionButtonLinksVirtual';

  /** @category Static Keys */
  static readonly weTaskActionCompletionNotificationsSectionName =
    'WeTaskActionCompletionNotifications';

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
  static readonly canEditCardKey = 'CanEditCard';

  /** @category Static Keys */
  static readonly canEditAnyFilesKey = 'CanEditAnyFiles';

  /** @category Static Keys */
  static readonly initTaskScriptKey = 'InitTaskScript';

  //#endregion

  //#region ctor

  constructor(
    action: WorkflowActionStorage,
    actionState?: WorkflowActionStateStorage,
    storage: IStorage = {}
  ) {
    super(action, actionState, storage);

    this.registerLinkPropertyName('completeOptionLinks', 'link');
  }

  //#endregion

  //#region props

  get functionRoles(): StorageArray<WorkflowTaskActionFunctionRoleRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.weTaskActionTaskRolesSectionName,
      x =>
        WorkflowTaskActionFunctionRoleRowSettings.factory(
          WorkflowTaskActionFunctionRoleRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set functionRoles(value: StorageArray<WorkflowTaskActionFunctionRoleRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrUniversalTaskActionSettings.weTaskActionTaskRolesSectionName,
      value
    );
  }

  get digest(): string | null {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetValue(KrUniversalTaskActionSettings.digestKey) ?? null
    );
  }
  @undoredo()
  set digest(value: string | null) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingField(KrUniversalTaskActionSettings.digestKey, value, FieldType.String);
  }

  get kind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetKeyPair(
        KrUniversalTaskActionSettings.kindKey,
        KrUniversalTaskActionSettings.kindIdKey,
        KrUniversalTaskActionSettings.kindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set kind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingKeyPair(
      KrUniversalTaskActionSettings.kindKey,
      KrUniversalTaskActionSettings.kindIdKey,
      KrUniversalTaskActionSettings.kindCaptionKey,
      value
    );
  }

  get result(): string | null {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetValue(KrUniversalTaskActionSettings.resultKey) ?? null
    );
  }
  @undoredo()
  set result(value: string | null) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingField(KrUniversalTaskActionSettings.resultKey, value, FieldType.String);
  }

  get period(): number | string | null {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetValue(KrUniversalTaskActionSettings.periodKey) ?? null
    );
  }
  @undoredo()
  set period(value: number | string | null) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingField(KrUniversalTaskActionSettings.periodKey, value, FieldType.Double);
  }

  get planned(): string | null {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetValue(KrUniversalTaskActionSettings.plannedKey) ?? null
    );
  }
  @undoredo()
  set planned(value: string | null) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingField(KrUniversalTaskActionSettings.plannedKey, value, FieldType.DateTime);
  }

  get canEditCard(): boolean | string {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetValue(KrUniversalTaskActionSettings.canEditCardKey) ?? false
    );
  }
  @undoredo()
  set canEditCard(value: boolean | string) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingField(KrUniversalTaskActionSettings.canEditCardKey, value, FieldType.Boolean);
  }

  get canEditAnyFiles(): boolean | string {
    return (
      this.tryGetSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      )?.tryGetValue(KrUniversalTaskActionSettings.canEditAnyFilesKey) ?? false
    );
  }
  @undoredo()
  set canEditAnyFiles(value: boolean | string) {
    this.getSubObject(
      KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
    ).setBindingField(KrUniversalTaskActionSettings.canEditAnyFilesKey, value, FieldType.Boolean);
  }

  get initTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName)
        ?.tryGetValue(KrUniversalTaskActionSettings.initTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set initTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName)
      .setBindingField(KrUniversalTaskActionSettings.initTaskScriptKey, value, FieldType.String);
  }

  get taskNotifications(): StorageArray<WorkflowTaskActionNotificationRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.weTaskActionNotificationsSectionName,
      x =>
        WorkflowTaskActionNotificationRowSettings.factory(
          WorkflowTaskActionNotificationRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set taskNotifications(value: StorageArray<WorkflowTaskActionNotificationRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrUniversalTaskActionSettings.weTaskActionNotificationsSectionName,
      value
    );
  }

  get completeOptions(): StorageArray<KrUniversalTaskActionOptionRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.krUniversalTaskActionButtonsVirtualSectionName,
      x =>
        KrUniversalTaskActionOptionRowSettings.factory(
          KrUniversalTaskActionOptionRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptions(value: StorageArray<KrUniversalTaskActionOptionRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrUniversalTaskActionSettings.krUniversalTaskActionButtonsVirtualSectionName,
      value
    );
  }

  get completionOptionsFunctionRoles(): StorageArray<KrUniversalTaskActionButtonTaskRoleRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.krUniversalTaskActionButtonTaskRolesVirtualSectionName,
      x =>
        KrUniversalTaskActionButtonTaskRoleRowSettings.factory(
          KrUniversalTaskActionButtonTaskRoleRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completionOptionsFunctionRoles(
    value: StorageArray<KrUniversalTaskActionButtonTaskRoleRowSettings> | null
  ) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrUniversalTaskActionSettings.krUniversalTaskActionButtonTaskRolesVirtualSectionName,
      value
    );
  }

  get completeOptionLinks(): StorageArray<KrUniversalTaskActionOptionRowLinksSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.krUniversalTaskActionButtonLinksVirtualSectionName,
      x =>
        KrUniversalTaskActionOptionRowLinksSettings.factory(
          KrUniversalTaskActionOptionRowLinksSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptionLinks(value: Array<KrUniversalTaskActionOptionRowLinksSettings>) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrUniversalTaskActionSettings.krUniversalTaskActionButtonLinksVirtualSectionName,
      value
    );
  }

  get completeOptionNotifications(): StorageArray<WorkflowTaskActionOptionNotificationRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.weTaskActionCompletionNotificationsSectionName,
      x =>
        WorkflowTaskActionOptionNotificationRowSettings.factory(
          WorkflowTaskActionOptionNotificationRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptionNotifications(value: Array<WorkflowTaskActionOptionNotificationRowSettings>) {
    this.setStorageValue(
      KrUniversalTaskActionSettings.weTaskActionCompletionNotificationsSectionName,
      value
    );
  }

  get events(): StorageArray<WorkflowTaskActionEventRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrUniversalTaskActionSettings.weTaskActionEventsSectionName,
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
      KrUniversalTaskActionSettings.weTaskActionEventsSectionName,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrUniversalTaskActionSettings {
    return new KrUniversalTaskActionSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    if (!this.planned) {
      this.getSubObject(
        KrUniversalTaskActionSettings.krUniversalTaskActionVirtualSectionName
      ).initField(KrUniversalTaskActionSettings.periodKey, 1, FieldType.Double);
    }
  }

  //#endregion
}
