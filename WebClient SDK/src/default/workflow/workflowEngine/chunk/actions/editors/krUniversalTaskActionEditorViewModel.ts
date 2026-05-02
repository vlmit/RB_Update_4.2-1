import {
  AutocompleteKeyValuePairDataConverter,
  WfeAutocompleteProperty,
  WfeCodeProperty,
  WfeGroup,
  WfeLayout,
  WfeTextProperty,
  WorkflowActionsEditorHelper,
  WorkflowActionSettingsEditorViewModelBase,
  WfeBooleanProperty,
  WfeTypedTableProperty,
  WorkflowHelper,
  WfeTypedChildTablePropertySettings,
  WorkflowTaskActionOptionNotificationRowSettings,
  AutocompleteActionLinksDataConverter,
  AutocompleteKeyValuePairBindingDataConverter,
  WfeLabelProperty
} from 'tessa/ui/workflow/chunk';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { AutocompleteDataViewContext } from 'ui/autocomplete';
import { KrUniversalTaskActionSettings } from '../models/krUniversalTaskActionSettings';
import { KrUniversalTaskActionStorage } from '../models/krUniversalTaskActionStorage';
import { GridHelper } from 'ui/grid';
import { FieldType, Guid } from '@tessa/core';
import { localize } from '@tessa/application';
import { KrUniversalTaskActionOptionRowLinksSettings } from '../models/krUniversalTaskActionOptionRowLinksSettings';
import { KrUniversalTaskActionOptionRowSettings } from '../models/krUniversalTaskActionOptionRowSettings';
import { KrUniversalTaskActionButtonTaskRoleRowSettings } from '../models/krUniversalTaskActionButtonTaskRoleRowSettings';
import { AutocompleteUniversalTaskActionButtonTaskRoleRowSettingsDataConverter } from './autocompleteUniversalTaskActionButtonTaskRoleRowSettingsDataConverter';

/**
 * Редактор действия "Настраиваемое задание".
 */
export class KrUniversalTaskActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region fields

  protected readonly _autocompleteKeyValuePairDataConverter =
    new AutocompleteKeyValuePairDataConverter();

  //#endregion

  //#region properties

  protected get actionSettings(): KrUniversalTaskActionSettings {
    return <KrUniversalTaskActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrUniversalTaskActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
        WorkflowActionsEditorHelper.createFunctionRoleTableProperty(
          () => this.actionSettings.functionRoleRoles,
          this._autocompleteKeyValuePairDataConverter,
          this.disposeList
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
              KrUniversalTaskActionSettings.kindIdKey,
              KrUniversalTaskActionSettings.kindCaptionKey
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
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrUniversalTaskActionStorage>(
          {
            disposeList: this.disposeList,
            getPeriod: () => this.actionSettings.period,
            resetPeriod: () => (this.actionSettings.period = null),
            getPlanned: () => this.actionSettings.planned,
            resetPlanned: () => (this.actionSettings.planned = null)
          }
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
        new WfeGroup(
          '$CardTypes_Blocks_Controls_TaskNotifications',
          !this.actionSettings.taskNotifications?.length
        ),
        WorkflowActionsEditorHelper.createNotificationsTableProperty(
          () => this.actionSettings.taskNotificationFunctionRoles
        ),
        new WfeGroup('$CardTypes_Blocks_ProcessingSettings'),
        this.createCompletionOptionsTableProperty(),
        WorkflowActionsEditorHelper.createEventsTableProperty(
          this._autocompleteKeyValuePairDataConverter
        )
      ]
    };
  }

  //#endregion

  //#region protected methods

  createCompletionOptionsTableProperty(): WfeTypedTableProperty<
    KrUniversalTaskActionStorage,
    KrUniversalTaskActionOptionRowSettings
  > {
    return new WfeTypedTableProperty<
      KrUniversalTaskActionStorage,
      KrUniversalTaskActionOptionRowSettings
    >(
      {
        caption: '$CardTypes_Controls_CompletionOptions',
        disabled: !this.designMode,
        tableOptions: {
          multiselect: true,
          columnsMetadata: GridHelper.ensureTypedMetadata<KrUniversalTaskActionOptionRowSettings>([
            {
              id: 'option',
              caption: '$CardTypes_Controls_CompletionOption',
              dataSourceKey: 'caption',
              dataType: FieldType.String
            },
            {
              id: 'links',
              caption: '$CardTypes_Controls_Links',
              dataSourceKey: 'links',
              dataType: FieldType.String
            }
          ]),
          orderSetting: 'order'
        },
        hooks: [
          {
            cellInitializing: async context => {
              switch (context.cell.column.id) {
                case 'links':
                  context.cell.formatters.push(cellContext => {
                    cellContext.formattedValue = WorkflowHelper.join(
                      this.actionSettings.completeOptionLinks,
                      row => row.buttonRowId,
                      context.cell.row.id,
                      row => `${localize(row.link?.value)} (${localize(row.link?.caption)})`,
                      ', '
                    );
                  });
                  break;
              }
            },
            rowDeleted: context => {
              const parentRowId = context.rowId;

              WorkflowHelper.removeAllById(
                this.actionSettings.completeOptionLinks,
                parentRowId,
                i => i.buttonRowId
              );

              WorkflowHelper.removeAllById(
                this.actionSettings.completeOptionNotifications,
                parentRowId,
                i => i.taskOptionRowId,
                deletedRow => {
                  const deletedRowId = deletedRow.rowId;

                  WorkflowHelper.removeAllById(
                    this.actionSettings.completionOptionsFunctionRoles,
                    deletedRowId,
                    i => i.taskButtonRowId
                  );

                  WorkflowHelper.removeAllById(
                    this.actionSettings.taskNotificationFunctionRoles,
                    deletedRowId,
                    i => i.completionNotificationRowId
                  );

                  WorkflowHelper.removeAllById(
                    this.actionSettings.taskNotificationRoles,
                    deletedRowId,
                    i => i.taskCompletionNotificationsRowId
                  );
                }
              );
            }
          }
        ]
      },
      {
        propertyName: 'completeOptions',
        getRowId: row => row.rowId,
        rowFactory: action =>
          new KrUniversalTaskActionOptionRowSettings(action.action, action.actionState),
        tableFactory: action =>
          KrUniversalTaskActionOptionRowSettings.factory(
            KrUniversalTaskActionOptionRowSettings,
            action.action,
            action.actionState
          ),
        title: '$CardTypes_Controls_CompletionOption',
        designModeOnly: true,
        hasBindingColumns: true,
        rowEditorLayout: {
          items: [
            new WfeTextProperty(
              args => {
                return {
                  caption: '$CardTypes_Control_CompletionOptions_ID',
                  required: true,
                  onInitialized: async property => {
                    property.control.minRows = 1;
                    property.control.maxRows = 1;

                    property.control.validationContainer.add(
                      context => {
                        if (!Guid.isValid(context.rawValue)) {
                          context.addError({
                            message: localize('$KrActions_UniversalTask_CompletionOptionIDEmpty')
                          });
                        }
                      },
                      { order: 0 }
                    );
                    property.control.validationContainer.add(
                      context => {
                        const rowId = (args.row as KrUniversalTaskActionOptionRowSettings).rowId;

                        if (
                          context.validationResult.isSuccessful &&
                          this.actionSettings.completeOptions?.some(
                            i =>
                              !Guid.equals(i.rowId, rowId) &&
                              Guid.equals(i.optionId, context.rawValue)
                          )
                        ) {
                          context.addError({
                            message: localize(
                              '$KrActions_UniversalTask_CompletionOptionIDNotUnique'
                            )
                          });
                        }
                      },
                      { order: 1 }
                    );
                  }
                };
              },
              { propertyName: 'optionId' }
            ),
            new WfeTextProperty(
              {
                caption: '$CardTypes_Controls_CompletionOption',
                required: true,
                onInitialized: async property => {
                  property.control.minRows = 1;
                  property.control.maxRows = 5;
                }
              },
              { propertyName: 'caption' }
            ),
            new WfeAutocompleteProperty(
              {
                caption: '$Views_TaskAssignedRoles_TaskRoleName',
                dataContext: new AutocompleteDataViewContext({
                  viewAlias: 'FunctionRoleCards',
                  refSection: 'FunctionRoles',
                  idColumn: 'FunctionRoleID',
                  nameColumn: 'FunctionRoleCaption',
                  parameterAlias: 'Caption',
                  unique: true,
                  multiple: true
                }),
                dataConverter:
                  new AutocompleteUniversalTaskActionButtonTaskRoleRowSettingsDataConverter(
                    this.actionSettings.action,
                    this.actionSettings.actionState
                  ),
                onInitialized: async property => {
                  property.control.menu.openAction.isCollapsed = true;
                }
              },
              { propertyName: 'functionRoles' }
            ),
            new WfeTextProperty(
              {
                caption: '$CardTypes_Controls_TaskDescription',
                onInitialized: async property => {
                  property.control.minRows = 1;
                  property.control.maxRows = 1;
                }
              },
              { propertyName: 'digest', bindingAllowed: true }
            ),
            new WfeBooleanProperty(
              {
                caption: '$CardTypes_Controls_ShowComment'
              },
              { propertyName: 'isShowComment', bindingAllowed: true }
            ),
            new WfeBooleanProperty(
              {
                caption: '$CardTypes_Controls_Additional'
              },
              { propertyName: 'isAdditionalOption', bindingAllowed: true }
            ),
            WorkflowActionsEditorHelper.createLinksControl(
              new AutocompleteActionLinksDataConverter(
                KrUniversalTaskActionOptionRowLinksSettings,
                this.actionSettings.action,
                this.actionSettings.actionState
              )
            ),
            new WfeCodeProperty(
              {
                caption: '$CardTypes_Controls_Scenario',
                highlightingMode: SyntaxHighlighting.CSharp,
                showLineNumbers: true,
                onInitialized: async property => {
                  property.control.minRows = 5;
                  property.control.maxRows = 30;
                }
              },
              { propertyName: 'script' }
            ),
            new WfeGroup('$CardTypes_Blocks_Controls_TaskCompletionNotifications', true),
            WorkflowActionsEditorHelper.createOptionNotificationsTableProperty(
              () => this.actionSettings.taskNotificationFunctionRoles,
              () => this.actionSettings.taskNotificationRoles
            )
          ]
        },
        childTableSettings: [
          new WfeTypedChildTablePropertySettings<
            KrUniversalTaskActionStorage,
            KrUniversalTaskActionSettings,
            KrUniversalTaskActionOptionRowSettings,
            KrUniversalTaskActionButtonTaskRoleRowSettings
          >({
            getParentRowId: row => row.taskButtonRowId,
            getRowId: row => row.rowId,
            getRowItems: row => row.functionRoles,
            getSettingsItems: settings =>
              (settings.completionOptionsFunctionRoles ??=
                KrUniversalTaskActionButtonTaskRoleRowSettings.factory(
                  KrUniversalTaskActionButtonTaskRoleRowSettings,
                  settings.action,
                  settings.actionState
                )),
            setParentRowId: (row, parentRowId) => (row.taskButtonRowId = parentRowId),
            tryGetSettingsItems: settings => settings.completionOptionsFunctionRoles
          }),
          new WfeTypedChildTablePropertySettings<
            KrUniversalTaskActionStorage,
            KrUniversalTaskActionSettings,
            KrUniversalTaskActionOptionRowSettings,
            KrUniversalTaskActionOptionRowLinksSettings
          >({
            getParentRowId: row => row.buttonRowId,
            getRowId: row => row.rowId,
            getRowItems: row => row.links,
            getSettingsItems: settings =>
              (settings.completeOptionLinks ??= KrUniversalTaskActionOptionRowLinksSettings.factory(
                KrUniversalTaskActionOptionRowLinksSettings,
                settings.action,
                settings.actionState
              )),
            setParentRowId: (row, parentRowId) => (row.buttonRowId = parentRowId),
            tryGetSettingsItems: settings => settings.completeOptionLinks
          }),
          new WfeTypedChildTablePropertySettings<
            KrUniversalTaskActionStorage,
            KrUniversalTaskActionSettings,
            KrUniversalTaskActionOptionRowSettings,
            WorkflowTaskActionOptionNotificationRowSettings
          >({
            getParentRowId: row => row.taskButtonRowId,
            getRowId: row => row.rowId,
            getRowItems: row => row.optionNotifications,
            getSettingsItems: settings =>
              (settings.completeOptionNotifications ??=
                WorkflowTaskActionOptionNotificationRowSettings.factory(
                  WorkflowTaskActionOptionNotificationRowSettings,
                  settings.action,
                  settings.actionState
                )),
            setParentRowId: (row, parentRowId) => (row.taskButtonRowId = parentRowId),
            tryGetSettingsItems: settings => settings.completeOptionNotifications
          })
        ]
      }
    );
  }

  //#endregion
}
