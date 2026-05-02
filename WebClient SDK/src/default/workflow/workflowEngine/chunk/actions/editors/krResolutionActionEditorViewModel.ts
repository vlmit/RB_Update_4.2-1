import { reaction } from 'mobx';
import {
  WfeAutocompleteProperty,
  WfeBooleanProperty,
  WfeCodeProperty,
  WfeGroup,
  WfeLayout,
  WfeTextProperty,
  WorkflowActionsEditorHelper,
  WorkflowActionSettingsEditorViewModelBase,
  WorkflowHelper,
  WorkflowPropertyGridBindingHelper,
  WorkflowActionRoleRowSettings,
  AutocompleteKeyValuePairBindingDataConverter,
  AutocompleteRolesDataConverter
} from 'tessa/ui/workflow/chunk';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { KrWorkflowActionsEditorHelper } from './krWorkflowActionsEditorHelper';
import { KrResolutionActionSettings } from '../models/krResolutionActionSettings';
import { KrResolutionActionStorage } from '../models/krResolutionActionStorage';
import { sqlApproverRoleId, sqlApproverRoleName } from '../../../../krProcess/krUIHelper';

/**
 * Редактор действия "Типовая задача".
 */
export class KrResolutionActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region properties

  protected get actionSettings(): KrResolutionActionSettings {
    return <KrResolutionActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrResolutionActionStorage> {
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
            propertyName: 'performers',
            designModeOnly: true,
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
            const newPerformer = new WorkflowActionRoleRowSettings(
              this._action.action,
              this._action.actionState
            );

            newPerformer.role = { key: sqlApproverRoleId, value: sqlApproverRoleName };

            (this.actionSettings.performers as WorkflowActionRoleRowSettings[]).push(newPerformer);
          },
          getRoleId: role => role.role?.key
        }),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_SendMassCreation'
          },
          { propertyName: 'isMassCreation', bindingAllowed: true, designModeOnly: true }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_MajorPerformer'
          },
          { propertyName: 'isMajorPerformer', bindingAllowed: true, designModeOnly: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_FromUserOrContextRole',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrResolutionActionSettings.authorIdKey,
              KrResolutionActionSettings.authorNameKey
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
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_SenderUserOrContextRole',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrResolutionActionSettings.senderIdKey,
              KrResolutionActionSettings.senderNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'sender',
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
          { propertyName: 'digest', bindingAllowed: true, designModeOnly: true }
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
              KrResolutionActionSettings.kindIdKey,
              KrResolutionActionSettings.kindNameKey
            ),
            onInitialized: async property => {
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'kind',
            designModeOnly: true,
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
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrResolutionActionStorage>({
          disposeList: this.disposeList,
          getPeriod: () => this.actionSettings.period,
          resetPeriod: () => (this.actionSettings.period = null),
          getPlanned: () => this.actionSettings.planned,
          resetPlanned: () => (this.actionSettings.planned = null)
        }),
        new WfeBooleanProperty(
          args => {
            return {
              caption: '$CardTypes_Controls_WithControl',
              tooltip: '$CardTypes_Controls_WithControl_Tooltip',
              onInitialized: async () => {
                const controllerProperty = WorkflowPropertyGridBindingHelper.findSettingsProperty(
                  args.tryGetPropertyGrid()!,
                  'controller'
                )!;

                const updateControllerPropertyVisibilityAction = () => {
                  if (this.actionSettings.withControl) {
                    controllerProperty.visibility = true;
                  } else {
                    controllerProperty.visibility = false;
                    WorkflowHelper.setValueIfDifferent(
                      null,
                      () => this.actionSettings.controller,
                      value => (this.actionSettings.controller = value)
                    );
                  }
                };

                updateControllerPropertyVisibilityAction();

                this.disposeList.add(
                  reaction(
                    () => this.actionSettings.withControl,
                    updateControllerPropertyVisibilityAction
                  )
                );
              }
            };
          },
          { propertyName: 'withControl', bindingAllowed: true, designModeOnly: true }
        ),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Controller',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrResolutionActionSettings.controllerIdKey,
              KrResolutionActionSettings.controllerNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'controller',
            designModeOnly: true,
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              name: 'Role'
            }
          }
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
        )
      ]
    };
  }

  //#endregion
}
