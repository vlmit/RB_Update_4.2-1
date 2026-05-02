import { observable, runInAction } from 'mobx';
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

/** Параметры действия "Согласование". */
export class KrApprovalActionSettings extends WorkflowActionSettingsStorageBase<KrApprovalActionSettings> {
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

  //#region KrApprovalActionVirtual

  /** @category Static Keys */
  static readonly krApprovalActionVirtualSectionName = 'KrApprovalActionVirtual';

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
  static readonly isAdvisoryKey = 'IsAdvisory';

  /** @category Static Keys */
  static readonly isDisableAutoApprovalKey = 'IsDisableAutoApproval';

  /** @category Static Keys */
  static readonly returnWhenApprovedKey = 'ReturnWhenApproved';

  /** @category Static Keys */
  static readonly expectAllApproversKey = 'ExpectAllApprovers';

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

  //#region fields

  private readonly _additionalApproversDisplay = KrAdditionalApproversSettings.factory(
    KrAdditionalApproversSettings,
    this.action,
    this.actionState,
    []
  );

  @observable.ref
  private _firstIsResponsibleDisplay: boolean;

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
    return this.getBindingArray(KrApprovalActionSettings.krWeRolesVirtualSectionName, x =>
      WorkflowActionRoleOrderedRowSettings.factory(
        WorkflowActionRoleOrderedRowSettings,
        this.action,
        this.actionState,
        x
      )
    );
  }
  @undoredo.array(true)
  set performers(value: StorageArray<WorkflowActionRoleOrderedRowSettings> | string) {
    this.setBindingArray(KrApprovalActionSettings.krWeRolesVirtualSectionName, value);
  }

  get additionalApprovers(): StorageArray<KrAdditionalApproversSettings> | null {
    return this.tryGetArray(
      KrApprovalActionSettings.krApprovalActionAdditionalPerformersVirtualSectionName,
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
      KrApprovalActionSettings.krApprovalActionAdditionalPerformersVirtualSectionName,
      value
    );
  }

  get additionalApproversDisplay(): StorageArray<KrAdditionalApproversSettings> {
    return this._additionalApproversDisplay;
  }

  get firstIsResponsibleDisplay(): boolean {
    return this._firstIsResponsibleDisplay;
  }
  set firstIsResponsibleDisplay(value: boolean) {
    runInAction(() => (this._firstIsResponsibleDisplay = value));
  }

  get author(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.authorKey,
        KrApprovalActionSettings.authorIdKey,
        KrApprovalActionSettings.authorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set author(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krApprovalActionVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.authorKey,
      KrApprovalActionSettings.authorIdKey,
      KrApprovalActionSettings.authorNameKey,
      value
    );
  }

  get digest(): string | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.digestKey) ?? null
    );
  }
  @undoredo()
  set digest(value: string | null) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.digestKey,
      value,
      FieldType.String
    );
  }

  get kind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.kindKey,
        KrApprovalActionSettings.kindIdKey,
        KrApprovalActionSettings.kindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set kind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krApprovalActionVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.kindKey,
      KrApprovalActionSettings.kindIdKey,
      KrApprovalActionSettings.kindCaptionKey,
      value
    );
  }

  get result(): string | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.resultKey) ?? null
    );
  }
  @undoredo()
  set result(value: string | null) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.resultKey,
      value,
      FieldType.String
    );
  }

  get period(): number | string | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.periodKey) ?? null
    );
  }
  @undoredo()
  set period(value: number | string | null) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.periodKey,
      value,
      FieldType.Double
    );
  }

  get planned(): string | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.plannedKey) ?? null
    );
  }
  @undoredo()
  set planned(value: string | null) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.plannedKey,
      value,
      FieldType.DateTime
    );
  }

  get isParallel(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.isParallelKey) ?? false
    );
  }
  @undoredo()
  set isParallel(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.isParallelKey,
      value,
      FieldType.Boolean
    );
  }

  get isAdvisory(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.isAdvisoryKey) ?? false
    );
  }
  @undoredo()
  set isAdvisory(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.isAdvisoryKey,
      value,
      FieldType.Boolean
    );
  }

  get isDisableAutoApproval(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.isDisableAutoApprovalKey) ?? false
    );
  }
  @undoredo()
  set isDisableAutoApproval(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.isDisableAutoApprovalKey,
      value,
      FieldType.Boolean
    );
  }

  get returnWhenApproved(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.returnWhenApprovedKey) ?? false
    );
  }
  @undoredo()
  set returnWhenApproved(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.returnWhenApprovedKey,
      value,
      FieldType.Boolean
    );
  }

  get expectAllApprovers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.expectAllApproversKey) ?? false
    );
  }
  @undoredo()
  set expectAllApprovers(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.expectAllApproversKey,
      value,
      FieldType.Boolean
    );
  }

  get changeStateOnStart(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.changeStateOnStartKey) ?? false
    );
  }
  @undoredo()
  set changeStateOnStart(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.changeStateOnStartKey,
      value,
      FieldType.Boolean
    );
  }

  get changeStateOnEnd(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.changeStateOnEndKey) ?? false
    );
  }
  @undoredo()
  set changeStateOnEnd(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.changeStateOnEndKey,
      value,
      FieldType.Boolean
    );
  }

  get notCreateReturnEditTaskHistoryRecord(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.notCreateReturnEditTaskHistoryRecordKey) ?? false
    );
  }
  @undoredo()
  set notCreateReturnEditTaskHistoryRecord(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.notCreateReturnEditTaskHistoryRecordKey,
      value,
      FieldType.Boolean
    );
  }

  get canEditCard(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.canEditCardKey) ?? false
    );
  }
  @undoredo()
  set canEditCard(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.canEditCardKey,
      value,
      FieldType.Boolean
    );
  }

  get canEditAnyFiles(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.canEditAnyFilesKey) ?? false
    );
  }
  @undoredo()
  set canEditAnyFiles(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.canEditAnyFilesKey,
      value,
      FieldType.Boolean
    );
  }

  get sqlPerformersScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.sqlPerformersScriptKey) ?? null
    );
  }
  @undoredo()
  set sqlPerformersScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName)
      .setBindingField(KrApprovalActionSettings.sqlPerformersScriptKey, value, FieldType.String);
  }

  get initTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.initTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set initTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName)
      .setBindingField(KrApprovalActionSettings.initTaskScriptKey, value, FieldType.String);
  }

  get notification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.notificationKey,
        KrApprovalActionSettings.notificationIdKey,
        KrApprovalActionSettings.notificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set notification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krApprovalActionVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.notificationKey,
      KrApprovalActionSettings.notificationIdKey,
      KrApprovalActionSettings.notificationNameKey,
      value
    );
  }

  get excludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.excludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set excludeDeputies(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.excludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get excludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krApprovalActionVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.excludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set excludeSubscribers(value: boolean | string) {
    this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).setBindingField(
      KrApprovalActionSettings.excludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get notificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.notificationScriptKey) ?? null
    );
  }
  @undoredo()
  set notificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName)
      .setField(KrApprovalActionSettings.notificationScriptKey, value, FieldType.String);
  }

  get editInterjectRole(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.editInterjectRoleKey,
        KrApprovalActionSettings.editInterjectRoleIdKey,
        KrApprovalActionSettings.editInterjectRoleNameKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectRole(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.editInterjectRoleKey,
      KrApprovalActionSettings.editInterjectRoleIdKey,
      KrApprovalActionSettings.editInterjectRoleNameKey,
      value
    );
  }

  get editInterjectAuthor(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.editInterjectAuthorKey,
        KrApprovalActionSettings.editInterjectAuthorIdKey,
        KrApprovalActionSettings.editInterjectAuthorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectAuthor(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.editInterjectAuthorKey,
      KrApprovalActionSettings.editInterjectAuthorIdKey,
      KrApprovalActionSettings.editInterjectAuthorNameKey,
      value
    );
  }

  get editInterjectKind(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.editInterjectKindKey,
        KrApprovalActionSettings.editInterjectKindIdKey,
        KrApprovalActionSettings.editInterjectKindCaptionKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectKind(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.editInterjectKindKey,
      KrApprovalActionSettings.editInterjectKindIdKey,
      KrApprovalActionSettings.editInterjectKindCaptionKey,
      value
    );
  }

  get editInterjectDigest(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.editInterjectDigestKey) ?? null
    );
  }
  @undoredo()
  set editInterjectDigest(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName)
      .setBindingField(KrApprovalActionSettings.editInterjectDigestKey, value, FieldType.String);
  }

  get editInterjectPeriod(): number | string | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.editInterjectPeriodKey) ?? null
    );
  }
  @undoredo()
  set editInterjectPeriod(value: number | string | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(KrApprovalActionSettings.editInterjectPeriodKey, value, FieldType.Double);
  }

  get editInterjectPlanned(): string | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.editInterjectPlannedKey) ?? null
    );
  }
  @undoredo()
  set editInterjectPlanned(value: string | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(KrApprovalActionSettings.editInterjectPlannedKey, value, FieldType.DateTime);
  }

  get editInterjectInitTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.editInterjectInitTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set editInterjectInitTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName)
      .setBindingField(
        KrApprovalActionSettings.editInterjectInitTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get editInterjectNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.editInterjectNotificationKey,
        KrApprovalActionSettings.editInterjectNotificationIdKey,
        KrApprovalActionSettings.editInterjectNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set editInterjectNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.editInterjectNotificationKey,
      KrApprovalActionSettings.editInterjectNotificationIdKey,
      KrApprovalActionSettings.editInterjectNotificationNameKey,
      value
    );
  }

  get editInterjectExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.editInterjectExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set editInterjectExcludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(
      KrApprovalActionSettings.editInterjectExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get editInterjectExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.editInterjectExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set editInterjectExcludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName
    ).setBindingField(
      KrApprovalActionSettings.editInterjectExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get editInterjectNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.editInterjectNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set editInterjectNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeEditInterjectOptionsVirtualSectionName)
      .setField(
        KrApprovalActionSettings.editInterjectNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get additionalApprovalInitTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.additionalApprovalInitTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set additionalApprovalInitTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
      .setBindingField(
        KrApprovalActionSettings.additionalApprovalInitTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get additionalApprovalNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.additionalApprovalNotificationKey,
        KrApprovalActionSettings.additionalApprovalNotificationIdKey,
        KrApprovalActionSettings.additionalApprovalNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set additionalApprovalNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.additionalApprovalNotificationKey,
      KrApprovalActionSettings.additionalApprovalNotificationIdKey,
      KrApprovalActionSettings.additionalApprovalNotificationNameKey,
      value
    );
  }

  get additionalApprovalExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.additionalApprovalExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set additionalApprovalExcludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
    ).setBindingField(
      KrApprovalActionSettings.additionalApprovalExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get additionalApprovalExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.additionalApprovalExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set additionalApprovalExcludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName
    ).setBindingField(
      KrApprovalActionSettings.additionalApprovalExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get additionalApprovalNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.additionalApprovalNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set additionalApprovalNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeAdditionalApprovalOptionsVirtualSectionName)
      .setField(
        KrApprovalActionSettings.additionalApprovalNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get requestCommentInitTaskScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.requestCommentInitTaskScriptKey) ?? null
    );
  }
  @undoredo()
  set requestCommentInitTaskScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName)
      .setBindingField(
        KrApprovalActionSettings.requestCommentInitTaskScriptKey,
        value,
        FieldType.String
      );
  }

  get requestCommentNotification(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName
      )?.tryGetKeyPair(
        KrApprovalActionSettings.requestCommentNotificationKey,
        KrApprovalActionSettings.requestCommentNotificationIdKey,
        KrApprovalActionSettings.requestCommentNotificationNameKey
      ) ?? null
    );
  }
  @undoredo()
  set requestCommentNotification(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName
    ).setBindingKeyPair(
      KrApprovalActionSettings.requestCommentNotificationKey,
      KrApprovalActionSettings.requestCommentNotificationIdKey,
      KrApprovalActionSettings.requestCommentNotificationNameKey,
      value
    );
  }

  get requestCommentExcludeDeputies(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.requestCommentExcludeDeputiesKey) ?? false
    );
  }
  @undoredo()
  set requestCommentExcludeDeputies(value: boolean | string) {
    this.getSubObject(
      KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName
    ).setBindingField(
      KrApprovalActionSettings.requestCommentExcludeDeputiesKey,
      value,
      FieldType.Boolean
    );
  }

  get requestCommentExcludeSubscribers(): boolean | string {
    return (
      this.tryGetSubObject(
        KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName
      )?.tryGetValue(KrApprovalActionSettings.requestCommentExcludeSubscribersKey) ?? false
    );
  }
  @undoredo()
  set requestCommentExcludeSubscribers(value: boolean | string) {
    this.getSubObject(
      KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName
    ).setBindingField(
      KrApprovalActionSettings.requestCommentExcludeSubscribersKey,
      value,
      FieldType.Boolean
    );
  }

  get requestCommentNotificationScript(): string | null {
    return (
      this.getTemplateSettingsOrThis()
        .tryGetSubObject(KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName)
        ?.tryGetValue(KrApprovalActionSettings.requestCommentNotificationScriptKey) ?? null
    );
  }
  @undoredo()
  set requestCommentNotificationScript(value: string | null) {
    this.getTemplateSettingsOrThis()
      .getSubObject(KrApprovalActionSettings.krWeRequestCommentOptionsVirtualSectionName)
      .setField(
        KrApprovalActionSettings.requestCommentNotificationScriptKey,
        value,
        FieldType.String
      );
  }

  get completeOptions(): StorageArray<KrTaskOptionRowSettings> {
    return this.getTemplateSettingsOrThis().getArray(
      KrApprovalActionSettings.krApprovalActionOptionsVirtualSectionName,
      x =>
        KrTaskOptionRowSettings.factory(KrTaskOptionRowSettings, this.action, this.actionState, x)
    );
  }
  @undoredo.array(true)
  set completeOptions(value: StorageArray<KrTaskOptionRowSettings>) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrApprovalActionSettings.krApprovalActionOptionsVirtualSectionName,
      value
    );
  }

  get completeOptionsNotificationsRecipients(): StorageArray<KrActionNotificationRowRolesSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrApprovalActionSettings.krApprovalActionNotificationRolesVirtualSectionName,
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
      KrApprovalActionSettings.krApprovalActionNotificationRolesVirtualSectionName,
      value
    );
  }

  get actionCompleteOptions(): StorageArray<KrActionOptionRowSettings> {
    return this.getTemplateSettingsOrThis().getArray(
      KrApprovalActionSettings.krApprovalActionOptionsActionVirtualSectionName,
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
  set actionCompleteOptions(value: StorageArray<KrActionOptionRowSettings>) {
    this.getTemplateSettingsOrThis().setStorageValue(
      KrApprovalActionSettings.krApprovalActionOptionsActionVirtualSectionName,
      value
    );
  }

  get actionCompleteOptionsLinks(): StorageArray<KrActionOptionLinksSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrApprovalActionSettings.krApprovalActionOptionLinksVirtualSectionName,
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
      KrApprovalActionSettings.krApprovalActionOptionLinksVirtualSectionName,
      value
    );
  }

  get actionCompleteOptionsNotificationsRecipients(): StorageArray<KrActionNotificationRowRolesSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrApprovalActionSettings.krApprovalActionNotificationActionRolesVirtualSectionName,
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
      KrApprovalActionSettings.krApprovalActionNotificationActionRolesVirtualSectionName,
      value
    );
  }

  get events(): StorageArray<WorkflowTaskActionEventRowSettings> | null {
    return this.getTemplateSettingsOrThis().tryGetArray(
      KrApprovalActionSettings.weTaskActionEventsSectionName,
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
      KrApprovalActionSettings.weTaskActionEventsSectionName,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrApprovalActionSettings {
    return new KrApprovalActionSettings(this.action, this.actionState, storage);
  }

  protected override initialize(): void {
    this.init(KrApprovalActionSettings.krWeRolesVirtualSectionName, null);
    this.getTemplateSettingsOrThis().init(
      KrApprovalActionSettings.krApprovalActionOptionsVirtualSectionName,
      null
    );
    this.getTemplateSettingsOrThis().init(
      KrApprovalActionSettings.krApprovalActionOptionsActionVirtualSectionName,
      null
    );
    if (!this.planned) {
      this.getSubObject(KrApprovalActionSettings.krApprovalActionVirtualSectionName).initField(
        KrApprovalActionSettings.periodKey,
        1,
        FieldType.Double
      );
    }
  }

  //#endregion
}
