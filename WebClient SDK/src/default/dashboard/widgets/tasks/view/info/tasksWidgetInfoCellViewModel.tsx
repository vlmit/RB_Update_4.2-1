import { localize } from '@tessa/application';
import { ITableCellViewModelCreateOptions, TableCellViewModel } from 'tessa/ui/views/content';
import { TasksWidgetInfoCellView } from './tasksWidgetInfoCellView';
import { StringHelper } from '@tessa/core';

/**
 * Модель представления для ячейки "Карточка и задание"
 * в представлении "Мои задания" для виджета {@link TasksWidget}.
 */
export class TasksWidgetInfoCellViewModel extends TableCellViewModel {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TasksWidgetInfoCellViewModel}.
   * @param options Параметры для создания контекста инициализации ячейки.
   */
  constructor(
    options: ITableCellViewModelCreateOptions & {
      taskType: string;
      cardType: string;
      cardName?: string | null;
      cardSubject?: string | null;
      taskInfo?: string | null;
    }
  ) {
    super(options);

    this.taskType = localize(options.taskType);
    this.cardType = localize(options.cardType);
    this.taskInfo = StringHelper.limit(localize(options.taskInfo), 150);

    const name = options.cardName;
    const subject = StringHelper.limit(localize(options.cardSubject), 150);
    this.cardInfo = [name, subject].filter(Boolean).join(' ') || null;

    this._getContent = () => <TasksWidgetInfoCellView viewModel={this} />;
  }

  //#endregion

  //#region properties

  /** Тип задания. */
  readonly taskType: string;

  /** Тип карточки. */
  readonly cardType: string;

  /** Краткое описание карточки. */
  readonly cardInfo?: string | null;

  /** Информация о задании. */
  readonly taskInfo?: string | null;

  //#endregion
}
