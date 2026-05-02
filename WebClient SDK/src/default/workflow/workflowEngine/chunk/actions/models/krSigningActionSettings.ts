import { FieldType, IStorage, StorageArray } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import {
  WorkflowActionSettingsStorageBase,
  WorkflowTaskActionEventRowSettings,
  WorkflowActionStorage,
  WorkflowActionStateStorage,
  WorkflowActionRoleOrderedRowSettings
} from 'tessa/ui/workflow/chunk';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { KrActionNotificationRowRolesSettings } from './krActionNotificationRowRolesSettings';
import { KrTaskOptionRowSettings } from './krTaskOptionRowSettings';
import { KrActionOptionLinksSettings } from './krActionOptionLinksSettings';
import { KrActionOptionRowSettings } from './krActionOptionRowSettings';
import { KrAdditionalApproversSettings } from './krAdditionalApproversSettings';
import { KrFileCategoryRowSettings } from './krFileCategoryRowSettings';

/** Параметры действия "Подписание". */
export class KrSigningActionSettings extends WorkflowActionSettingsStorageBase<KrSigningActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krWeRolesVirtualSectionName = 'KrWeRolesVirtual';

  /** @category Static Keys */
  static readonly krApprovalActionAdditionalPerformersVirtualSectionName =
    'KrApprovalActionAdditionalPerformersVirtual';

  /** @category Static Keys */
  static readonly krApprovalActionNotificationRolesVirtualSectionName =
    'KrApprovalActionNotificationRolesVirtual';

  /** @category Static Keys */
  static readonly krApprovalActionNotificationActionRolesVirtualSectionName =
    'KrApprovalActionNotificationActionRolesVirtual';

  /** @category Static Keys */
  static readonly krApprovalActionOptionsVirtualSectionName = 'KrApprovalActionOptionsVirtual';

  /** @category Static Keys */
  static readonly krApprovalActionOptionsActionVirtualSectionName =
    'KrApprovalActionOptionsActionVirtual';

  /** @category Static Keys */
  static readonly krApprovalActionOptionLinksVirtualSectionName =
    'KrApprovalActionOptionLinksVirtual';

  /** @category Static Keys */
  static readonly weTaskActionEventsSectionName = 'WeTaskActionEvents';

  /** @category Static Keys */
  static readonly krSigningActionFileCategoriesVirtualSectionName =
    'KrSigningActionFileCategoriesVirtual';

  /** @category Static Keys */
  static readonly krSigningActionHiddenFileCategoriesVirtualSectionName =
    'KrSigningActionHiddenFileCategoriesVirtual';

  //#region KrApprovalActionVirtual

  /** @category Static Keys */
  static readonly krSigningActionVirtualSectionName = 'KrSigningActionVirtual';

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
  static readonly isParallelKey = 'IsParallel';

  /** @category Static Keys */
  static readonly allowAdditionalApprovalKey = 'AllowAdditionalApproval';

  /** @category Static Keys */
  static readonly signCardFilesKey = 'SignCardFiles';

  /** @category Static Keys */
  static readonly noCommentDialogKey = 'NoCommentDialog';

  /** @category Static Keys */
  static readonly noSignFilesDialogKey = 'NoSignFilesDialog';

  /** @category Static Keys */
  static readonly doNotSignFileCopiesKey = 'DoNotSignFileCopies';

  /** @category Static Keys */
  static readonly returnWhenApprovedKey = 'ReturnWhenApproved';

  /** @category Static Keys */
  static readonly expectAllSignersKey = 'ExpectAllSigners';

  /** @category Static Keys */
  static readonly changeStateOnStartKey = 'ChangeStateOnStart';

  /** @category Static Keys */
  static readonly changeStateOnEndKey = 'ChangeStateOnEnd';

  /** @category Static Keys */
  static readonly notCreateReturnEditTaskHistoryRecordKey = 'NotCreateReturnEditTaskHistoryRecord';

  /** @category Static Keys */
  static readonly canEditCardKey = 'CanEditCard';

  /** @category Static Keys */
  static readonly canEditAnyFilesKey = 'CanEditAnyFiles';

  /** @category Static Keys */
  static readonly sqlPerformersScriptKey = 'SqlPerformersScript';

  /** @category Static Keys */
  static readonly initTaskScriptKey = 'InitTaskScript';

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

  //#endregion

  //#region KrWeEditInterjectOptionsVirtual

  /** @category Static Keys */
  static readonly krWeEditInterjectOptionsVirtualSectionName = 'KrWeEditInterjectOptionsVirtual';

  /** @category Static Keys */
  static readonly editInterjectRoleKey = 'Role';

  /** @category Static Keys */
  static readonly editInterjectRoleIdKey = 'ID';

  /** @category Static Keys */
  static readonly editInterjectRoleNameKey = 'Name';

  /** @category Static Keys */
  static readonly editInterjectAuthorKey = 'Author';

  /** @category Static Keys */
  static readonly editInterjectAuthorIdKey = 'ID';

  /** @category Static Keys */
  static readonly editInterjectAuthorNameKey = 'Name';

  /** @category Static Keys */
  static readonly editInterjectKindKey = 'Kind';

  /** @category Static Keys */
  static readonly editInterjectKindIdKey = 'ID';

  /** @category Static Keys */
  static readonly editInterjectKindCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly editInterjectDigestKey = 'Digest';

  /** @category Static Keys */
  static readonly editInterjectPeriodKey = 'Period';

  /** @category Static Keys */
  static readonly editInterjectPlannedKey = 'Planned';

  /** @category Static Keys */
  static readonly editInterjectInitTaskScriptKey = 'InitTaskScript';

  /** @category Static Keys */
  static readonly editInterjectNotificationKey = 'Notification';

  /** @category Static Keys */
  static readonly editInterjectNotificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly editInterjectNotificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly editInterjectExcludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly editInterjectExcludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly editInterjectNotificationScriptKey = 'NotificationScript';

  //#endregion

  //#region KrWeAdditionalApprovalOptionsVirtual

  /** @category Static Keys */
  static readonly krWeAdditionalApprovalOptionsVirtualSectionName =
    'KrWeAdditionalApprovalOptionsVirtual';

  /** @category Static Keys */
  static readonly additionalApprovalInitTaskScriptKey = 'InitTaskScript';

  /** @category Static Keys */
  static readonly additionalApprovalNotificationKey = 'Notification';

  /** @category Static Keys */
  static readonly additionalApprovalNotificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly additionalApprovalNotificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly additionalApprovalExcludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly additionalApprovalExcludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly additionalApprovalNotificationScriptKey = 'NotificationScript';

  //#endregion

  //#region KrWeRequestCommentOptionsVirtual

  /** @category Static Keys */
  static readonly krWeRequestCommentOptionsVirtualSectionName = 'KrWeRequestCommentOptionsVirtual';

  /** @category Static Keys */
  static readonly requestCommentInitTaskScriptKey = 'InitTaskScript';

  /** @category Static Keys */
  static readonly requestCommentNotificationKey = 'Notification';

  /** @category Static Keys */
  static readonly requestCommentNotificationIdKey = 'ID';

  /** @category Static Keys */
  static readonly requestCommentNotificationNameKey = 'Name';

  /** @category Static Keys */
  static readonly requestCommentExcludeDeputiesKey = 'ExcludeDeputies';

  /** @category Static Keys */
  static readonly requestCommentExcludeSubscribersKey = 'ExcludeSubscribers';

  /** @category Static Keys */
  static readonly requestCommentNotificationScriptKey = 'NotificationScript';

  //#endregion

  //#endregion

  //#region ctor

  constructor(
    action: WorkflowActionStorage,
    actionState?: WorkflowActionStateStorage,
    storage: IStorage = {}
  ) {
    super(action, actionState, storage);

    this.registerLinkPropertyName('actionCompleteOptionsLinks', 'link');
  }

  //#endregion

  //#region props

  get performers(): StorageArray<WorkflowActionRoleOrderedRowSettings> | string {
    return this.getBindingArray(KrSigningActionSettings.krWeRolesVirtualSectionName, x =>
      WorkflowActionRoleOrderedRowSettings.factory(
        WorkflowActionRoleOrderedRowSettings,
        this.action,
        this.actionState,
        x
      )
    );
  }
  @undoredo.array(true)
  set performers(value: StorageArray<WorkflowActionRoleOrderedRowSettings> | string | null) {
    this.setBindingArray(KrSigningActionSettings.krWeRolesVirtualSectionName, value);
  }

  get additionalApprovers(): StorageArray<KrAdditionalApproversSettings> | null {
    return this.tryGetArray(
      KrSigningActionSettings.krApprovalActionAdditionalPerformersVirtualSectionName,
      x =>
        KrAdditionalApproversSettings.factory(
          KrAdditionalApproversSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set additionalApprovers(value: StorageArray<KrAdditionalApproversSettings> | null) {
    this.setStorageValue(
      KrSigningActionSettings.krApprovalActionAdditionalPerformersVirtualSectionName,
      value
    );
  }

  get author(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krSigningActionVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.authorKey,
        KrSigningActionSettings.authorIdKey,
        KrSigningActionSettings.authorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set author(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingKeyPair(
      KrSigningActionSettings.authorKey,
      KrSigningActionSettings.authorIdKey,
      KrSigningActionSettings.authorNameKey,
      value
    );
  }

  get digest(): string | null {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.digestKey
      ) ?? null
    );
  }
  @undoredo()
  set digest(value: string | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.digestKey,
      value,
      FieldType.String
    );
  }

  get kind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krSigningActionVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.kindKey,
        KrSigningActionSettings.kindIdKey,
        KrSigningActionSettings.kindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set kind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingKeyPair(
      KrSigningActionSettings.kindKey,
      KrSigningActionSettings.kindIdKey,
      KrSigningActionSettings.kindCaptionKey,
      value
    );
  }

  get result(): string | null {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.resultKey
      ) ?? null
    );
  }
  @undoredo()
  set result(value: string | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.resultKey,
      value,
      FieldType.String
    );
  }

  get period(): number | string | null {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.periodKey
      ) ?? null
    );
  }
  @undoredo()
  set period(value: number | string | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.periodKey,
      value,
      FieldType.Double
    );
  }

  get planned(): string | null {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.plannedKey
      ) ?? null
    );
  }
  @undoredo()
  set planned(value: string | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.plannedKey,
      value,
      FieldType.DateTime
    );
  }

  get isParallel(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.isParallelKey
      ) ?? false
    );
  }
  @undoredo()
  set isParallel(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.isParallelKey,
      value,
      FieldType.Boolean
    );
  }

  get allowAdditionalApproval(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.allowAdditionalApprovalKey
      ) ?? false
    );
  }
  @undoredo()
  set allowAdditionalApproval(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.allowAdditionalApprovalKey,
      value,
      FieldType.Boolean
    );
  }

  get signCardFiles(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.signCardFilesKey
      ) ?? false
    );
  }
  @undoredo()
  set signCardFiles(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.signCardFilesKey,
      value,
      FieldType.Boolean
    );
  }

  get noCommentDialog(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.noCommentDialogKey
      ) ?? false
    );
  }
  @undoredo()
  set noCommentDialog(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.noCommentDialogKey,
      value,
      FieldType.Boolean
    );
  }

  get noSignFilesDialog(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.noSignFilesDialogKey
      ) ?? false
    );
  }
  @undoredo()
  set noSignFilesDialog(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.noSignFilesDialogKey,
      value,
      FieldType.Boolean
    );
  }

  get doNotSignFileCopies(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.doNotSignFileCopiesKey
      ) ?? false
    );
  }
  @undoredo()
  set doNotSignFileCopies(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.doNotSignFileCopiesKey,
      value,
      FieldType.Boolean
    );
  }

  get signFileCategories(): StorageArray<KrFileCategoryRowSettings> | string | null {
    return this.tryGetBindingArray(
      KrSigningActionSettings.krSigningActionFileCategoriesVirtualSectionName,
      x =>
        KrFileCategoryRowSettings.factory(
          KrFileCategoryRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set signFileCategories(value: StorageArray<KrFileCategoryRowSettings> | string | null) {
    this.setBindingArray(
      KrSigningActionSettings.krSigningActionFileCategoriesVirtualSectionName,
      value
    );
  }

  get signHiddenFileCategories(): StorageArray<KrFileCategoryRowSettings> | string | null {
    return this.tryGetBindingArray(
      KrSigningActionSettings.krSigningActionHiddenFileCategoriesVirtualSectionName,
      x =>
        KrFileCategoryRowSettings.factory(
          KrFileCategoryRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set signHiddenFileCategories(value: StorageArray<KrFileCategoryRowSettings> | string | null) {
    this.setBindingArray(
      KrSigningActionSettings.krSigningActionHiddenFileCategoriesVirtualSectionName,
      value
    );
  }

  get returnWhenApproved(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.returnWhenApprovedKey
      ) ?? false
    );
  }
  @undoredo()
  set returnWhenApproved(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.returnWhenApprovedKey,
      value,
      FieldType.Boolean
    );
  }

  get expectAllSigners(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.expectAllSignersKey
      ) ?? false
    );
  }
  @undoredo()
  set expectAllSigners(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.expectAllSignersKey,
      value,
      FieldType.Boolean
    );
  }

  get changeStateOnStart(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.changeStateOnStartKey
      ) ?? false
    );
  }
  @undoredo()
  set changeStateOnStart(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.changeStateOnStartKey,
      value,
      FieldType.Boolean
    );
  }

  get changeStateOnEnd(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.changeStateOnEndKey
      ) ?? false
    );
  }
  @undoredo()
  set changeStateOnEnd(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.changeStateOnEndKey,
      value,
      FieldType.Boolean
    );
  }

  get notCreateReturnEditTaskHistoryRecord(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.notCreateReturnEditTaskHistoryRecordKey
      ) ?? false
    );
  }
  @undoredo()
  set notCreateReturnEditTaskHistoryRecord(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.notCreateReturnEditTaskHistoryRecordKey,
      value,
      FieldType.Boolean
    );
  }

  get canEditCard(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.canEditCardKey
      ) ?? false
    );
  }
  @undoredo()
  set canEditCard(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.canEditCardKey,
      value,
      FieldType.Boolean
    );
  }

  get canEditAnyFiles(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.canEditAnyFilesKey
      ) ?? false
    );
  }
  @undoredo()
  set canEditAnyFiles(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.canEditAnyFilesKey,
      value,
      FieldType.Boolean
    );
  }

  get sqlPerformersScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.sqlPerformersScriptKey) ?? null
    );
  }
  @undoredo()
  set sqlPerformersScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)
      .setBindingField(KrSigningActionSettings.sqlPerformersScriptKey, value, FieldType.String);
  }

  get initTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.initTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set initTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)
      .setBindingField(KrSigningActionSettings.initTaskScriptKey, value, FieldType.String);
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krSigningActionVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.notificationKey,
        KrSigningActionSettings.notificationIdKey,
        KrSigningActionSettings.notificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingKeyPair(
      KrSigningActionSettings.notificationKey,
      KrSigningActionSettings.notificationIdKey,
      KrSigningActionSettings.notificationNameKey,
      value
    );
  }

  get excludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.excludeDeputiesKey
      ) ?? false
    );
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.excludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get excludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)?.tryGetValue(
        KrSigningActionSettings.excludeSubscribersKey
      ) ?? false
    );
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).setBindingField(
      KrSigningActionSettings.excludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get notificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.notificationScriptKey) ?? null
    );
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName)
      .setField(KrSigningActionSettings.notificationScriptKey, value, FieldType.String);
  }

  get editInterjectRole(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.editInterjectRoleKey,
        KrSigningActionSettings.editInterjectRoleIdKey,
        KrSigningActionSettings.editInterjectRoleNameKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectRole(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrSigningActionSettings.editInterjectRoleKey,
      KrSigningActionSettings.editInterjectRoleIdKey,
      KrSigningActionSettings.editInterjectRoleNameKey,
      value
    );
  }

  get editInterjectAuthor(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.editInterjectAuthorKey,
        KrSigningActionSettings.editInterjectAuthorIdKey,
        KrSigningActionSettings.editInterjectAuthorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectAuthor(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrSigningActionSettings.editInterjectAuthorKey,
      KrSigningActionSettings.editInterjectAuthorIdKey,
      KrSigningActionSettings.editInterjectAuthorNameKey,
      value
    );
  }

  get editInterjectKind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.editInterjectKindKey,
        KrSigningActionSettings.editInterjectKindIdKey,
        KrSigningActionSettings.editInterjectKindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectKind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrSigningActionSettings.editInterjectKindKey,
      KrSigningActionSettings.editInterjectKindIdKey,
      KrSigningActionSettings.editInterjectKindCaptionKey,
      value
    );
  }

  get editInterjectDigest(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.editInterjectDigestKey) ?? null
    );
  }
  @undoredo()
  set editInterjectDigest(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName)
      .setBindingField(KrSigningActionSettings.editInterjectDigestKey, value, FieldType.String);
  }

  get editInterjectPeriod(): number | string | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.editInterjectPeriodKey) ?? null
    );
  }
  @undoredo()
  set editInterjectPeriod(value: number | string | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(KrSigningActionSettings.editInterjectPeriodKey, value, FieldType.Double);
  }

  get editInterjectPlanned(): string | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.editInterjectPlannedKey) ?? null
    );
  }
  @undoredo()
  set editInterjectPlanned(value: string | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(KrSigningActionSettings.editInterjectPlannedKey, value, FieldType.DateTime);
  }

  get editInterjectInitTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.editInterjectInitTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set editInterjectInitTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName)
      .setBindingField(
        KrSigningActionSettings.editInterjectInitTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get editInterjectNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.editInterjectNotificationKey,
        KrSigningActionSettings.editInterjectNotificationIdKey,
        KrSigningActionSettings.editInterjectNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrSigningActionSettings.editInterjectNotificationKey,
      KrSigningActionSettings.editInterjectNotificationIdKey,
      KrSigningActionSettings.editInterjectNotificationNameKey,
      value
    );
  }

  get editInterjectExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.editInterjectExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set editInterjectExcludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(
      KrSigningActionSettings.editInterjectExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get editInterjectExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.editInterjectExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set editInterjectExcludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(
      KrSigningActionSettings.editInterjectExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get editInterjectNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.editInterjectNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set editInterjectNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeEditInterjectOptionsVirtualSectionName)
      .setField(
        KrSigningActionSettings.editInterjectNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get additionalApprovalInitTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.additionalApprovalInitTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set additionalApprovalInitTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
      .setBindingField(
        KrSigningActionSettings.additionalApprovalInitTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get additionalApprovalNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.additionalApprovalNotificationKey,
        KrSigningActionSettings.additionalApprovalNotificationIdKey,
        KrSigningActionSettings.additionalApprovalNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set additionalApprovalNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrSigningActionSettings.additionalApprovalNotificationKey,
      KrSigningActionSettings.additionalApprovalNotificationIdKey,
      KrSigningActionSettings.additionalApprovalNotificationNameKey,
      value
    );
  }

  get additionalApprovalExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.additionalApprovalExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set additionalApprovalExcludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
    ).setBindingField(
      KrSigningActionSettings.additionalApprovalExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get additionalApprovalExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.additionalApprovalExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set additionalApprovalExcludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
    ).setBindingField(
      KrSigningActionSettings.additionalApprovalExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get additionalApprovalNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.additionalApprovalNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set additionalApprovalNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
      .setField(
        KrSigningActionSettings.additionalApprovalNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get requestCommentInitTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.requestCommentInitTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set requestCommentInitTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName)
      .setBindingField(
        KrSigningActionSettings.requestCommentInitTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get requestCommentNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrSigningActionSettings.requestCommentNotificationKey,
        KrSigningActionSettings.requestCommentNotificationIdKey,
        KrSigningActionSettings.requestCommentNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set requestCommentNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrSigningActionSettings.requestCommentNotificationKey,
      KrSigningActionSettings.requestCommentNotificationIdKey,
      KrSigningActionSettings.requestCommentNotificationNameKey,
      value
    );
  }

  get requestCommentExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.requestCommentExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set requestCommentExcludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName
    ).setBindingField(
      KrSigningActionSettings.requestCommentExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get requestCommentExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName
      )?.tryGetValue(KrSigningActionSettings.requestCommentExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set requestCommentExcludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName
    ).setBindingField(
      KrSigningActionSettings.requestCommentExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get requestCommentNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName)
        ?.tryGetValue(KrSigningActionSettings.requestCommentNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set requestCommentNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrSigningActionSettings.krWeRequestCommentOptionsVirtualSectionName)
      .setField(
        KrSigningActionSettings.requestCommentNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get completeOptions(): StorageArray<KrTaskOptionRowSettings> {
    return this.getTemplateSettingsOrThis().getArray(
      KrSigningActionSettings.krApprovalActionOptionsVirtualSectionName,
      x =>
        KrTaskOptionRowSettings.factory(KrTaskOptionRowSettings, this.action, this.actionState, x)
    );
  }
  @undoredo.array(true)
  set completeOptions(value: StorageArray<KrTaskOptionRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrSigningActionSettings.krApprovalActionOptionsVirtualSectionName,
      value
    );
  }

  get completeOptionsNotificationsRecipients(): StorageArray<KrActionNotificationRowRolesSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrSigningActionSettings.krApprovalActionNotificationRolesVirtualSectionName,
      x =>
        KrActionNotificationRowRolesSettings.factory(
          KrActionNotificationRowRolesSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set completeOptionsNotificationsRecipients(
    value: StorageArray<KrActionNotificationRowRolesSettings> | null
  ) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrSigningActionSettings.krApprovalActionNotificationRolesVirtualSectionName,
      value
    );
  }

  get actionCompleteOptions(): StorageArray<KrActionOptionRowSettings> {
    return this.getTemplateSettingsOrThis().getArray(
      KrSigningActionSettings.krApprovalActionOptionsActionVirtualSectionName,
      x =>
        KrActionOptionRowSettings.factory(
          KrActionOptionRowSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set actionCompleteOptions(value: StorageArray<KrActionOptionRowSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrSigningActionSettings.krApprovalActionOptionsActionVirtualSectionName,
      value
    );
  }

  get actionCompleteOptionsLinks(): StorageArray<KrActionOptionLinksSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrSigningActionSettings.krApprovalActionOptionLinksVirtualSectionName,
      x =>
        KrActionOptionLinksSettings.factory(
          KrActionOptionLinksSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set actionCompleteOptionsLinks(value: StorageArray<KrActionOptionLinksSettings> | null) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrSigningActionSettings.krApprovalActionOptionLinksVirtualSectionName,
      value
    );
  }

  get actionCompleteOptionsNotificationsRecipients(): StorageArray<KrActionNotificationRowRolesSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrSigningActionSettings.krApprovalActionNotificationActionRolesVirtualSectionName,
      x =>
        KrActionNotificationRowRolesSettings.factory(
          KrActionNotificationRowRolesSettings,
          this.action,
          this.actionState,
          x
        )
    );
  }
  @undoredo.array(true)
  set actionCompleteOptionsNotificationsRecipients(
    value: StorageArray<KrActionNotificationRowRolesSettings> | null
  ) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrSigningActionSettings.krApprovalActionNotificationActionRolesVirtualSectionName,
      value
    );
  }

  get events(): StorageArray<WorkflowTaskActionEventRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrSigningActionSettings.weTaskActionEventsSectionName,
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
      KrSigningActionSettings.weTaskActionEventsSectionName,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrSigningActionSettings {
    return new KrSigningActionSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    this.init(KrSigningActionSettings.krWeRolesVirtualSectionName, null);
    this.getTemplateSettingsOrThis().init(
      KrSigningActionSettings.krApprovalActionOptionsVirtualSectionName,
      null
    );
    this.getTemplateSettingsOrThis().init(
      KrSigningActionSettings.krApprovalActionOptionsActionVirtualSectionName,
      null
    );
    if (!this.planned) {
      this.getSubObject(KrSigningActionSettings.krSigningActionVirtualSectionName).initField(
        KrSigningActionSettings.periodKey,
        1,
        FieldType.Double
      );
    }
  }

  //#endregion
}
