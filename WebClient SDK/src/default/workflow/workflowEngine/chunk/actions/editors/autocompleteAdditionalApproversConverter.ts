import { WorkflowActionStateStorage, WorkflowActionStorage } from 'tessa/ui/workflow/chunk';
import { IAutocompleteDataConverter, IAutocompleteRecord } from 'ui/autocomplete';
import { KrAdditionalApproversSettings } from '../models/krAdditionalApproversSettings';

/**
 * Конвертер, выполняющий преобразование между {@link KrAdditionalApproversSettings} и {@link IAutocompleteRecord}.
 */
export class AutocompleteAdditionalApproversConverter implements IAutocompleteDataConverter<KrAdditionalApproversSettings> {
  //#region ctor

  constructor(
    private readonly _getCurrentMainApproverRowId: () => string | null,
    private readonly _action: WorkflowActionStorage,
    private readonly _actionState?: WorkflowActionStateStorage
  ) {}

  //#endregion

  //#region IAutocompleteDataConverter members

  toRecord(storage: KrAdditionalApproversSettings): IAutocompleteRecord {
    return {
      id: storage.role?.key,
      name: storage.role?.value ?? null,
      data: storage.getStorage()
    };
  }

  fromRecord(record: IAutocompleteRecord): KrAdditionalApproversSettings {
    const storage = new KrAdditionalApproversSettings(this._action, this._actionState);
    storage.role = { key: record.id as string, value: record.name };
    storage.mainApproverRowId = this._getCurrentMainApproverRowId();

    return storage;
  }

  //#endregion
}
