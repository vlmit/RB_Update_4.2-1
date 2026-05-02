import { Moment } from 'moment';
import { StringHelper } from '@tessa/core';
import { localize } from '@tessa/application';
import { hexToRgbA } from 'tessa/ui/uiHelper';
import { Themes } from 'tessa/ui/themes/themesHandler';
import { ITableCellViewModelCreateOptions, TableCellViewModel } from 'tessa/ui/views/content';
import { TasksWidgetCompletionCellView } from './tasksWidgetCompletionCellView';

/**
 * Модель представления для ячейки "Завершить до"
 * в представлении "Мои задания" для виджета {@link TasksWidget}.
 */
export class TasksWidgetCompletionCellViewModel extends TableCellViewModel {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TasksWidgetCompletionCellViewModel}.
   * @param options Параметры для создания контекста инициализации ячейки.
   */
  constructor(options: ITableCellViewModelCreateOptions & { planned: Moment; completion: string }) {
    super(options);

    this.planned = options.planned.format('D MMMM');
    this.completion = TasksWidgetCompletionCellViewModel.calculateCompletion(options.completion);

    if (!this.style.background) {
      const variable = 'var(--dashboard-widget-tasks-valid-background)';
      const hex = Themes.current?.resolveVariables(variable) ?? '';
      this.style = Object.assign(this.style, { background: hexToRgbA(hex, true) });
    }

    this._getContent = () => <TasksWidgetCompletionCellView viewModel={this} />;
  }

  //#endregion

  //#region properties

  readonly planned: string;

  readonly completion: string;

  //#endregion

  //#region private methods

  private static isNumeric(value: string): boolean {
    try {
      const parsed = parseFloat(value);
      return !isNaN(parsed) && isFinite(parsed);
    } catch {
      return false;
    }
  }

  private static calculateCompletion(value: string): string {
    return TasksWidgetCompletionCellViewModel.isNumeric(value.split(' ')[0])
      ? `${StringHelper.capitalize(localize('$Dashboard_Widget_Tasks_View_More'), true)} ${value}`
      : StringHelper.capitalize(value, true);
  }

  //#endregion
}
