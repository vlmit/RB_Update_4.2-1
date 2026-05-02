import { reaction } from 'mobx';
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
  WorkflowEditorHelper,
  WorkflowPropertyGridBindingHelper,
  WorkflowActionWithSettingsBase,
  WorkflowActionSettingsEditorOptions,
  AutocompleteKeyValuePairBindingDataConverter,
  AutocompleteRolesDataConverter,
  WorkflowActionRoleOrderedRowSettings
} from 'tessa/ui/workflow/chunk';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { KrSigningActionStorage } from '../models/krSigningActionStorage';
import { sqlApproverRoleId, sqlApproverRoleName } from '../../../../krProcess/krUIHelper';
import { KrSigningActionSettings } from '../models/krSigningActionSettings';
import { KrWorkflowActionsEditorHelper } from './krWorkflowActionsEditorHelper';
import { AutocompleteFileCategoryRowSettingsDataConverter } from './autocompleteFileCategoryRowSettingsDataConverter';
import { WfeLabelProperty } from 'tessa/ui/workflow/chunk/editors/actionPropertyGrid/wfeLabelProperty';
import { localize } from '@tessa/application';

/**
 * Редактор действия "Подписание".
 */
export class KrSigningActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region fields

  protected readonly _autocompleteSigningActionFileCategoryRowSettingsDataConverter: AutocompleteFileCategoryRowSettingsDataConverter;

  //#endregion

  //#region ctor

  constructor(
    action: WorkflowActionWithSettingsBase,
    options: WorkflowActionSettingsEditorOptions
  ) {
    super(action, options);

    this._autocompleteSigningActionFileCategoryRowSettingsDataConverter =
      new AutocompleteFileCategoryRowSettingsDataConverter(
        this.actionSettings.action,
        this.actionSettings.actionState
      );

    this.subscribeCollectionToHistoryChanges(
      this.actionSettings,
      s => s.performers,
      KrSigningActionSettings.krWeRolesVirtualSectionName
    );
  }

  //#endregion

  //#region properties

  protected get actionSettings(): KrSigningActionSettings {
    return <KrSigningActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrSigningActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Performers',
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
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            },
            openInTable: true,
            orderPropertyName: 'order'
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
          addNewRole: () => {
            const newPerformer = new WorkflowActionRoleOrderedRowSettings(
              this._action.action,
              this._action.actionState
            );

            newPerformer.role = { key: sqlApproverRoleId, value: sqlApproverRoleName };

            (this.actionSettings.performers as WorkflowActionRoleOrderedRowSettings[]).push(
              newPerformer
            );
          },
          getRoleId: role => role.role?.key
        }),
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
              KrSigningActionSettings.authorIdKey,
              KrSigningActionSettings.authorNameKey
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
              KrSigningActionSettings.kindIdKey,
              KrSigningActionSettings.kindCaptionKey
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
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrSigningActionStorage>({
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
            caption: '$CardTypes_Columns_Controls_AllowAdditionalApproval'
          },
          { propertyName: 'allowAdditionalApproval', bindingAllowed: true }
        ),
        new WfeGroup('$CardTypes_Blocks_AdditionalSettings', true),
        new WfeBooleanProperty(
          args => {
            return {
              caption: '$CardTypes_Columns_Controls_SignFilesOptions_SignFiles',
              onInitialized: async () => {
                const propertyGrid = args.tryGetPropertyGrid()!;

                const noCommentDialogDialogProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'noCommentDialog'
                  );
                const noSignFilesDialogProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'noSignFilesDialog'
                  );
                const doNotSignFileCopiesProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'doNotSignFileCopies'
                  );
                const signFileCategoriesProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'signFileCategories'
                  );
                const signHiddenFileCategoriesProperty =
                  WorkflowPropertyGridBindingHelper.findSettingsProperty(
                    propertyGrid,
                    'signHiddenFileCategories'
                  );

                this.disposeList.add(
                  reaction(
                    () => !!this.actionSettings.signCardFiles,
                    signCardFiles => {
                      WorkflowEditorHelper.setVisibilityProperty(
                        noCommentDialogDialogProperty,
                        signCardFiles
                      );
                      WorkflowEditorHelper.setVisibilityProperty(
                        noSignFilesDialogProperty,
                        signCardFiles
                      );
                      WorkflowEditorHelper.setVisibilityProperty(
                        doNotSignFileCopiesProperty,
                        signCardFiles
                      );
                      WorkflowEditorHelper.setVisibilityProperty(
                        signFileCategoriesProperty,
                        signCardFiles
                      );
                      WorkflowEditorHelper.setVisibilityProperty(
                        signHiddenFileCategoriesProperty,
                        signCardFiles
                      );
                    }
                  )
                );
              }
            };
          },
          { propertyName: 'signCardFiles', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_SignFilesOptions_NoCommentDialog',
            visibility: !!this.actionSettings.signCardFiles
          },
          { propertyName: 'noCommentDialog', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_SignFilesOptions_NoDialog',
            visibility: !!this.actionSettings.signCardFiles
          },
          { propertyName: 'noSignFilesDialog', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_SignFilesOptions_NoCopies',
            visibility: !!this.actionSettings.signCardFiles
          },
          { propertyName: 'doNotSignFileCopies', bindingAllowed: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Columns_Controls_SignFilesOptions_FileCategories',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'FileCategoriesFiltered',
              refSection: 'FileCategory',
              idColumn: 'CategoryID',
              nameColumn: 'CategoryName',
              parameterAlias: 'Name',
              unique: true,
              multiple: true
            }),
            dataConverter: this._autocompleteSigningActionFileCategoryRowSettingsDataConverter,
            visibility: !!this.actionSettings.signCardFiles,
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'signFileCategories',
            bindingAllowed: true,
            bindingType: {
              isMultiple: true,
              viewAlias: 'FileCategoriesFiltered',
              refSection: 'FileCategory',
              viewReference: 'Category',
              complexPrefix: 'Category',
              idColumn: 'ID',
              nameColumn: 'Name'
            }
          }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Columns_Controls_SignFilesOptions_HiddenCategories',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'FileCategoriesFiltered',
              refSection: 'FileCategory',
              idColumn: 'CategoryID',
              nameColumn: 'CategoryName',
              parameterAlias: 'Name',
              unique: true,
              multiple: true
            }),
            dataConverter: this._autocompleteSigningActionFileCategoryRowSettingsDataConverter,
            visibility: !!this.actionSettings.signCardFiles,
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'signHiddenFileCategories',
            bindingAllowed: true,
            bindingType: {
              isMultiple: true,
              viewAlias: 'FileCategoriesFiltered',
              refSection: 'FileCategory',
              viewReference: 'Category',
              complexPrefix: 'Category',
              idColumn: 'ID',
              nameColumn: 'Name'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Columns_Controls_ReturnAfterSigning'
          },
          { propertyName: 'returnWhenApproved', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExpectAllSigners'
          },
          { propertyName: 'expectAllSigners', bindingAllowed: true }
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
              KrSigningActionSettings.notificationIdKey,
              KrSigningActionSettings.notificationNameKey
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
              KrSigningActionSettings.editInterjectRoleIdKey,
              KrSigningActionSettings.editInterjectRoleNameKey
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
              KrSigningActionSettings.editInterjectAuthorIdKey,
              KrSigningActionSettings.editInterjectAuthorNameKey
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
              KrSigningActionSettings.editInterjectKindIdKey,
              KrSigningActionSettings.editInterjectKindCaptionKey
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
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrSigningActionStorage>({
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
              KrSigningActionSettings.editInterjectNotificationIdKey,
              KrSigningActionSettings.editInterjectNotificationNameKey
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
              KrSigningActionSettings.additionalApprovalNotificationIdKey,
              KrSigningActionSettings.additionalApprovalNotificationNameKey
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
              KrSigningActionSettings.requestCommentNotificationIdKey,
              KrSigningActionSettings.requestCommentNotificationNameKey
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
          KrSigningActionStorage,
          KrSigningActionSettings
        >(
          settings => settings.completeOptionsNotificationsRecipients,
          (settings, value) => (settings.completeOptionsNotificationsRecipients = value)
        ),
        KrWorkflowActionsEditorHelper.createActionCompleteOptionsTableProperty<
          KrSigningActionStorage,
          KrSigningActionSettings
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

  //#endregion
}
