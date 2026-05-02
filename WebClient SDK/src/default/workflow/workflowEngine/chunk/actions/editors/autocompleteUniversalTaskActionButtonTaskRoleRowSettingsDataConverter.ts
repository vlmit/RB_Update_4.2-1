import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { WorkflowActionStateStorage, WorkflowActionStorage } from 'tessa/ui/workflow/chunk';
import { KrUniversalTaskActionButtonTaskRoleRowSettings } from '../models/krUniversalTaskActionButtonTaskRoleRowSettings';

/**
 * Конвертер, выполняющий преобразование между {@link KrUniversalTaskActionButtonTaskRoleRowSettings} и {@link IAutocompleteRecord}.
 */
export class AutocompleteUniversalTaskActionButtonTaskRoleRowSettingsDataConverter implements IAutocompleteDataConverter<KrUniversalTaskActionButtonTaskRoleRowSettings> {
  //#region ctor

  constructor(
    private readonly _action: WorkflowActionStorage,
    private readonly _actionState?: WorkflowActionStateStorage
  ) {}

  //#endregion

  //#region IAutocompleteDataConverter members

  toRecord(storage: KrUniversalTaskActionButtonTaskRoleRowSettings): IAutocompleteRecord {
    return {
      id: storage.taskRole!.key,
      name: storage.taskRole!.value
    };
  }

  fromRecord(record: IAutocompleteRecord): KrUniversalTaskActionButtonTaskRoleRowSettings {
    const storage = new KrUniversalTaskActionButtonTaskRoleRowSettings(
      this._action,
      this._actionState
    );
    storage.taskRole = { key: record.id as string, value: record.name };

    return storage;
  }

  //#endregion
}
