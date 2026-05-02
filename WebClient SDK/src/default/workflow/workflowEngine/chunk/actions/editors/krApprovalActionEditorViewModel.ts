import { reaction, runInAction } from 'mobx';
import { FieldType, Guid, StorageArray, StorageHelper } from '@tessa/core';
import {
  AutocompleteKeyValuePairDataConverter,
  WfeAutocompleteProperty,
  WfeBooleanProperty,
  WfeCodeProperty,
  WfeGroup,
  WfeLayout,
  WfeTextProperty,
  WorkflowActionsEditorHelper,
  WorkflowActionSettingsEditorViewModelBase,
  WorkflowActionSettingsRowStorageBase,
  WorkflowEditorHelper,
  WorkflowHelper,
  WorkflowPropertyGridBindingHelper,
  WorkflowActionWithSettingsBase,
  WorkflowActionSettingsEditorOptions,
  AutocompleteKeyValuePairBindingDataConverter,
  WorkflowActionRoleOrderedRowSettings,
  AutocompleteRolesDataConverter,
  WfeLabelProperty
} from 'tessa/ui/workflow/chunk';
import {
  AutocompleteDataViewContext,
  AutocompleteEventNames,
  AutocompleteMode,
  AutocompleteViewModel
} from 'ui/autocomplete';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { ParametrizedProperty } from 'tessa/ui/workflow';
import { KrApprovalActionStorage } from '../models/krApprovalActionStorage';
import {
  advisoryTaskKindId,
  existsMarkName,
  getKindCaption,
  markName,
  sqlApproverRoleId,
  sqlApproverRoleName,
  unmarkName
} from '../../../../krProcess/krUIHelper';
import { KrWorkflowActionsEditorHelper } from './krWorkflowActionsEditorHelper';
import { KrApprovalActionSettings } from '../models/krApprovalActionSettings';
import { AutocompleteProperty, BooleanProperty, IPropertyGrid } from 'tessa/ui/propertyGrid';
import { AutocompleteAdditionalApproversConverter } from './autocompleteAdditionalApproversConverter';
import { KrAdditionalApproversSettings } from '../models/krAdditionalApproversSettings';
import { IViewRepository } from '@tessa/platform';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { localize } from '@tessa/application';

/**
 * Редактор действия "Согласование".
 */
export class KrApprovalActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region fields

  private static additionalApproversChangedKey = 'additionalApproversChanged';

  //#endregion

  //#region ctor

  constructor(
    action: WorkflowActionWithSettingsBase,
    options: WorkflowActionSettingsEditorOptions,
    protected readonly _viewRepository: IViewRepository
  ) {
    super(action, options);

    this.subscribeCollectionToHistoryChanges(
      this.actionSettings,
      s => s.performers,
      KrApprovalActionSettings.krWeRolesVirtualSectionName
    );

    this.subscribeCollectionToHistoryChanges(
      this.actionSettings,
      s => s.additionalApprovers,
      KrApprovalActionSettings.krApprovalActionAdditionalPerformersVirtualSectionName,
      (args, item) => {
        if (
          args.fieldName === KrAdditionalApproversSettings.isResponsibleKey &&
          item.mainApproverRowId &&
          item.order === 0 &&
          this.actionSettings.additionalApproversDisplay.length > 0 &&
          this.actionSettings.additionalApproversDisplay[0].rowId === item.rowId
        ) {
          this.actionSettings.firstIsResponsibleDisplay =
            this.actionSettings.additionalApproversDisplay[0].isResponsible;
        }
      }
    );
  }

  //#endregion

  //#region properties

  protected get actionSettings(): KrApprovalActionSettings {
    return <KrApprovalActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrApprovalActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
        new WfeAutocompleteProperty(
          args => {
            return {
              caption: '$CardTypes_Controls_Approvers',
              dataContext: new AutocompleteDataViewContext({
                viewAlias: 'Roles',
                idColumn: 'RoleID',
                nameColumn: 'RoleName',
                parameterAlias: 'Name',
                unique: true,
                multiple: true
              }),
              dataConverter: new AutocompleteRolesDataConverter(
                WorkflowActionRoleOrderedRowSettings,
                this.actionSettings.action,
                this.actionSettings.actionState
              ),
              orderPropertyName: 'order',
              openInTable: true,
              onInitialized: async property => {
                property.control.mode = AutocompleteMode.NonDroppable;
                property.control.maxRows = 15;

                // Отключение сброса выделения после закрытия меню.
                property.control.menu.onClosed.removeByName(AutocompleteEventNames.onMenuClosed);

                const propertyGrid = args.tryGetPropertyGrid()!;

                // Скрытие/отображение свойств при выборе/сбросе выделения с согласующего.
                const additionalApproversDisplayProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'additionalApproversDisplay'
                  );
                const firstIsResponsibleDisplayProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'firstIsResponsibleDisplay'
                  );

                this.disposeList.add(
                  reaction(
                    () => !!property.control.selectedRecord,
                    isSelected => {
                      WorkflowEditorHelper.setVisibilityProperty(
                        additionalApproversDisplayProperty,
                        isSelected
                      );

                      WorkflowEditorHelper.setVisibilityProperty(
                        firstIsResponsibleDisplayProperty,
                        isSelected
                      );
                    }
                  )
                );

                // Перенос данных из additionalApprovers в additionalApproversDisplay и additionalApproversDisplay в соответствии с выбранным основным согласующим (e.record).
                const firstIsResponsibleDisplayPropertyControl = (
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'firstIsResponsibleDisplay'
                  ) as BooleanProperty
                ).control;

                property.disposeList.add(
                  property.control.onSelectRecord.add(() => {
                    this.updateAdditionalApproversDisplay(
                      property.control,
                      firstIsResponsibleDisplayPropertyControl
                    );
                  })
                );

                this.subscribeAdditionalApproversChanges(
                  property,
                  firstIsResponsibleDisplayPropertyControl
                );

                // Удаление доп. исполнителей из места постоянного хранения (additionalApprovers) при удалении основного согласующего.
                const performers = this.actionSettings.performers;
                this.subscribePerformers(
                  performers as StorageArray<WorkflowActionRoleOrderedRowSettings>
                );

                this.disposeList.add(
                  this.actionSettings.cachedMemberChanged.add(e => {
                    if (e.fieldName === KrApprovalActionSettings.krWeRolesVirtualSectionName) {
                      this.unsubscribePrformers(
                        e.oldValue as StorageArray<WorkflowActionRoleOrderedRowSettings>
                      );
                      this.subscribePerformers(
                        e.newValue as StorageArray<WorkflowActionRoleOrderedRowSettings>
                      );
                    }
                  })
                );
              }
            };
          },
          {
            propertyName: 'performers',
            bindingAllowed: true,
            bindingType: {
              isMultiple: true,
              name: 'Role'
            }
          }
        ),
        KrWorkflowActionsEditorHelper.createAddRoleButton({
          disposeList: this.disposeList,
          getRolesOrBinding: () => this.actionSettings.performers,
          addNewRole: args => {
            const newPerformer = new WorkflowActionRoleOrderedRowSettings(
              this._action.action,
              this._action.actionState
            );

            newPerformer.role = { key: sqlApproverRoleId, value: sqlApproverRoleName };

            // Добавление нового элемента через AutocompleteProperty для вызова события onAddedRecord.
            const performersPropertyControl = (
              WorkflowPropertyGridBindingHelper.findSettingsProperty(
                args.tryGetPropertyGrid()!,
                'performers'
              ) as ParametrizedProperty
            ).innerProperty as AutocompleteProperty;

            performersPropertyControl.control.addRecord({
              record: performersPropertyControl.dataSource.converter.toRecord(newPerformer)
            });
          },
          getRoleId: role => role.role?.key
        }),
        new WfeAutocompleteProperty(
          args => {
            return {
              caption: '$CardTypes_Controls_PerformersForCurrentApprover',
              dataContext: new AutocompleteDataViewContext({
                viewAlias: 'Roles',
                idColumn: 'RoleID',
                nameColumn: 'RoleName',
                parameterAlias: 'Name',
                unique: true,
                multiple: true
              }),
              dataConverter: new AutocompleteAdditionalApproversConverter(
                () =>
                  this.getMainApproverRowId(
                    this.getPerformersProperty(args.tryGetPropertyGrid()!).control
                  ),
                this.actionSettings.action,
                this.actionSettings.actionState
              ),
              orderPropertyName: 'order',
              openInTable: true,
              visibility: false,
              onInitialized: async property => {
                property.control.menu.openAction.isCollapsed = true;
                property.control.mode = AutocompleteMode.NonDroppable;
                property.control.maxRows = 15;

                const propertyGrid = args.tryGetPropertyGrid()!;
                const firstIsResponsibleDisplayPropertyControl = (
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'firstIsResponsibleDisplay'
                  )! as BooleanProperty
                ).control;
                const performersPropertyControl = this.getPerformersProperty(propertyGrid).control;

                // Добавление новой записи в постоянное место хранения (additionalApprovers) при добавлении новой записи в additionalApproversDisplay.
                property.disposeList.add(
                  property.control.onAddedRecord.add(() => {
                    this.actionSettings.additionalApprovers ??=
                      KrAdditionalApproversSettings.factory(
                        KrAdditionalApproversSettings,
                        this.actionSettings.action,
                        this.actionSettings.actionState
                      );

                    this.actionSettings.additionalApprovers.push(
                      this.actionSettings.additionalApproversDisplay[
                        this.actionSettings.additionalApproversDisplay.length - 1
                      ]
                    );

                    const mainApproverRowId = this.getMainApproverRowId(performersPropertyControl);

                    if (mainApproverRowId) {
                      this.updateMarkToSelectedPerformer(mainApproverRowId);

                      if (this.getAdditionalApprovers(mainApproverRowId).length === 0) {
                        firstIsResponsibleDisplayPropertyControl.disabled = false;
                      }
                    }
                  })
                );

                // Удаление доп. согласующего из постоянного места хранения (additionalApprovers) при удалении записи из additionalApproversDisplay.
                property.control.onRemovedRecord.add(e => {
                  const additionalApprovers = this.actionSettings.additionalApprovers;

                  if (!additionalApprovers || additionalApprovers.length === 0) {
                    return;
                  }

                  const rowId = StorageHelper.tryGetValue(
                    e.record.data,
                    WorkflowActionSettingsRowStorageBase.rowIdKey,
                    FieldType.Guid
                  );

                  if (!rowId) {
                    return;
                  }

                  const mainApproverRowId = this.getMainApproverRowId(performersPropertyControl);

                  if (!mainApproverRowId) {
                    return;
                  }

                  runInAction(() => {
                    const firstAdditionalApprover = additionalApprovers[0];
                    const removedIndex = WorkflowHelper.removeFirstById(
                      additionalApprovers,
                      rowId,
                      i => i.rowId
                    );

                    // Если после удаления есть доп. согласующие, то необходимо перенести флаг "Первый исполнитель - ответственный" на следующего, иначе его надо снять.
                    if (removedIndex === 0) {
                      this.setFirstIsResponsible(
                        mainApproverRowId,
                        firstAdditionalApprover.isResponsible,
                        false
                      );
                    }

                    if (this.getAdditionalApprovers(mainApproverRowId).length === 0) {
                      this.actionSettings.firstIsResponsibleDisplay = false;

                      firstIsResponsibleDisplayPropertyControl.disabled = true;
                    }
                  });

                  this.updateMarkToSelectedPerformer(mainApproverRowId);
                });
              }
            };
          },
          { propertyName: 'additionalApproversDisplay' }
        ),
        new WfeBooleanProperty(
          args => {
            return {
              caption: '$CardTypes_Controls_FirstIsResponsible',
              tooltip: '$CardTypes_Controls_FirstIsResponsible_Tooltip',
              visibility: false,
              onInitialized: async () => {
                const propertyGrid = args.tryGetPropertyGrid()!;
                const performersPropertyControl = this.getPerformersProperty(propertyGrid).control;

                // Перенос значения флага firstIsResponsibleDisplay в соответствующие записи additionalApprovers и additionalApproversDisplay.
                this.disposeList.add(
                  reaction(
                    () => this.actionSettings.firstIsResponsibleDisplay,
                    firstIsResponsible => {
                      if (
                        !this.actionSettings.additionalApprovers ||
                        this.actionSettings.additionalApprovers.length === 0 ||
                        this.actionSettings.additionalApproversDisplay.length === 0
                      ) {
                        return;
                      }

                      const mainApproverRowId =
                        this.getMainApproverRowId(performersPropertyControl);

                      if (!mainApproverRowId) {
                        return;
                      }

                      this.setFirstIsResponsible(mainApproverRowId, firstIsResponsible, false);
                    }
                  )
                );
              }
            };
          },
          { propertyName: 'firstIsResponsibleDisplay' }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_AuthorUserOrContextRole',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.authorIdKey,
              KrApprovalActionSettings.authorNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'author',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              name: 'Role'
            }
          }
        ),
        new WfeTextProperty(
          {
            caption: '$CardTypes_Controls_TaskDescription',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
            }
          },
          { propertyName: 'digest', bindingAllowed: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Kind',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'TaskKinds',
              idColumn: 'KindID',
              nameColumn: 'KindCaption',
              parameterAlias: 'Caption',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.kindIdKey,
              KrApprovalActionSettings.kindCaptionKey
            ),
            onInitialized: async property => {
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'kind',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'TaskKinds',
              viewReference: 'Kind',
              idColumn: 'ID',
              nameColumn: 'Caption'
            }
          }
        ),
        new WfeTextProperty(
          {
            caption: '$CardTypes_Controls_Result',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
            }
          },
          { propertyName: 'result', bindingAllowed: true }
        ),
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrApprovalActionStorage>({
          disposeList: this.disposeList,
          getPeriod: () => this.actionSettings.period,
          resetPeriod: () => (this.actionSettings.period = null),
          getPlanned: () => this.actionSettings.planned,
          resetPlanned: () => (this.actionSettings.planned = null)
        }),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_ApprovalAction_IsParallel'
          },
          { propertyName: 'isParallel', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$UI_KrApproval_Advisory',
            tooltip: '$UI_KrApproval_AdvisoryTooltip',
            onInitialized: async () => {
              this.disposeList.add(
                reaction(
                  () => this.actionSettings.isAdvisory,
                  async isAdvisory => {
                    if (isAdvisory) {
                      if (!Guid.equals(this.actionSettings.kind?.key, advisoryTaskKindId)) {
                        const kindCaption = await getKindCaption(
                          this._viewRepository,
                          advisoryTaskKindId
                        );

                        if (kindCaption) {
                          this.actionSettings.kind = {
                            key: advisoryTaskKindId,
                            value: kindCaption
                          };
                        }
                      }
                    } else {
                      this.actionSettings.kind = null;
                    }
                  }
                )
              );
            }
          },
          { propertyName: 'isAdvisory', bindingAllowed: true }
        ),
        new WfeGroup('$CardTypes_Blocks_AdditionalSettings', true),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_DisableAutoApproval'
          },
          { propertyName: 'isDisableAutoApproval', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_ReturnAfterApproval'
          },
          { propertyName: 'returnWhenApproved', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExpectAllApprovers'
          },
          { propertyName: 'expectAllApprovers', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$UI_KrApproval_ChangeStateOnStart'
          },
          { propertyName: 'changeStateOnStart', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$UI_KrApproval_ChangeStateOnEnd'
          },
          { propertyName: 'changeStateOnEnd', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_NotCreateReturnEditTaskHistoryRecord'
          },
          { propertyName: 'notCreateReturnEditTaskHistoryRecord', bindingAllowed: true }
        ),
        new WfeLabelProperty(
          {
            onInitialized: async property => {
              property.control.text = localize(
                '$CardTypes_Controls_ApprovalAction_Disclaimer_Tooltip'
              );
            }
          },
          {
            propertyName: 'label'
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_EditCard'
          },
          { propertyName: 'canEditCard', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_EditAnyFiles',
            tooltip: '$CardTypes_Columns_Controls_CanEditAnyFiles_ToolTip'
          },
          { propertyName: 'canEditAnyFiles', bindingAllowed: true }
        ),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_SQLPerformers',
            highlightingMode: SyntaxHighlighting.Sql,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'sqlPerformersScript', designModeOnly: true }
        ),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_TaskInitializationScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'initTaskScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_TaskNotification', true),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Notification',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Notifications',
              refSection: 'Notifications',
              idColumn: 'NotificationID',
              nameColumn: 'NotificationName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.notificationIdKey,
              KrApprovalActionSettings.notificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'notification',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'Notifications',
              viewReference: 'Notification',
              idColumn: 'ID',
              nameColumn: 'Name',
              refSection: 'Notifications'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeDeputies'
          },
          { propertyName: 'excludeDeputies', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeSubscribers'
          },
          { propertyName: 'excludeSubscribers', bindingAllowed: true }
        ),
        new WfeCodeProperty(
          args => {
            args.action;
            return {
              caption: '$CardTypes_Controls_EmailModifyScenario',
              highlightingMode: SyntaxHighlighting.CSharp,
              showLineNumbers: true,
              onInitialized: async property => {
                property.control.minRows = 3;
                property.control.maxRows = 15;
              }
            };
          },
          { propertyName: 'notificationScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_RevisionAuthor', true),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Role',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.editInterjectRoleIdKey,
              KrApprovalActionSettings.editInterjectRoleNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'editInterjectRole',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              name: 'Role'
            }
          }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_AuthorUserOrContextRole',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.editInterjectAuthorIdKey,
              KrApprovalActionSettings.editInterjectAuthorNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'editInterjectAuthor',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              name: 'Role'
            }
          }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Kind',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'TaskKinds',
              idColumn: 'KindID',
              nameColumn: 'KindCaption',
              parameterAlias: 'Caption',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.editInterjectKindIdKey,
              KrApprovalActionSettings.editInterjectKindCaptionKey
            ),
            onInitialized: async property => {
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'editInterjectKind',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'TaskKinds',
              viewReference: 'Kind',
              idColumn: 'ID',
              nameColumn: 'Caption'
            }
          }
        ),
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrApprovalActionStorage>({
          disposeList: this.disposeList,
          periodPropertyName: 'editInterjectPeriod',
          getPeriod: () => this.actionSettings.editInterjectPeriod,
          resetPeriod: () => (this.actionSettings.editInterjectPeriod = null),
          plannedPropertyName: 'editInterjectPlanned',
          getPlanned: () => this.actionSettings.editInterjectPlanned,
          resetPlanned: () => (this.actionSettings.editInterjectPlanned = null)
        }),
        new WfeTextProperty(
          {
            caption: '$CardTypes_Controls_TaskDescription',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
            }
          },
          { propertyName: 'editInterjectDigest', bindingAllowed: true }
        ),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_TaskInitializationScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'editInterjectInitTaskScript', designModeOnly: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Notification',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Notifications',
              refSection: 'Notifications',
              idColumn: 'NotificationID',
              nameColumn: 'NotificationName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.editInterjectNotificationIdKey,
              KrApprovalActionSettings.editInterjectNotificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'editInterjectNotification',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'Notifications',
              viewReference: 'Notification',
              idColumn: 'ID',
              nameColumn: 'Name',
              refSection: 'Notifications'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeDeputies'
          },
          { propertyName: 'editInterjectExcludeDeputies', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeSubscribers'
          },
          { propertyName: 'editInterjectExcludeSubscribers', bindingAllowed: true }
        ),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_EmailModifyScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'editInterjectNotificationScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_Controls_AdditionalApproval', true),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_TaskInitializationScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'additionalApprovalInitTaskScript', designModeOnly: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Notification',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Notifications',
              refSection: 'Notifications',
              idColumn: 'NotificationID',
              nameColumn: 'NotificationName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.additionalApprovalNotificationIdKey,
              KrApprovalActionSettings.additionalApprovalNotificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'additionalApprovalNotification',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'Notifications',
              viewReference: 'Notification',
              idColumn: 'ID',
              nameColumn: 'Name',
              refSection: 'Notifications'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeDeputies'
          },
          { propertyName: 'additionalApprovalExcludeDeputies', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeSubscribers'
          },
          { propertyName: 'additionalApprovalExcludeSubscribers', bindingAllowed: true }
        ),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_EmailModifyScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'additionalApprovalNotificationScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_Controls_RequestComment', true),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_TaskInitializationScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'requestCommentInitTaskScript', designModeOnly: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Notification',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Notifications',
              refSection: 'Notifications',
              idColumn: 'NotificationID',
              nameColumn: 'NotificationName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrApprovalActionSettings.requestCommentNotificationIdKey,
              KrApprovalActionSettings.requestCommentNotificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'requestCommentNotification',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'Notifications',
              viewReference: 'Notification',
              idColumn: 'ID',
              nameColumn: 'Name',
              refSection: 'Notifications'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeDeputies'
          },
          { propertyName: 'requestCommentExcludeDeputies', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeSubscribers'
          },
          { propertyName: 'requestCommentExcludeSubscribers', bindingAllowed: true }
        ),
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_EmailModifyScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'requestCommentNotificationScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_ProcessingSettings'),
        KrWorkflowActionsEditorHelper.createCompleteOptionsTableProperty<
          KrApprovalActionStorage,
          KrApprovalActionSettings
        >(
          settings => settings.completeOptionsNotificationsRecipients,
          (settings, value) => (settings.completeOptionsNotificationsRecipients = value)
        ),
        KrWorkflowActionsEditorHelper.createActionCompleteOptionsTableProperty<
          KrApprovalActionStorage,
          KrApprovalActionSettings
        >(
          settings => settings.actionCompleteOptionsNotificationsRecipients,
          (settings, value) => (settings.actionCompleteOptionsNotificationsRecipients = value),
          settings => settings.actionCompleteOptionsLinks,
          (settings, value) => (settings.actionCompleteOptionsLinks = value)
        ),
        WorkflowActionsEditorHelper.createEventsTableProperty(
          new AutocompleteKeyValuePairDataConverter()
        )
      ]
    };
  }

  protected override disposeCore(): void {
    super.disposeCore();

    if (this.actionSettings.additionalApprovers) {
      this.actionSettings.additionalApprovers.collectionChanged.removeByName(
        KrApprovalActionEditorViewModel.additionalApproversChangedKey
      );
    }
  }

  //#endregion

  //#region methods

  /**
   * Возвращает свойство "Согласующие".
   * @param propertyGrid {@link IPropertyGrid}
   * @returns Свойство "Согласующие".
   */
  protected getPerformersProperty(propertyGrid: IPropertyGrid): AutocompleteProperty {
    return (
      WorkflowPropertyGridBindingHelper.findSettingsProperty(
        propertyGrid,
        'performers'
      ) as ParametrizedProperty
    ).innerProperty as AutocompleteProperty;
  }

  /**
   * Возвращает идентификатор записи с информацией о выбранном основном согласующим.
   * @param performersPropertyControl Контрол свойства "Согласующие".
   * @returns Идентификатор записи с информацией о выбранном основном согласующим.
   */
  protected getMainApproverRowId(performersPropertyControl: AutocompleteViewModel): string | null {
    return (
      StorageHelper.tryGetValue(
        performersPropertyControl.selectedRecord?.model.data,
        WorkflowActionSettingsRowStorageBase.rowIdKey,
        FieldType.Guid
      ) ?? null
    );
  }

  /**
   * Обновляет при необходимости метку о наличии доп. согласующих.
   * @param mainApproverRowId Идентификатор основного согласующего.
   */
  protected updateMarkToSelectedPerformer(mainApproverRowId: string): void {
    if (!Array.isArray(this.actionSettings.performers)) {
      return;
    }

    const mainApproverIndex = this.actionSettings.performers.findIndex(i =>
      Guid.equals(i.rowId, mainApproverRowId)
    );

    const mainApprover = this.actionSettings.performers[mainApproverIndex];
    const mainApproverRoleName = mainApprover.role!.value;
    const setMark = this.actionSettings.additionalApproversDisplay.length > 0;

    if (existsMarkName(mainApproverRoleName) !== setMark) {
      mainApprover.role = {
        key: mainApprover.role!.key,
        value: setMark ? markName(mainApproverRoleName)! : unmarkName(mainApproverRoleName)!
      };
    }
  }

  /**
   * Возвращает список доп. согласующих отфильтрованный по основному согласующему.
   * @param mainApproverRowId Идентификатор основного согласующего.
   * @returns Список доп. согласующих.
   */
  protected getAdditionalApprovers(mainApproverRowId: string): KrAdditionalApproversSettings[] {
    return (
      this.actionSettings.additionalApprovers
        ?.filter(i => Guid.equals(i.mainApproverRowId, mainApproverRowId))
        .sort((a, b) => a.order - b.order) ?? []
    );
  }

  /**
   * Устанавливает значение флага "Первый исполнитель - ответственный" для первого доп. согласующего.
   * @param mainApproverRowId Идентификатор основного согласующего.
   * @param value Значение флага.
   * @param reorder Определяет, что выполняется пересортировка элементов. При пересортировке стоит отключать флаг для предыдущих значений.
   */
  protected setFirstIsResponsible(
    mainApproverRowId: string,
    value: boolean,
    reorder: boolean
  ): void {
    const additionalApprovers = this.getAdditionalApprovers(mainApproverRowId);

    if (additionalApprovers.length === 0) {
      return;
    }

    if (additionalApprovers[0].isResponsible !== value) {
      additionalApprovers[0].isResponsible = value;
    }

    if (this.actionSettings.firstIsResponsibleDisplay !== value) {
      this.actionSettings.firstIsResponsibleDisplay = value;
    }

    if (reorder) {
      for (let i = 1; i < additionalApprovers.length; i++) {
        const additionalApprover = additionalApprovers[i];

        if (additionalApprover.isResponsible) {
          additionalApprover.isResponsible = false;
        }
      }
    }
  }

  private subscribePerformers(
    performers: StorageArray<WorkflowActionRoleOrderedRowSettings>
  ): void {
    if (!Array.isArray(performers)) {
      return;
    }

    performers.collectionChanged.add(
      e => {
        if (e.removed.length > 0) {
          for (const removed of e.removed) {
            const mainApproverRowId = removed.rowId;

            runInAction(() =>
              WorkflowHelper.removeAllById(
                this.actionSettings.additionalApprovers,
                mainApproverRowId,
                i => i.mainApproverRowId
              )
            );
          }
        }
      },
      { name: 'updateAdditionalApprovers' }
    );
  }

  private unsubscribePrformers(
    performers: StorageArray<WorkflowActionRoleOrderedRowSettings>
  ): void {
    if (!Array.isArray(performers)) {
      return;
    }

    performers.collectionChanged.removeByName('updateAdditionalApprovers');
  }

  private updateAdditionalApproversDisplay(
    performersControl: AutocompleteViewModel,
    firstIsResponsibleDisplayPropertyControl: CheckboxViewModel
  ) {
    if (!performersControl.selectedRecord) {
      this.clearAdditionalAproversDisplay();
      return;
    }

    const mainApproverRowId = StorageHelper.tryGetValue(
      performersControl.selectedRecord.model.data,
      WorkflowActionSettingsRowStorageBase.rowIdKey,
      FieldType.Guid
    );

    if (!mainApproverRowId) {
      this.clearAdditionalAproversDisplay();
      return;
    }

    const newAdditionalApprovers = this.getAdditionalApprovers(mainApproverRowId);

    if (
      newAdditionalApprovers.length === this.actionSettings.additionalApproversDisplay.length &&
      !newAdditionalApprovers.find(
        (x, i) => !Guid.equals(x.rowId, this.actionSettings.additionalApproversDisplay[i].rowId)
      )
    ) {
      if (newAdditionalApprovers.length > 0) {
        this.actionSettings.firstIsResponsibleDisplay = newAdditionalApprovers[0].isResponsible;
      }
      return;
    }

    this.clearAdditionalAproversDisplay();

    if (newAdditionalApprovers.length > 0) {
      runInAction(() => {
        this.actionSettings.additionalApproversDisplay.push(...newAdditionalApprovers);
      });

      this.actionSettings.firstIsResponsibleDisplay = newAdditionalApprovers[0].isResponsible;

      firstIsResponsibleDisplayPropertyControl.disabled = false;
    } else {
      firstIsResponsibleDisplayPropertyControl.disabled = true;
    }
  }

  private clearAdditionalAproversDisplay(): void {
    runInAction(() => {
      this.actionSettings.additionalApproversDisplay.length = 0;
      this.actionSettings.firstIsResponsibleDisplay = false;
    });
  }

  private subscribeAdditionalApproversChanges(
    property: AutocompleteProperty,
    firstIsResponsibleDisplayPropertyControl: CheckboxViewModel
  ): void {
    property.disposeList.add(
      this.actionSettings.cachedMemberChanged.add(e => {
        if (
          e.fieldName ===
          KrApprovalActionSettings.krApprovalActionAdditionalPerformersVirtualSectionName
        ) {
          if (e.oldValue) {
            (e.oldValue as StorageArray).collectionChanged.removeByName(
              KrApprovalActionEditorViewModel.additionalApproversChangedKey
            );
          }

          if (e.newValue) {
            (e.newValue as StorageArray).collectionChanged.add(
              () => {
                this.updateAdditionalApproversDisplay(
                  property.control,
                  firstIsResponsibleDisplayPropertyControl
                );
              },
              { name: KrApprovalActionEditorViewModel.additionalApproversChangedKey }
            );
          }
        }
      })
    );

    if (this.actionSettings.additionalApprovers) {
      this.actionSettings.additionalApprovers.collectionChanged.add(
        () => {
          this.updateAdditionalApproversDisplay(
            property.control,
            firstIsResponsibleDisplayPropertyControl
          );
        },
        { name: KrApprovalActionEditorViewModel.additionalApproversChangedKey }
      );
    }

    this.disposeList.add(
      this.actionSettings.additionalApproversDisplay.collectionChanged.add(e => {
        if (
          e.added.length === 0 &&
          e.removed.length === 0 &&
          this.actionSettings.additionalApproversDisplay.length > 0
        ) {
          this.setFirstIsResponsible(
            this.actionSettings.additionalApproversDisplay[0].mainApproverRowId!,
            this.actionSettings.additionalApproversDisplay[0].isResponsible ||
              this.actionSettings.firstIsResponsibleDisplay,
            true
          );
        }
      })
    );
  }

  //#endregion
}
