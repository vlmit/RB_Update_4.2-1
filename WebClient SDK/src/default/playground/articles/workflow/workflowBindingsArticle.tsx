import { inject, injectable, localize } from '@tessa/application';
import { UIButton } from 'tessa/ui';
import { AsyncLazy, FieldType } from '@tessa/core';
import { Visibility } from 'tessa/platform';
import { PropertyGrid, PropertyGridBuilder, PropertyGridDataProvider } from 'tessa/ui/propertyGrid';
import { PropertyGridHelper } from 'tessa/ui/propertyGrid/propertyGridHelper';
import {
  AutocompleteMode,
  IAutocompleteDataConverter,
  IAutocompleteItem,
  IAutocompleteRecord
} from 'ui/autocomplete';
import { AutocompleteDataPlainContext } from 'ui/autocomplete/core/autocompleteDataPlainContext';
import { WorkflowBindingEditor } from 'tessa/ui/workflow/bindings/workflowBindingEditor';
import {
  BindingParamType,
  WorkflowBindingComplexType,
  WorkflowHashParameterComplexTypesCache$
} from 'tessa/ui/workflow';
import { WorkflowBindingContext } from 'tessa/ui/workflow/bindings/workflowBindingContext';
import {
  isWorkflowBindingComplexType,
  WorkflowBindingHelper
} from 'tessa/ui/workflow/bindings/workflowBindingHelper';
import { observable, runInAction } from 'mobx';
import { WorkflowBindingEditorViewModel } from 'tessa/ui/workflow/bindings/workflowBindingEditorViewModel';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { WorkflowHashParameterComplexTypesCache } from 'tessa/ui/workflow/workflowHashParameterComplexTypesCache';
import { WorkflowSettingsComplexTypesProvider } from 'tessa/ui/workflow/workflowHashEditor/workflowSettingsComplexTypesProvider';

function getParamTypeCaption(paramType: BindingParamType): string {
  if (isWorkflowBindingComplexType(paramType)) {
    const complexType = paramType as WorkflowBindingComplexType;
    return complexType.viewAlias + ' ' + (complexType.isMultiple ? 'LIST' : 'DICTIONARY');
  }
  switch (paramType) {
    case FieldType.String:
      return '$HashEditor_Types_String';
    case FieldType.Guid:
      return '$HashEditor_Types_Guid';
    case FieldType.DateTime:
      return '$HashEditor_Types_DateTime';
    case FieldType.Boolean:
      return '$HashEditor_Types_Boolean';
    case FieldType.Int:
      return '$HashEditor_Types_Integer';
    case FieldType.Double:
      return '$HashEditor_Types_Double';
    case FieldType.Decimal:
      return '$HashEditor_Types_Decimal';
    default:
      throw new Error('Unsupported');
  }
}

class AutocompleteParamTypeDataConverter implements IAutocompleteDataConverter<BindingParamType> {
  //#region ctor

  constructor(paramTypes: BindingParamType[]) {
    this._paramTypes = paramTypes;
  }

  //#endregion

  //#region fields

  private _paramTypes: BindingParamType[];

  //#endregion

  //#region IAutocompleteDataConverter

  toRecord(value: BindingParamType): IAutocompleteRecord {
    return {
      id: value,
      name: getParamTypeCaption(value)
    };
  }

  fromRecord(record: IAutocompleteRecord): BindingParamType {
    return this._paramTypes.find(x => x === record.id)!;
  }

  //#endregion
}

@injectable()
export class WorkflowBindingsArticle extends PlaygroundArticle {
  //#region fields

  @observable
  private _paramTypeButton: UIButton;

  @observable
  private _paramType: BindingParamType = FieldType.String;

  @observable
  private _binding: string = '';

  private _availableParamTypes: BindingParamType[] = [
    FieldType.String,
    FieldType.Guid,
    FieldType.DateTime,
    FieldType.Boolean,
    FieldType.Int,
    FieldType.Double,
    FieldType.Decimal,
    {
      isMultiple: false,
      viewReference: 'Type',
      viewAlias: 'TaskTypes',
      idColumn: 'ID',
      nameColumn: 'Caption',
      additionalColumns: ['Name'],
      paramTypeObjectName: 'TaskType'
    },
    {
      isMultiple: false,
      viewReference: 'Ref',
      viewAlias: 'DialogCardStoreModes',
      idColumn: 'ID',
      nameColumn: 'Name',
      paramTypeObjectName: 'DialogCardStoreMode'
    },
    {
      isMultiple: true,
      viewReference: 'SignalType',
      viewAlias: 'WorkflowSignalTypes',
      idColumn: 'ID',
      nameColumn: 'Name',
      refSection: 'WorkflowSignalTypes',
      complexPrefix: 'WorkflowSignalType'
    },
    {
      isMultiple: true,
      viewAlias: 'Roles',
      viewReference: 'Role',
      idColumn: 'ID',
      nameColumn: 'Name',
      refSection: 'Roles',
      paramTypeObjectName: 'Role',
      complexPrefix: 'Role'
    },
    {
      isMultiple: false,
      viewReference: 'Role',
      viewAlias: 'Roles',
      idColumn: 'ID',
      nameColumn: 'Name',
      refSection: 'Roles',
      paramTypeObjectName: 'Role'
    }
  ];

  //#endregion

  //#region ctors

  constructor(
    @inject(WorkflowHashParameterComplexTypesCache$, { lazy: true })
    protected readonly _workflowHashParameterComplexTypesCache: AsyncLazy<WorkflowHashParameterComplexTypesCache>
  ) {
    super();
  }

  //#endregion

  //#region props

  private get paramType(): BindingParamType {
    return this._paramType;
  }

  private set paramType(value: BindingParamType) {
    runInAction(() => (this._paramType = value));
  }

  private get binding(): string {
    return this._binding;
  }

  private set binding(value: string) {
    runInAction(() => (this._binding = value));
  }

  //#endregion

  //#region overrides
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Workflow/Bindings',
      description: 'Workflow bindings'
    };
  }

  override async initialize(): Promise<void> {
    await this.addWorkflowBindingsNode('WorkflowBindings');
  }

  //#endregion

  //#region add blocks methods

  async addWorkflowBindingsNode(caption: string, description?: string): Promise<void> {
    this._paramTypeButton = UIButton.create({
      name: 'paramType',
      type: 'small',
      theme: 'control',
      visibility: Visibility.Visible
    });
    const complexTypedProvider = new WorkflowSettingsComplexTypesProvider(
      await this._workflowHashParameterComplexTypesCache.getValue()
    );

    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        const dataProvider = new PropertyGridDataProvider(this, true);
        const propertyGrid = this.createParamTypeGrid(dataProvider);
        await propertyGrid.initialize();
        await PropertyGridHelper.showDialog(propertyGrid, { autoSizeHeight: true });

        const viewModel = new WorkflowBindingEditorViewModel({
          paramType: this.paramType,
          context: new WorkflowBindingContext({}, {}, {}, complexTypedProvider),
          binding: this.binding ? WorkflowBindingHelper.bindingPrefix + this.binding : ''
        });

        await viewModel.initialize();
        this.updateButtonCaption();
        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ width: '100%' })}>
          <WorkflowBindingEditor viewModel={viewModel} />
        </DemoForm>
      ),
      buttons: [this._paramTypeButton]
    });
  }

  private updateButtonCaption(): void {
    this._paramTypeButton.caption = 'Param Type: ' + localize(getParamTypeCaption(this.paramType));
  }

  private createParamTypeGrid(dataProvider: PropertyGridDataProvider): PropertyGrid {
    const items: IAutocompleteItem[] = this._availableParamTypes.map(x => ({
      id: x,
      name: getParamTypeCaption(x)
    }));
    const dataContext = new AutocompleteDataPlainContext({
      items: items
    });

    const grid = PropertyGridBuilder.create(dataProvider);

    grid
      .addAutocompleteProperty({
        data: dataProvider,
        alias: 'paramType',
        caption: '$WorkflowEngine_BindingEditor_BindingTypeLabel',
        dataContext: dataContext,
        dataConverter: new AutocompleteParamTypeDataConverter(this._availableParamTypes),
        onInitialized: async property => {
          property.control.mode = AutocompleteMode.Droppable;
          property.control.toolbar.buttons
            .find(p => p.name === 'Clear')
            ?.setVisibility(Visibility.Collapsed);
          property.control.notifyMode = 'instant';
          property.control.syncCommittedChanges = true;
        }
      })
      .addTextProperty({
        data: dataProvider,
        alias: 'binding',
        caption: '$HashEditor_SelectedParamCaption',
        placeholder: 'e.g. Card.PersonalRole.Roles.Name',
        onInitialized: async property => {
          property.control.notifyMode = 'instant';
          property.control.syncCommittedChanges = true;
        }
      });

    grid.onGridCreated(grid => {
      grid.toolbarVisibility = false;
    });

    return grid.build();
  }

  //#endregion
}
