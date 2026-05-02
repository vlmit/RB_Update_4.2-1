import { FieldType } from '@tessa/core';
import { localize } from '@tessa/application';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { GridHelper } from 'ui/grid';
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
  WfeTypedTableProperty,
  WorkflowHelper,
  AutocompleteActionLinksDataConverter,
  WfeTypedChildTablePropertySettings,
  WorkflowEditorHelper,
  WorkflowTaskActionOptionRowLinksSettings
} from 'tessa/ui/workflow/chunk';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { KrTaskRegistrationActionSettings } from '../models/krTaskRegistrationActionSettings';
import { KrTaskRegistrationActionStorage } from '../models/krTaskRegistrationActionStorage';
import { KrTaskRegistrationActionOptionRowSettings } from '../models/krTaskRegistrationActionOptionRowSettings';
import { AutocompleteActionNotificationRowRolesSettingsDataConverter } from './autocompleteActionNotificationRowRolesSettingsDataConverter';
import { KrActionNotificationRowRolesSettings } from '../models/krActionNotificationRowRolesSettings';

/**
 * Редактор действия "Задание регистрации".
 */
export class KrTaskRegistrationActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region fields

  protected readonly _autocompleteKeyValuePairDataConverter =
    new AutocompleteKeyValuePairDataConverter();

  //#endregion

  //#region properties

  protected get actionSettings(): KrTaskRegistrationActionSettings {
    return <KrTaskRegistrationActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrTaskRegistrationActionStorage> {
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
              KrTaskRegistrationActionSettings.performerIdKey,
              KrTaskRegistrationActionSettings.performerNameKey
            ),
            onInitialized: async property => {
              property.control.menu.openAction.isCollapsed = true;
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'performer',
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
              KrTaskRegistrationActionSettings.authorIdKey,
              KrTaskRegistrationActionSettings.authorNameKey
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
              KrTaskRegistrationActionSettings.kindIdKey,
              KrTaskRegistrationActionSettings.kindCaptionKey
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
        ...WorkflowActionsEditorHelper.createPlannedAndPeriodControls<KrTaskRegistrationActionStorage>(
          {
            disposeList: this.disposeList,
            getPeriod: () => this.actionSettings.period,
            resetPeriod: () => (this.actionSettings.period = null),
            getPlanned: () => this.actionSettings.planned,
            resetPlanned: () => (this.actionSettings.planned = null)
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
            caption: '$CardTypes_Columns_Controls_EditAnyFiles'
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
              KrTaskRegistrationActionSettings.notificationIdKey,
              KrTaskRegistrationActionSettings.notificationNameKey
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
        new WfeGroup('$CardTypes_Blocks_ProcessingSettings', true),
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
    KrTaskRegistrationActionStorage,
    KrTaskRegistrationActionOptionRowSettings
  > {
    return new WfeTypedTableProperty<
      KrTaskRegistrationActionStorage,
      KrTaskRegistrationActionOptionRowSettings
    >(
      {
        caption: '$CardTypes_Controls_CompletionOptions',
        disabled: !this.designMode,
        tableOptions: {
          multiselect: true,
          columnsMetadata:
            GridHelper.ensureTypedMetadata<KrTaskRegistrationActionOptionRowSettings>([
              {
                id: 'option',
                caption: '$CardTypes_Controls_CompletionOption',
                dataSourceKey: 'option',
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
                case 'option':
                  context.cell.formatters.push(
                    WorkflowEditorHelper.formatReadOnlyKeyValuePairValue
                  );
                  break;
                case 'links':
                  context.cell.formatters.push(cellContext => {
                    cellContext.formattedValue = WorkflowHelper.join(
                      this.actionSettings.completeOptionsLinks,
                      row => row.optionRowId,
                      context.cell.row.id,
                      row => `${localize(row.link?.value)} (${localize(row.link?.caption)})`,
                      ', '
                    );
                  });
                  break;
              }
            }
          }
        ]
      },
      {
        propertyName: 'completeOptions',
        getRowId: row => row.rowId,
        rowFactory: action =>
          new KrTaskRegistrationActionOptionRowSettings(action.action, action.actionState),
        tableFactory: action =>
          KrTaskRegistrationActionOptionRowSettings.factory(
            KrTaskRegistrationActionOptionRowSettings,
            action.action,
            action.actionState
          ),
        title: '$CardTypes_Controls_CompletionOption',
        designModeOnly: true,
        hasBindingColumns: true,
        rowEditorLayout: {
          items: [
            new WfeAutocompleteProperty(
              {
                caption: '$CardTypes_Controls_CompletionOption',
                dataContext: new AutocompleteDataViewContext({
                  viewAlias: 'CompletionOptions',
                  idColumn: 'OptionID',
                  nameColumn: 'OptionCaption',
                  parameterAlias: 'Caption',
                  unique: true
                }),
                dataConverter: this._autocompleteKeyValuePairDataConverter,
                onInitialized: async property => {
                  property.control.mode = AutocompleteMode.NonDroppable;
                  property.control.menu.openAction.isCollapsed = true;
                }
              },
              { propertyName: 'option' }
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
            WorkflowActionsEditorHelper.createLinksControl(
              new AutocompleteActionLinksDataConverter(
                WorkflowTaskActionOptionRowLinksSettings,
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
                  KrTaskRegistrationActionOptionRowSettings.notificationIdKey,
                  KrTaskRegistrationActionOptionRowSettings.notificationNameKey
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
                dataConverter: new AutocompleteActionNotificationRowRolesSettingsDataConverter(
                  this.actionSettings.action,
                  this.actionSettings.actionState
                ),
                onInitialized: async property => {
                  property.control.mode = AutocompleteMode.NonDroppable;
                  property.control.maxRows = 15;
                }
              },
              { propertyName: 'recipients' }
            ),
            new WfeBooleanProperty(
              {
                caption: '$CardTypes_Controls_SendToPerformer'
              },
              { propertyName: 'sendToPerformer', bindingAllowed: true }
            ),
            new WfeBooleanProperty(
              {
                caption: '$CardTypes_Controls_SendToAuthor'
              },
              { propertyName: 'sendToAuthor', bindingAllowed: true }
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
            )
          ]
        },
        childTableSettings: [
          new WfeTypedChildTablePropertySettings<
            KrTaskRegistrationActionStorage,
            KrTaskRegistrationActionSettings,
            KrTaskRegistrationActionOptionRowSettings,
            KrActionNotificationRowRolesSettings
          >({
            getParentRowId: row => row.optionRowId,
            getRowId: row => row.rowId,
            getRowItems: row => row.recipients,
            tryGetSettingsItems: settings => settings.completeOptionsNotificationsRecipients,
            getSettingsItems: settings =>
              (settings.completeOptionsNotificationsRecipients ??=
                KrActionNotificationRowRolesSettings.factory(
                  KrActionNotificationRowRolesSettings,
                  settings.action,
                  settings.actionState
                )),
            setParentRowId: (row, parentRowId) => (row.optionRowId = parentRowId)
          }),
          new WfeTypedChildTablePropertySettings<
            KrTaskRegistrationActionStorage,
            KrTaskRegistrationActionSettings,
            KrTaskRegistrationActionOptionRowSettings,
            WorkflowTaskActionOptionRowLinksSettings
          >({
            getParentRowId: row => row.optionRowId,
            getRowId: row => row.rowId,
            getRowItems: row => row.links,
            tryGetSettingsItems: settings => settings.completeOptionsLinks,
            getSettingsItems: settings =>
              (settings.completeOptionsLinks ??= WorkflowTaskActionOptionRowLinksSettings.factory(
                WorkflowTaskActionOptionRowLinksSettings,
                settings.action,
                settings.actionState
              )),
            setParentRowId: (row, parentRowId) => (row.optionRowId = parentRowId)
          })
        ]
      }
    );
  }

  //#endregion
}
