import { IDictionary } from '@tessa/core';
import { localize } from '@tessa/application';
import {
  IViewMetadata,
  SchemeType,
  SortColumn,
  ViewColumnMetadata,
  ViewMetadata
} from '@tessa/platform';
import { TasksWidgetViewHelper } from './tasksWidgetViewHelper';

/** Метаданные представления "Мои задания" для виджета {@link TasksWidget}. */
export class TasksWidgetViewMetadata extends ViewMetadata {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TasksWidgetViewMetadata}.
   * @param metadata Метаданные исходного представления.
   */
  constructor(metadata: IViewMetadata) {
    super();

    const serialized = metadata.serializeToStorage();
    const deserialized = this.deserializeFromStorage(serialized);

    this.initialize(deserialized);
  }

  //#endregion

  //#region protected methods

  protected initialize(metadata: IViewMetadata): void {
    TasksWidgetViewMetadata.hideEntities(metadata.columns);
    TasksWidgetViewMetadata.hideEntities(metadata.parameters);

    TasksWidgetViewMetadata.addAuthorColumnMetadata(metadata);
    TasksWidgetViewMetadata.addInfoColumnMetadata(metadata);
    TasksWidgetViewMetadata.addPerformerColumnMetadata(metadata);
    TasksWidgetViewMetadata.addCompletionColumnMetadata(metadata);

    metadata.defaultSortColumns = [new SortColumn(TasksWidgetViewHelper.CompletionColumnName)];
  }

  protected static addAuthorColumnMetadata(metadata: IViewMetadata): void {
    const column = new ViewColumnMetadata();
    column.alias = TasksWidgetViewHelper.AuthorColumnName;
    column.caption = localize('$Dashboard_Widget_Tasks_View_Author');
    column.schemeType = SchemeType.NullableString;
    column.localizable = true;
    metadata.columns.set(column.alias, column);
  }

  protected static addInfoColumnMetadata(metadata: IViewMetadata): void {
    const column = new ViewColumnMetadata();
    column.alias = TasksWidgetViewHelper.InfoColumnName;
    column.caption = localize('$Dashboard_Widget_Tasks_View_Info');
    column.schemeType = SchemeType.NullableString;
    column.localizable = true;
    column.disableGrouping = true;
    column.maxLength = 300;
    metadata.columns.set(column.alias, column);
  }

  protected static addPerformerColumnMetadata(metadata: IViewMetadata): void {
    const column = new ViewColumnMetadata();
    column.alias = TasksWidgetViewHelper.PerformerColumnName;
    column.caption = localize('$Dashboard_Widget_Tasks_View_Performer');
    column.schemeType = SchemeType.NullableString;
    column.localizable = true;
    metadata.columns.set(column.alias, column);
  }

  protected static addCompletionColumnMetadata(metadata: IViewMetadata): void {
    const column = new ViewColumnMetadata();
    column.alias = TasksWidgetViewHelper.CompletionColumnName;
    column.caption = localize('$Dashboard_Widget_Tasks_View_Completion');
    column.schemeType = SchemeType.NullableString;
    column.localizable = true;
    column.appearance = TasksWidgetViewHelper.AppearanceColumnName;
    const plannedColumn = metadata.columns.get(TasksWidgetViewHelper.PlannedDateColumnName);
    plannedColumn && (column.sortBy = plannedColumn.sortBy);
    metadata.columns.set(column.alias, column);
  }

  //#endregion

  //#region private methods

  private static hideEntities(entities: IDictionary<{ hidden: boolean }>) {
    for (const entity of entities.values()) {
      entity.hidden = true;
    }
  }

  //#endregion
}
