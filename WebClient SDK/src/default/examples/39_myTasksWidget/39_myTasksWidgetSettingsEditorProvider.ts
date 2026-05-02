import { localize } from '@tessa/application';
import {
  AutocompleteDataViewContext,
  AutocompleteMode,
  IAutocompleteDataConverter,
  IAutocompleteRecord
} from 'ui/autocomplete';
import { PropertyGridBuilderInstance, PropertyGridDataProvider } from 'tessa/ui/propertyGrid';
import { ButtonWidgetSettingsEditorProvider } from '../../dashboard/widgets/button/buttonWidgetSettingsEditorProvider';
import { MyTasksWidget } from './39_myTasksWidget';
import { MyTasksWidgetSettings } from './39_myTasksWidgetSettings';
import { MyTasksWidgetType } from './39_myTasksWidgetType';
import { TaskTypeReference } from './39_taskTypeReference';

export class MyTasksWidgetSettingsEditorProvider extends ButtonWidgetSettingsEditorProvider<MyTasksWidgetSettings> {
  //#region ctor

  constructor(widget: MyTasksWidget) {
    super(widget);
  }

  //#endregion

  //#region methods

  protected override modifyBuilder(
    builder: PropertyGridBuilderInstance,
    data: PropertyGridDataProvider
  ): PropertyGridBuilderInstance {
    const taskTypeContext = new AutocompleteDataViewContext({
      unique: true,
      viewAlias: 'TaskTypes',
      idColumn: 'TypeID',
      nameColumn: 'TypeCaption',
      parameterAlias: 'Caption',
      multiple: false
    });
    const converter = new TaskTypeDataConverter();

    const propertiesToHide = ['caption', 'captionHidden', 'icon'];

    return builder
      .addAutocompleteProperty({
        alias: 'taskType',
        caption: 'Тип задания',
        data: data,
        required: true,
        dataContext: taskTypeContext,
        dataConverter: converter,
        onInitialized: async ({ control }) => {
          control.mode = AutocompleteMode.NonDroppable;
          control.menu.openAction.isCollapsed = true;
        }
      })
      .onGridCreated(grid => {
        propertiesToHide.forEach(propertyName => {
          const property = grid.findProperty(propertyName);
          if (property) {
            property.visibility = false;
          }
        });

        grid.title = MyTasksWidgetType.title;
      });
  }

  //#endregion
}

class TaskTypeDataConverter implements IAutocompleteDataConverter<TaskTypeReference> {
  toRecord(value: TaskTypeReference): IAutocompleteRecord {
    return {
      id: value.id,
      name: localize(value.caption)
    };
  }
  fromRecord(record: IAutocompleteRecord): TaskTypeReference {
    const taskType = new TaskTypeReference();
    taskType.id = record.id as string;
    taskType.caption = record.name!;

    return taskType;
  }
}
