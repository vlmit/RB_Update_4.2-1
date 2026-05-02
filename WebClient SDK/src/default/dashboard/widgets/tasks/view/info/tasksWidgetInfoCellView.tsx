import { observer } from 'mobx-react-lite';
import { TasksWidgetInfoCellViewModel } from './tasksWidgetInfoCellViewModel';
import './tasksWidgetInfoCellStyle.scss';

/** Компонент ячейки "Карточка и задание" в представлении "Мои задания" для виджета {@link TasksWidget}. */
export const TasksWidgetInfoCellView = observer<{
  viewModel: TasksWidgetInfoCellViewModel;
}>(function TasksWidgetInfoCellView({ viewModel }) {
  const { taskType, cardType, cardInfo, taskInfo, onDoubleClick } = viewModel;

  return (
    <div className="info-table-cell">
      <span className="cell-data-task">{taskType}</span>
      <div className="cell-data-card">
        <span className="cell-data-card-type">{cardType}</span>
        {cardInfo && (
          <span className="cell-data-card-info" onClick={onDoubleClick}>
            {cardInfo}
          </span>
        )}
      </div>
      {taskInfo && <span className="cell-data-details">{taskInfo}</span>}
    </div>
  );
});
