import { AutocompleteDataViewContext } from 'ui/autocomplete';
import { FieldType } from '@tessa/core';
import {
  AutocompleteKeyValuePairBindingDataConverter,
  WfeAutocompleteProperty,
  WfeGroup,
  WfeLayout,
  WorkflowActionSettingsEditorViewModelBase
} from 'tessa/ui/workflow/chunk';
import { KrChangeStateActionSettings } from '../models/krChangeStateActionSettings';
import { KrChangeStateActionStorage } from '../models/krChangeStateActionStorage';

/**
 * Редактор действия "Смена состояния".
 */
export class KrChangeStateActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region properties

  protected get actionSettings(): KrChangeStateActionSettings {
    return <KrChangeStateActionSettings>this._action.settings;
  }

  //#endregion

  //#region methods

  protected override initializeLayout(): WfeLayout<KrChangeStateActionStorage> {
    return {
      items: [
        new WfeGroup('$CardTypes_Blocks_MainInformation'),
        new WfeAutocompleteProperty(
          {
            caption: '$UI_KrChangeState_State',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'KrDocStates',
              idColumn: 'StateID',
              nameColumn: 'StateName',
              parameterAlias: 'Name',
              multiple: false
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              KrChangeStateActionSettings.stateIdKey,
              KrChangeStateActionSettings.stateNameKey
            )
          },
          {
            propertyName: 'state',
            bindingAllowed: true,
            bindingType: {
              idColumn: 'ID',
              nameColumn: 'Name',
              viewAlias: 'KrDocStates',
              viewReference: 'State',
              complexPrefix: 'State',
              isMultiple: false,
              paramTypeObjectName: 'KrDocState',
              idColumnTypeOverride: FieldType.Int
            }
          }
        )
      ]
    };
  }

  //#endregion
}
