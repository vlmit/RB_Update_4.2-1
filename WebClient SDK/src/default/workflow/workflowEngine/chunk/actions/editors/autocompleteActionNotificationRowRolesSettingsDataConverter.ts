import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { WorkflowActionStateStorage, WorkflowActionStorage } from 'tessa/ui/workflow/chunk';
import { KrActionNotificationRowRolesSettings } from '../models/krActionNotificationRowRolesSettings';

/**
 * Конвертер, выполняющий преобразование между {@link KrActionNotificationRowRolesSettings} и {@link IAutocompleteRecord}.
 */
export class AutocompleteActionNotificationRowRolesSettingsDataConverter implements IAutocompleteDataConverter<KrActionNotificationRowRolesSettings> {
  //#region ctor

  constructor(
    private readonly _action: WorkflowActionStorage,
    private readonly _actionState?: WorkflowActionStateStorage
  ) {}

  //#endregion

  //#region IAutocompleteDataConverter members

  toRecord(storage: KrActionNotificationRowRolesSettings): IAutocompleteRecord {
    return {
      id: storage.role!.key,
      name: storage.role!.value
    };
  }

  fromRecord(record: IAutocompleteRecord): KrActionNotificationRowRolesSettings {
    const storage = new KrActionNotificationRowRolesSettings(this._action, this._actionState);
    storage.role = { key: record.id as string, value: record.name };

    return storage;
  }

  //#endregion
}
