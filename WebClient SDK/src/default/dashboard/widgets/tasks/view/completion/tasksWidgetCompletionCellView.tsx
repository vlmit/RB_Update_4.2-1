import { observer } from 'mobx-react-lite';
import { TasksWidgetCompletionCellViewModel } from './tasksWidgetCompletionCellViewModel';
import './tasksWidgetCompletionCellStyle.scss';

/** Компонент ячейки "Завершить до" в представлении "Мои задания" для виджета {@link TasksWidget}. */
export const TasksWidgetCompletionCellView = observer<{
  viewModel: TasksWidgetCompletionCellViewModel;
}>(function TasksWidgetCompletionCellView({ viewModel }) {
  return (
    <div className="completion-table-cell">
      <span className="cell-data-planned">{viewModel.planned}</span>
      <span className="cell-data-completion">{viewModel.completion}</span>
    </div>
  );
});
