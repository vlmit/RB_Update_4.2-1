import { reaction } from 'mobx';
import {
  AutocompleteActionLinksDataConverter,
  AutocompleteKeyValueCaptionTripleDataConverter,
  AutocompleteKeyValuePairDataConverter,
  WfeAutocompleteProperty,
  WfeBooleanProperty,
  WfeButtonProperty,
  WfeCodeProperty,
  WfeEntryProperty,
  WfeGroup,
  WfePropertyArgs,
  WfeTextProperty,
  WfeTypedChildTablePropertySettings,
  WfeTypedTableProperty,
  WorkflowActionsEditorHelper,
  WorkflowActionSettingsRowStorageBase,
  WorkflowActionSettingsStorageCoreBase,
  WorkflowActionWithSettingsBase,
  WorkflowEditorHelper,
  WorkflowHelper
} from 'tessa/ui/workflow/chunk';
import { DisposeList, FieldType, Guid, StorageArray } from '@tessa/core';
import { UIButton } from 'tessa/ui';
import { GridHelper } from 'ui/grid';
import { localize } from '@tessa/application';
import { AutocompleteDataPlainContext } from 'ui/autocomplete/core/autocompleteDataPlainContext';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { SyntaxHighlighting } from 'tessa/cards/syntaxHighlighting';
import { KrActionNotificationRowRolesSettings } from '../models/krActionNotificationRowRolesSettings';
import { KrActionOptionLinksSettings } from '../models/krActionOptionLinksSettings';
import { KrActionOptionRowSettings } from '../models/krActionOptionRowSettings';
import { AutocompleteActionNotificationRowRolesSettingsDataConverter } from './autocompleteActionNotificationRowRolesSettingsDataConverter';
import { sqlApproverRoleId } from '../../../../krProcess/krUIHelper';
import { KrTaskOptionRowSettings } from '../models/krTaskOptionRowSettings';
import { IReadOnlyKeyValueCaptionTriple } from 'tessa/ui/workflow';

/**
 * Общие методы и типы, используемые при создании редакторов действий.
 * @helper
 */
export namespace KrWorkflowActionsEditorHelper {
  /**
   * Создаёт и настраивает свойство {@link WfeButtonProperty}, по нажатию на которое выполняется добавление роли {@link performerRoleId}.
   * @param args Параметры.
   * @returns Созданное свойство.
   */
  export function createAddRoleButton<TAction extends WorkflowActionWithSettingsBase, TRole>(args: {
    /**
     * Список, содержащий освобождаемые объекты.
     */
    disposeList: DisposeList;

    /**
     * Отслеживаемый список ролей или привязка.
     */
    getRolesOrBinding: () => TRole[] | string;

    /**
     * Действие, выполняющая создание и добавление новой роли. Выполняется только, если {@link getRolesOrBinding} является списком.
     */
    addNewRole: (args: WfePropertyArgs<TAction>) => void;

    /**
     * Функция, возвращающая идентификатор роли.
     * @param role Объект роли.
     * @returns Идентификатор роли.
     */
    getRoleId: (role: TRole) => string | null | undefined;

    /**
     * Название свойства.
     * @remarks Значение по умолчанию: `addComputedRoleLink`.
     */
    propertyName?: string;

    /**
     * Заголовок кнопки.
     * @remarks Значение по умолчанию: `$CardTypes_Controls_AddComputedRoleLink`.
     */
    buttonCaption?: string;

    /**
     * Значение, показывающее, что кнопка будет доступна только в режиме редактирования шаблона.
     * @remarks Значение по умолчанию: `true`.
     */
    designModeOnly?: boolean;

    /**
     * Идентификатор добавляемой роли. Значение используется при проверке её вхождения в {@link rolesOrBinding}.
     * @remarks Значение по умолчанию: {@link sqlApproverRoleId}.
     */
    performerRoleId?: string;
  }): WfeEntryProperty<TAction> {
    const { disposeList, getRolesOrBinding, addNewRole, getRoleId } = args;
    let { propertyName, buttonCaption, designModeOnly, performerRoleId } = args;

    propertyName ??= 'addComputedRoleLink';
    buttonCaption ??= '$CardTypes_Controls_AddComputedRoleLink';
    designModeOnly ??= true;
    performerRoleId ??= sqlApproverRoleId;

    return new WfeButtonProperty<TAction>(
      args => {
        return {
          buttons: [
            UIButton.create({
              name: propertyName,
              type: 'normal',
              theme: 'control',
              caption: buttonCaption,
              buttonAction: () => {
                if (Array.isArray(getRolesOrBinding())) {
                  addNewRole(args);
                }
              }
            })
          ],
          onInitialized: async property => {
            const updateVisibilityAction = () => {
              property.visibility = !WorkflowHelper.containsWithBinding(
                getRolesOrBinding(),
                i => i && Guid.equals(getRoleId(i), performerRoleId)
              );
            };

            updateVisibilityAction();

            disposeList.add(
              reaction(() => {
                const rolesOrBinding = getRolesOrBinding();
                return Array.isArray(rolesOrBinding) ? rolesOrBinding.length : -1;
              }, updateVisibilityAction)
            );
          }
        };
      },
      { propertyName: propertyName, designModeOnly: designModeOnly }
    );
  }

  /**
   * Создаёт и настраивает таблицу "Варианты завершения".
   * @param tryGetCompleteOptionsNotificationsRecipients Возвращает параметры действия, расположенные в таблице "Варианты завершения" в списке "Получатели".
   * @param setCompleteOptionsNotificationsRecipients Устанавливает параметры действия, расположенные в таблице "Варианты завершения" в списке "Получатели".
   * @returns Созданная таблица.
   */
  export function createCompleteOptionsTableProperty<
    TAction extends WorkflowActionWithSettingsBase,
    TSettings extends WorkflowActionSettingsStorageCoreBase
  >(
    tryGetCompleteOptionsNotificationsRecipients: (
      settings: TSettings
    ) => StorageArray<KrActionNotificationRowRolesSettings> | null,
    setCompleteOptionsNotificationsRecipients: (
      settings: TSettings,
      value: StorageArray<KrActionNotificationRowRolesSettings>
    ) => void
  ): WfeTypedTableProperty<TAction, KrTaskOptionRowSettings> {
    const autocompleteKeyValuePairDataConverter = new AutocompleteKeyValuePairDataConverter();

    return new WfeTypedTableProperty<TAction, KrTaskOptionRowSettings>(
      {
        caption: '$CardTypes_Controls_CompletionOptions',
        tableOptions: {
          multiselect: true,
          columnsMetadata: GridHelper.ensureTypedMetadata<KrTaskOptionRowSettings>([
            {
              id: 'taskType',
              caption: '$CardTypes_Controls_TaskType',
              dataSourceKey: 'taskType',
              dataType: FieldType.String
            },
            {
              id: 'option',
              caption: '$CardTypes_Controls_CompletionOption',
              dataSourceKey: 'option',
              dataType: FieldType.String
            }
          ]),
          orderSetting: 'order'
        },
        hooks: [
          {
            cellInitializing: async context => {
              switch (context.cell.column.id) {
                case 'taskType':
                  context.cell.formatters.push(cellContext => {
                    const value =
                      cellContext.cell.getValue<
                        IReadOnlyKeyValueCaptionTriple<string, string | null, string | null>
                      >();
                    cellContext.formattedValue = value
                      ? `${localize(value.caption)} (${localize(value.value)})`
                      : null;
                  });
                  break;
                case 'option':
                  context.cell.formatters.push(
                    WorkflowEditorHelper.formatReadOnlyKeyValuePairValue
                  );
                  break;
              }
            }
          }
        ]
      },
      {
        propertyName: 'completeOptions',
        getRowId: row => row.rowId,
        rowFactory: action => new KrTaskOptionRowSettings(action.action, action.actionState),
        tableFactory: action =>
          WorkflowActionSettingsRowStorageBase.factory(
            KrTaskOptionRowSettings,
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
                caption: '$CardTypes_Controls_TaskType',
                dataContext: new AutocompleteDataViewContext({
                  viewAlias: 'TaskTypes',
                  refSection: 'TaskTypes',
                  idColumn: 'TypeID',
                  nameColumn: 'TypeName',
                  parameterAlias: 'NameOrCaption',
                  unique: true,
                  layoutColumns: ['TypeName', 'TypeCaption']
                }),
                dataConverter: new AutocompleteKeyValueCaptionTripleDataConverter(
                  'TypeID',
                  'TypeName',
                  'TypeCaption',
                  storage => `${localize(storage.value)} (${localize(storage.caption)})`
                ),
                onInitialized: async property => {
                  property.control.mode = AutocompleteMode.NonDroppable;
                  property.control.menu.openAction.isCollapsed = true;
                }
              },
              { propertyName: 'taskType' }
            ),
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
                dataConverter: autocompleteKeyValuePairDataConverter,
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
                dataConverter: autocompleteKeyValuePairDataConverter,
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
              args => {
                return {
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
                    args.action.action,
                    args.action.actionState
                  ),
                  onInitialized: async property => {
                    property.control.mode = AutocompleteMode.NonDroppable;
                    property.control.maxRows = 15;
                  }
                };
              },
              {
                propertyName: 'recipients',
                designModeOnly: true,
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
            TAction,
            TSettings,
            KrTaskOptionRowSettings,
            KrActionNotificationRowRolesSettings
          >({
            tryGetSettingsItems: tryGetCompleteOptionsNotificationsRecipients,
            getSettingsItems: settings => {
              let value = tryGetCompleteOptionsNotificationsRecipients(settings);

              if (!value) {
                value = WorkflowActionSettingsRowStorageBase.factory(
                  KrActionNotificationRowRolesSettings,
                  settings.action,
                  settings.actionState
                );
                setCompleteOptionsNotificationsRecipients(settings, value);
              }

              return value;
            },
            getRowItems: row => row.recipients,
            getRowId: row => row.rowId,
            getParentRowId: row => row.optionRowId,
            setParentRowId: (row, parentRowId) => (row.optionRowId = parentRowId)
          })
        ]
      }
    );
  }

  /**
   * Создаёт и настраивает таблицу "Варианты завершения действия".
   * @param tryGetActionCompleteOptionsNotificationsRecipients Возвращает параметры действия, расположенные в таблице "Варианты завершения действия" в списке "Получатели".
   * @param setActionCompleteOptionsNotificationsRecipients Устанавливает параметры действия, расположенные в таблице "Варианты завершения действия" в списке "Получатели".
   * @param tryGetActionCompleteOptionsLinks Возвращает параметры действия, расположенные в таблице "Варианты завершения действия" в списке "Переходы".
   * @param setActionCompleteOptionsLinks Устанавливает параметры действия, расположенные в таблице "Варианты завершения действия" в списке "Переходы".
   * @returns Созданная таблица.
   */
  export function createActionCompleteOptionsTableProperty<
    TAction extends WorkflowActionWithSettingsBase,
    TSettings extends WorkflowActionSettingsStorageCoreBase
  >(
    tryGetActionCompleteOptionsNotificationsRecipients: (
      settings: TSettings
    ) => StorageArray<KrActionNotificationRowRolesSettings> | null,
    setActionCompleteOptionsNotificationsRecipients: (
      settings: TSettings,
      value: StorageArray<KrActionNotificationRowRolesSettings>
    ) => void,
    tryGetActionCompleteOptionsLinks: (
      settings: TSettings
    ) => StorageArray<KrActionOptionLinksSettings> | null,
    setActionCompleteOptionsLinks: (
      settings: TSettings,
      value: StorageArray<KrActionOptionLinksSettings>
    ) => void
  ): WfeTypedTableProperty<TAction, KrActionOptionRowSettings> {
    const autocompleteKeyValuePairDataConverter = new AutocompleteKeyValuePairDataConverter();

    return new WfeTypedTableProperty<TAction, KrActionOptionRowSettings>(
      args => {
        return {
          caption: '$CardTypes_Controls_CompletionOptionsAction',
          disabled: true,
          tableOptions: {
            multiselect: true,
            columnsMetadata: GridHelper.ensureTypedMetadata<KrActionOptionRowSettings>([
              {
                id: 'actionOption',
                caption: '$CardTypes_Controls_CompletionOptionAction',
                dataSourceKey: 'actionOption',
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
                  case 'actionOption':
                    context.cell.formatters.push(
                      WorkflowEditorHelper.formatReadOnlyKeyValuePairValue
                    );
                    break;
                  case 'links':
                    context.cell.formatters.push(cellContext => {
                      cellContext.formattedValue = WorkflowHelper.join(
                        tryGetActionCompleteOptionsLinks(args.action.settings as TSettings),
                        row => row.actionOptionRowId,
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
        };
      },
      {
        propertyName: 'actionCompleteOptions',
        getRowId: row => row.rowId,
        rowFactory: action => new KrActionOptionRowSettings(action.action, action.actionState),
        tableFactory: action =>
          WorkflowActionSettingsRowStorageBase.factory(
            KrActionOptionRowSettings,
            action.action,
            action.actionState
          ),
        title: '$CardTypes_Controls_CompletionOption',
        hasBindingColumns: true,
        rowEditorLayout: {
          items: [
            new WfeAutocompleteProperty(
              {
                caption: '$CardTypes_Controls_CompletionOptionAction',
                dataContext: new AutocompleteDataPlainContext({ items: [] }),
                disabled: true,
                dataConverter: autocompleteKeyValuePairDataConverter,
                onInitialized: async property => {
                  property.control.mode = AutocompleteMode.NonDroppable;
                  property.control.menu.openAction.isCollapsed = true;
                }
              },
              { propertyName: 'actionOption' }
            ),
            WorkflowActionsEditorHelper.createLinksControl(
              args =>
                new AutocompleteActionLinksDataConverter(
                  KrActionOptionLinksSettings,
                  args.action.action,
                  args.action.actionState
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
                dataConverter: autocompleteKeyValuePairDataConverter,
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
              args => {
                return {
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
                    args.action.action,
                    args.action.actionState
                  ),
                  onInitialized: async property => {
                    property.control.mode = AutocompleteMode.NonDroppable;
                    property.control.maxRows = 15;
                  }
                };
              },
              {
                propertyName: 'recipients',
                designModeOnly: true,
                bindingAllowed: true,
                bindingType: {
                  isMultiple: true,
                  name: 'Role'
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
            )
          ]
        },
        childTableSettings: [
          new WfeTypedChildTablePropertySettings<
            TAction,
            TSettings,
            KrActionOptionRowSettings,
            KrActionNotificationRowRolesSettings
          >({
            tryGetSettingsItems: tryGetActionCompleteOptionsNotificationsRecipients,
            getSettingsItems: settings => {
              let value = tryGetActionCompleteOptionsNotificationsRecipients(settings);

              if (!value) {
                value = WorkflowActionSettingsRowStorageBase.factory(
                  KrActionNotificationRowRolesSettings,
                  settings.action,
                  settings.actionState
                );
                setActionCompleteOptionsNotificationsRecipients(settings, value);
              }

              return value;
            },
            getRowItems: row => row.recipients,
            getRowId: row => row.rowId,
            getParentRowId: row => row.optionRowId,
            setParentRowId: (row, parentRowId) => (row.optionRowId = parentRowId)
          }),
          new WfeTypedChildTablePropertySettings<
            TAction,
            TSettings,
            KrActionOptionRowSettings,
            KrActionOptionLinksSettings
          >({
            tryGetSettingsItems: tryGetActionCompleteOptionsLinks,
            getSettingsItems: settings => {
              let value = tryGetActionCompleteOptionsLinks(settings);

              if (!value) {
                value = WorkflowActionSettingsRowStorageBase.factory(
                  KrActionOptionLinksSettings,
                  settings.action,
                  settings.actionState
                );
                setActionCompleteOptionsLinks(settings, value);
              }

              return value;
            },
            getRowItems: row => row.links,
            getRowId: row => row.rowId,
            getParentRowId: row => row.actionOptionRowId,
            setParentRowId: (row, parentRowId) => (row.actionOptionRowId = parentRowId)
          })
        ]
      }
    );
  }
}
