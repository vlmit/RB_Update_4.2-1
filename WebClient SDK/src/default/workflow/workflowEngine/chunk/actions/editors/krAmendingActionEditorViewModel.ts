import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { KrAmendingActionSettings } from '../models/krAmendingActionSettings';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { KrAmendingActionStorage } from '../models/krAmendingActionStorage';
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
  AutocompleteKeyValuePairBindingDataConverter,
  AutocompleteRolesDataConverter,
  WorkflowActionRoleRowSettings
} from 'tessa/ui/workflow/chunk';

/**
 * Редактор действия "Доработка".
 */
export class KrAmendingActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region properties

  protected get actionSettings(): KrAmendingActionSettings {
    return <KrAmendingActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrAmendingActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
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
              KrAmendingActionSettings.roleIdKey,
              KrAmendingActionSettings.roleNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'role',
            bindingAllowed: true,
            designModeOnly: true,
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
              KrAmendingActionSettings.authorIdKey,
              KrAmendingActionSettings.authorNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'author',
            designModeOnly: true,
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
              KrAmendingActionSettings.kindIdKey,
              KrAmendingActionSettings.kindCaptionKey
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
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrAmendingActionStorage>({
          disposeList: this.disposeList,
          getPeriod: () => this.actionSettings.period,
          resetPeriod: () => (this.actionSettings.period = null),
          getPlanned: () => this.actionSettings.planned,
          resetPlanned: () => (this.actionSettings.planned = null)
        }),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_AmendingAction_IncrementCycle'
          },
          { propertyName: 'isIncrementCycle', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_AmendingAction_ChangeState'
          },
          { propertyName: 'isChangeState', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_AmendingAction_EditApprovalSchemeAccess'
          },
          { propertyName: 'hasEditApprovalSchemeAccess', bindingAllowed: true }
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
        new WfeCodeProperty(
          {
            caption: '$CardTypes_Controls_TaskCompletionScenario',
            highlightingMode: SyntaxHighlighting.CSharp,
            showLineNumbers: true,
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          { propertyName: 'completeOptionTaskScript', designModeOnly: true }
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
              KrAmendingActionSettings.notificationIdKey,
              KrAmendingActionSettings.notificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'notification',
            bindingAllowed: true,
            designModeOnly: true,
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
          { propertyName: 'excludeDeputies', bindingAllowed: true, designModeOnly: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeSubscribers'
          },
          { propertyName: 'excludeSubscribers', bindingAllowed: true, designModeOnly: true }
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
          { propertyName: 'notificationScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_TaskCompletionNotification', true),
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
              KrAmendingActionSettings.completeOptionNotificationIdKey,
              KrAmendingActionSettings.completeOptionNotificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'completeOptionNotification',
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
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Recipients',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true,
              multiple: true
            }),
            dataConverter: new AutocompleteRolesDataConverter(
              WorkflowActionRoleRowSettings,
              this.actionSettings.action,
              this.actionSettings.actionState
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'recipients',
            bindingAllowed: true,
            bindingType: {
              isMultiple: true,
              name: 'Role'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_SendToPerformer'
          },
          { propertyName: 'completeOptionSendToPerformer', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_SendToAuthor'
          },
          { propertyName: 'completeOptionSendToAuthor', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeDeputies'
          },
          { propertyName: 'completeOptionExcludeDeputies', bindingAllowed: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeSubscribers'
          },
          { propertyName: 'completeOptionExcludeSubscribers', bindingAllowed: true }
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
          { propertyName: 'completeOptionNotificationScript', designModeOnly: true }
        ),
        new WfeGroup('$CardTypes_Blocks_ProcessingSettings', true),
        WorkflowActionsEditorHelper.createEventsTableProperty(
          new AutocompleteKeyValuePairDataConverter()
        )
      ]
    };
  }

  //#endregion
}
