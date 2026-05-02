import {
  FieldType,
  IStorage,
  IStorageArray,
  StorageArray,
  StorageValueFactory,
  TypedField
} from '@tessa/core';
import { WorkflowActionStorage } from 'tessa/ui/workflow/chunk/models/workflowActionStorage';
import { WorkflowActionWithSettingsBase } from 'tessa/ui/workflow/chunk/models/workflowActionWithSettingsBase';
import { WorkflowActionSettingsStorageBase } from 'tessa/ui/workflow/chunk/models/workflowActionSettingsStorageBase';
import { undoredo } from '@tessa/platform';
import { WorkflowActionSettingsEditorViewModelBase } from 'tessa/ui/workflow/chunk/editors/workflowActionSettingsEditorViewModelBase';
import {
  IPropertyGridDataProvider,
  PropertyGridBuilder,
  PropertyGridBuilderInstance
} from 'tessa/ui/propertyGrid';
import { WorkflowPropertyGridBindingHelper } from 'tessa/ui/workflow/chunk/editors/workflowPropertyGridBindingHelper';
import { WorkflowEngineUIHelper } from 'tessa/ui/workflow/workflowEngineUIHelper';
import { WorkflowPropertyGridBuilder } from 'tessa/ui/workflow/propertyGrid/workflowPropertyGridBuilder';
import { WorkflowHistoryMessageInfo } from 'tessa/ui/workflow/chunk/layout/workflowTypes';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import {
  AutocompleteDataViewContext,
  AutocompleteMode,
  IAutocompleteDataConverter,
  IAutocompleteRecord
} from 'ui/autocomplete';
import { AutocompleteKeyValuePairBindingDataConverter } from 'tessa/ui/workflow/chunk/editors/autocompleteKeyValuePairBindingDataConverter';
import { WorkflowActionStateStorage } from 'tessa/ui/workflow/chunk/models/workflowActionStateStorage';

class WorkflowParametrizedTestAutocompleteMultipleSettings extends WorkflowActionSettingsStorageBase<WorkflowParametrizedTestAutocompleteMultipleSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly rowIdKey = 'RowID';

  /** @category Static Keys */
  static readonly valueKey = 'Value';

  /** @category Static Keys */
  static readonly valueIdKey = 'ID';

  /** @category Static Keys */
  static readonly valueNameKey = 'Name';

  //#endregion

  //#region props

  get rowId(): string {
    return this.getValue(WorkflowParametrizedTestAutocompleteMultipleSettings.rowIdKey);
  }
  set rowId(value: string) {
    this.setField(
      WorkflowParametrizedTestAutocompleteMultipleSettings.rowIdKey,
      value,
      FieldType.Guid
    );
  }

  get value(): IReadOnlyKeyValuePair<string, string | null> | null {
    const storage = this.tryGetSubObject(
      WorkflowParametrizedTestAutocompleteMultipleSettings.valueKey
    );

    if (!storage) {
      return null;
    }

    return {
      key: storage.getValue(WorkflowParametrizedTestAutocompleteMultipleSettings.valueIdKey),
      value: storage.tryGetValue(WorkflowParametrizedTestAutocompleteMultipleSettings.valueNameKey)
    };
  }
  @undoredo()
  set value(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.set(
      WorkflowParametrizedTestAutocompleteMultipleSettings.valueKey,
      value
        ? {
            [WorkflowParametrizedTestAutocompleteMultipleSettings.valueIdKey]:
              TypedField.createGuid(value.key),
            [WorkflowParametrizedTestAutocompleteMultipleSettings.valueNameKey]: TypedField.create(
              value.value,
              FieldType.String
            )
          }
        : null
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(
    storage: IStorage
  ): WorkflowParametrizedTestAutocompleteMultipleSettings {
    return new WorkflowParametrizedTestAutocompleteMultipleSettings(
      this.action,
      this.actionState,
      storage
    );
  }

  protected override initialize(): void {
    this.init(
      WorkflowParametrizedTestAutocompleteMultipleSettings.rowIdKey,
      TypedField.createNewGuid()
    );
  }

  //#endregion
}

class WorkflowParametrizedControlTestActionSettings extends WorkflowActionSettingsStorageBase<WorkflowParametrizedControlTestActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly weTestActionSectionName = 'WeTestAction';

  /** @category Static Keys */
  static readonly textNormalKey = 'TextNormal';

  /** @category Static Keys */
  static readonly textNormalDisabledKey = 'TextNormalDisabled';

  /** @category Static Keys */
  static readonly textVisibilityFalseKey = 'TextVisibilityFalse';

  /** @category Static Keys */
  static readonly textRequiredKey = 'TextRequired';

  /** @category Static Keys */
  static readonly boolNormalKey = 'BoolNormal';

  /** @category Static Keys */
  static readonly intNormalKey = 'IntNormal';

  /** @category Static Keys */
  static readonly autocompleteKey = 'Autocomplete';

  /** @category Static Keys */
  static readonly autocompleteIdKey = 'ID';

  /** @category Static Keys */
  static readonly autocompleteCaptionKey = 'Caption';

  /** @category Static Keys */
  static readonly autocompleteMultipleSectionName = 'AutocompleteMultiple';

  //#endregion

  //#region props

  get textNormal(): string | null {
    return (
      this.tryGetSubObject(
        WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
      )?.tryGetValue(WorkflowParametrizedControlTestActionSettings.textNormalKey) ?? null
    );
  }
  @undoredo()
  set textNormal(value: string | null) {
    this.getSubObject(
      WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
    ).setField(
      WorkflowParametrizedControlTestActionSettings.textNormalKey,
      value,
      FieldType.String
    );
  }

  get textNormalDisabled(): string | null {
    return (
      this.tryGetSubObject(
        WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
      )?.tryGetValue(WorkflowParametrizedControlTestActionSettings.textNormalDisabledKey) ?? null
    );
  }
  @undoredo()
  set textNormalDisabled(value: string | null) {
    this.getSubObject(
      WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
    ).setField(
      WorkflowParametrizedControlTestActionSettings.textNormalDisabledKey,
      value,
      FieldType.String
    );
  }

  get textVisibilityFalse(): string | null {
    return (
      this.tryGetSubObject(
        WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
      )?.tryGetValue(WorkflowParametrizedControlTestActionSettings.textVisibilityFalseKey) ?? null
    );
  }
  @undoredo()
  set textVisibilityFalse(value: string | null) {
    this.getSubObject(
      WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
    ).setField(
      WorkflowParametrizedControlTestActionSettings.textVisibilityFalseKey,
      value,
      FieldType.String
    );
  }

  get textRequired(): string | null {
    return (
      this.tryGetSubObject(
        WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
      )?.tryGetValue(WorkflowParametrizedControlTestActionSettings.textRequiredKey) ?? null
    );
  }
  @undoredo()
  set textRequired(value: string | null) {
    this.getSubObject(
      WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
    ).setField(
      WorkflowParametrizedControlTestActionSettings.textRequiredKey,
      value,
      FieldType.String
    );
  }

  get boolNormal(): boolean | string {
    return (
      this.tryGetSubObject(
        WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
      )?.tryGetValue(WorkflowParametrizedControlTestActionSettings.boolNormalKey) ?? false
    );
  }
  @undoredo()
  set boolNormal(value: boolean | string) {
    this.getSubObject(
      WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
    ).setField(
      WorkflowParametrizedControlTestActionSettings.boolNormalKey,
      value,
      typeof value === 'boolean' ? FieldType.Boolean : FieldType.String
    );
  }

  get intNormal(): number | string {
    return (
      this.tryGetSubObject(
        WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
      )?.tryGetValue(WorkflowParametrizedControlTestActionSettings.intNormalKey) ?? 0
    );
  }
  @undoredo()
  set intNormal(value: number | string) {
    this.getSubObject(
      WorkflowParametrizedControlTestActionSettings.weTestActionSectionName
    ).setField(
      WorkflowParametrizedControlTestActionSettings.intNormalKey,
      value,
      typeof value === 'number' ? FieldType.Int : FieldType.String
    );
  }

  get autocomplete(): IReadOnlyKeyValuePair<string, string | null> | null {
    const autocompleteStorage = this.tryGetSubObject(
      WorkflowParametrizedControlTestActionSettings.autocompleteKey
    );

    if (!autocompleteStorage) {
      return null;
    }

    return {
      key: autocompleteStorage.getValue(
        WorkflowParametrizedControlTestActionSettings.autocompleteIdKey
      ),
      value: autocompleteStorage.tryGetValue(
        WorkflowParametrizedControlTestActionSettings.autocompleteCaptionKey
      )
    };
  }
  @undoredo()
  set autocomplete(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.set(
      WorkflowParametrizedControlTestActionSettings.autocompleteKey,
      value
        ? {
            [WorkflowParametrizedControlTestActionSettings.autocompleteIdKey]:
              TypedField.createGuid(value.key),
            [WorkflowParametrizedControlTestActionSettings.autocompleteCaptionKey]:
              TypedField.create(value.value, FieldType.String)
          }
        : null
    );
  }

  get autocompleteMultiple():
    | StorageArray<WorkflowParametrizedTestAutocompleteMultipleSettings>
    | string
    | null {
    return this.tryGetBindingArray(
      WorkflowParametrizedControlTestActionSettings.autocompleteMultipleSectionName,
      this.autocompleteMultipleFactory
    );
  }
  @undoredo.array(true)
  set autocompleteMultiple(
    value: Array<WorkflowParametrizedTestAutocompleteMultipleSettings> | string
  ) {
    this.setBindingArray(
      WorkflowParametrizedControlTestActionSettings.autocompleteMultipleSectionName,
      value
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(
    storage: IStorage
  ): WorkflowParametrizedControlTestActionSettings {
    return new WorkflowParametrizedControlTestActionSettings(
      this.action,
      this.actionState,
      storage
    );
  }

  //#endregion

  autocompleteMultipleFactory(
    array: IStorageArray = []
  ): StorageArray<WorkflowParametrizedTestAutocompleteMultipleSettings> {
    return StorageArray.from(array, {
      factory: new StorageValueFactory(
        () => ({}),
        s =>
          new WorkflowParametrizedTestAutocompleteMultipleSettings(this.action, this.actionState, s)
      ),
      observable: true
    });
  }
}

class WorkflowParametrizedTestAutocompleteMultipleAutocompleteDataConverter implements IAutocompleteDataConverter<WorkflowParametrizedTestAutocompleteMultipleSettings> {
  //#region ctor

  constructor(
    private readonly _action: WorkflowActionStorage,
    private readonly _actionState?: WorkflowActionStateStorage
  ) {}

  //#endregion

  //#region IAutocompleteDataConverter members

  toRecord(storage: WorkflowParametrizedTestAutocompleteMultipleSettings): IAutocompleteRecord {
    return {
      id: storage.value!.key,
      name: storage.value!.value
    };
  }

  fromRecord(record: IAutocompleteRecord): WorkflowParametrizedTestAutocompleteMultipleSettings {
    const storage = new WorkflowParametrizedTestAutocompleteMultipleSettings(
      this._action,
      this._actionState
    );
    storage.value = { key: record.id as string, value: record.name };

    return storage;
  }

  //#endregion
}

export class WorkflowParametrizedControlTestActionStorage extends WorkflowActionWithSettingsBase {
  //#region base overrides

  protected override settingsFactory(
    action: WorkflowActionStorage,
    actionState: WorkflowActionStateStorage | undefined,
    storage: IStorage
  ): WorkflowParametrizedControlTestActionSettings {
    return new WorkflowParametrizedControlTestActionSettings(action, actionState, storage);
  }

  /** Маппинг полей настроек действия к имени контрола. */
  override get fieldToCaptionMap(): Map<string, WorkflowHistoryMessageInfo> {
    return new Map<string, WorkflowHistoryMessageInfo>();
  }

  //#endregion
}

export class WorkflowParametrizedTestControlActionEditorViewModel extends WorkflowActionSettingsEditorViewModelBase {
  //#region properties

  protected get actionSettings(): WorkflowParametrizedControlTestActionSettings {
    return <WorkflowParametrizedControlTestActionSettings>this._action.settings;
  }

  //#endregion

  //#region methods

  protected override createPropertyGridCore(
    builder: PropertyGridBuilderInstance,
    dataProvider: IPropertyGridDataProvider
  ): void {
    builder
      .startGroup('Normal')
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textNormal'),
            caption: 'PARAMETRIZED Text. Normal',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .startGroup('Disabled')
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textNormalDisabled'),
          caption: 'ORIGINAL Text. Normal. Disabled (settings)',
          disabled: true,
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
          }
        })
      )
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textNormalDisabled'),
          caption: 'ORIGINAL Text. Normal. Disabled (onInitialized)',
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

            property.disabled = true;
          }
        })
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textNormalDisabled'),
            caption: 'PARAMETRIZED Text. Normal. Disabled (settings)',
            disabled: true,
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textNormalDisabled'),
            caption: 'PARAMETRIZED Text. Normal. Disabled (onInitialized)',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

              property.disabled = true;
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .startGroup('PropertyVisibility')
      .addTextProperty({
        data: dataProvider,
        alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
        caption: 'Для информации. Все контролы в этой группе должны быть скрыты.'
      })
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
          caption: 'ORIGINAL Text. Normal. PropertyVisibility=false (settings)',
          visibility: false,
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
          }
        })
      )
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
          caption: 'ORIGINAL Text. Normal. PropertyVisibility=false (onInitialized)',
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

            property.visibility = false;
          }
        })
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
            caption: 'PARAMETRIZED Text. Normal. PropertyVisibility=false (settings)',
            visibility: false,
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
            caption: 'PARAMETRIZED Text. Normal. PropertyVisibility=false (onInitialized)',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

              property.visibility = false;
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .startGroup('ControlVisibility')
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
          caption: 'ORIGINAL Text. Normal. ControlVisibility=false (onInitialized)',
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

            property.control.visibility = false;
          }
        })
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textVisibilityFalse'),
            caption: 'PARAMETRIZED Text. Normal. ControlVisibility=false (onInitialized)',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

              property.control.visibility = false;
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .startGroup('PropertyRequired')
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textRequired'),
          caption: 'ORIGINAL Text. Normal. Required (settings)',
          required: true,
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
          }
        })
      )
      .addProperty(
        PropertyGridBuilder.createTextProperty({
          data: dataProvider,
          alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textRequired'),
          caption: 'ORIGINAL Text. Normal. Required (onInitialized)',
          onInitialized: async property => {
            property.control.minRows = 1;
            property.control.maxRows = 5;
            property.control.notifyMode = 'instant';
            property.control.syncCommittedChanges = true;
            WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

            property.required = true;
          }
        })
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textRequired'),
            caption: 'PARAMETRIZED Text. Normal. Required (settings)',
            required: true,
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createTextProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('textRequired'),
            caption: 'PARAMETRIZED Text. Normal. Required (onInitialized)',
            onInitialized: async property => {
              property.control.minRows = 1;
              property.control.maxRows = 5;
              property.control.notifyMode = 'instant';
              property.control.syncCommittedChanges = true;
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);

              property.required = true;
            }
          },
          this.bindingContext,
          { type: FieldType.String }
        )
      )
      .startGroup('BooleanProperty')
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createBooleanProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('boolNormal'),
            caption: 'PARAMETRIZED Boolean. Normal',
            onInitialized: async property => {
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
            }
          },
          this.bindingContext,
          { type: FieldType.Boolean }
        )
      )
      .startGroup('IntProperty')
      .addProperty(
        WorkflowPropertyGridBuilder.createParametrizedProperty(
          PropertyGridBuilder.createNumericProperty,
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('intNormal'),
            caption: 'PARAMETRIZED Int. Normal',
            controlType: 'Integer',
            onInitialized: async property => {
              WorkflowEngineUIHelper.batchHistoryWhenFocused(property);
            }
          },
          this.bindingContext,
          { type: FieldType.Int }
        )
      )
      .startGroup('AutocompleteProperty')
      .addProperty(
        WorkflowPropertyGridBuilder.createAutocompleteParametrizedProperty(
          {
            data: dataProvider,
            alias: WorkflowPropertyGridBindingHelper.getSettingsPropertyName('autocomplete'),
            caption: 'PARAMETRIZED Autocomplete. Normal. Single',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'CompletionOptions',
              idColumn: 'OptionID',
              nameColumn: 'OptionCaption',
              parameterAlias: 'Caption',
              unique: true
            }),
            dataConverter: new AutocompleteKeyValuePairBindingDataConverter(
              WorkflowParametrizedControlTestActionSettings.autocompleteIdKey,
              WorkflowParametrizedControlTestActionSettings.autocompleteCaptionKey
            ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.menu.openAction.isCollapsed = true;
              property.control.context = this.uiContext;
            }
          },
          this.bindingContext,
          {
            type: {
              isMultiple: false,
              viewReference: 'Role',
              viewAlias: 'Roles',
              idColumn: 'RoleID',
              nameColumn: 'RoleName',
              additionalColumns: ['ID', 'Name'],
              paramTypeObjectName: 'Role'
            }
          }
        )
      )
      .addProperty(
        WorkflowPropertyGridBuilder.createAutocompleteParametrizedProperty(
          {
            data: dataProvider,
            alias:
              WorkflowPropertyGridBindingHelper.getSettingsPropertyName('autocompleteMultiple'),
            caption: 'PARAMETRIZED Autocomplete. Normal. Multiple',
            dataContext: new AutocompleteDataViewContext({
              viewAlias: 'CompletionOptions',
              idColumn: 'OptionID',
              nameColumn: 'OptionCaption',
              parameterAlias: 'Caption',
              unique: true,
              multiple: true
            }),
            dataConverter:
              new WorkflowParametrizedTestAutocompleteMultipleAutocompleteDataConverter(
                this._action.action
              ),
            onInitialized: async property => {
              property.control.mode = AutocompleteMode.NonDroppable;
              property.control.menu.openAction.isCollapsed = true;
              property.control.context = this.uiContext;
            }
          },
          this.bindingContext,
          {
            type: {
              isMultiple: true,
              viewAlias: 'Roles',
              viewReference: 'Role',
              idColumn: 'ID',
              nameColumn: 'Name',
              paramTypeObjectName: 'Role',
              complexPrefix: 'Role'
            }
          }
        )
      );
  }

  //#endregion
}
