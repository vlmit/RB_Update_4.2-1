import {
  AutocompleteKeyValuePairBindingDataConverter,
  WfeAutocompleteProperty,
  WfeGroup,
  WfeLayout,
  WfeTextProperty,
  WorkflowActionSettingsEditorViewModelBase
} from 'tessa/ui/workflow/chunk';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { KrRouteInitializationActionStorage } from '../models/krRouteInitializationActionStorage';
import { KrRouteInitializationActionSettings } from '../models/krRouteInitializationActionSettings';

/**
 * Редактор действия "Инициализация маршрута".
 */
export class KrRouteInitializationActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region base overrides

  protected override initializeLayout(): WfeLayout<KrRouteInitializationActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
        new WfeAutocompleteProperty(
          {
            caption: '$CardTypes_Controls_TheInitiatorOfTheApproval',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'Users',
              idColumn: 'UserID',
              nameColumn: 'UserName',
              parameterAlias: 'Name',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrRouteInitializationActionSettings.initiatorIdKey,
              KrRouteInitializationActionSettings.initiatorNameKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.menu.openAction.isCollapsed = true;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'initiator',
            designModeOnly: true,
            bindingAllowed: true,
            bindingType: {
              isMultiple: false,
              viewAlias: 'Users',
              viewReference: 'User',
              idColumn: 'ID',
              nameColumn: 'Name'
            }
          }
        ),
        new WfeTextProperty(
          {
            caption: '$CardTypes_Controls_CommentToApprovalCycle',
            onInitialized: async property => {
              property.control.minRows = 3;
              property.control.maxRows = 15;
            }
          },
          {
            propertyName: 'initiatorComment',
            designModeOnly: true,
            bindingAllowed: true
          }
        )
      ]
    };
  }

  //#endregion
}
