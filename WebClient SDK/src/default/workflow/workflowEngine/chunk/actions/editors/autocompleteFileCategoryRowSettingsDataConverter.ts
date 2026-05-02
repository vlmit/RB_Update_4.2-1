import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { WorkflowActionStateStorage, WorkflowActionStorage } from 'tessa/ui/workflow/chunk';
import { KrFileCategoryRowSettings } from '../models/krFileCategoryRowSettings';

/**
 * Конвертер, выполняющий преобразование между {@link KrFileCategoryRowSettings} и {@link IAutocompleteRecord}.
 */
export class AutocompleteFileCategoryRowSettingsDataConverter implements IAutocompleteDataConverter<KrFileCategoryRowSettings> {
  //#region ctor

  constructor(
    private readonly _action: WorkflowActionStorage,
    private readonly _actionState?: WorkflowActionStateStorage
  ) {}

  //#endregion

  //#region IAutocompleteDataConverter members

  toRecord(storage: KrFileCategoryRowSettings): IAutocompleteRecord {
    return {
      id: storage.fileCategory!.key,
      name: storage.fileCategory!.value
    };
  }

  fromRecord(record: IAutocompleteRecord): KrFileCategoryRowSettings {
    const storage = new KrFileCategoryRowSettings(this._action, this._actionState);
    storage.fileCategory = { key: record.id as string, value: record.name };

    return storage;
  }

  //#endregion
}
