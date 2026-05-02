import {
  AutocompleteKeyValuePairBindingDataConverter,
  AutocompleteRolesDataConverter,
  WfeAutocompleteProperty,
  WfeBooleanProperty,
  WfeGroup,
  WfeLayout,
  WfeTextProperty,
  WorkflowActionRoleRowSettings,
  WorkflowActionSettingsEditorViewModelBase
} from 'tessa/ui/workflow/chunk';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { KrAcquaintanceActionSettings } from '../models/krAcquaintanceActionSettings';
import { KrAcquaintanceActionStorage } from '../models/krAcquaintanceActionStorage';

/**
 * Редактор действия "Ознакомление".
 */
export class KrAcquaintanceActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region properties

  protected get actionSettings(): KrAcquaintanceActionSettings {
    return <KrAcquaintanceActionSettings>this._action.settings;
  }

  //#endregion

  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrAcquaintanceActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_Recipients',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              multiple: true
            }),
            dataConverter: new AutocompleteRolesDataConverter(
              WorkflowActionRoleRowSettings,
              this.actionSettings.action,
              this.actionSettings.actionState
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.menu.openAction.isCollapsed = true;
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
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_SenderUserOrContextRole',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              parameterAlias: 'Name',
              multiple: false
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrAcquaintanceActionSettings.senderIdKey,
              KrAcquaintanceActionSettings.senderNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.menu.openAction.isCollapsed = true;
            }
          },
          {
            propertyName: 'sender',
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              name: 'Role'
            }
          }
        ),
        new WfeTextProperty(
          {
            caption: '$CardTypes_Controls_Comment',
            onInitialized: async property => {
              property.control.minRows = 5;
              property.control.maxRows = 10;
            }
          },
          { propertyName: 'comment', bindingAllowed: true }
        ),
        new WfeTextProperty(
          {
            caption: '$CardTypes_Blocks_Controls_PlaceholderAliases',
            tooltip: '$CardTypes_Controls_PlaceholderAliases_ToolTip',
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 10;
            }
          },
          { propertyName: 'aliasMetadata', bindingAllowed: true }
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
              KrAcquaintanceActionSettings.notificationIdKey,
              KrAcquaintanceActionSettings.notificationNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.menu.openAction.isCollapsed = true;
            }
          },
          {
            propertyName: 'notification',
            bindingAllowed: true,
            bindingType: {
              idColumn: 'ID',
              nameColumn: 'Name',
              viewAlias: 'Notifications',
              viewReference: 'Notification',
              complexPrefix: 'Notification',
              isMultiple: false,
              paramTypeObjectName: 'Notification'
            }
          }
        ),
        new WfeBooleanProperty(
          {
            caption: '$CardTypes_Controls_ExcludeDeputies'
          },
          { propertyName: 'excludeDeputies', bindingAllowed: true }
        )
      ]
    };
  }

  //#endregion
}
