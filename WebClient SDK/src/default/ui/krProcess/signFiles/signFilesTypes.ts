import { ICardModel } from 'tessa/ui/cards';
import { TaskViewModel } from 'tessa/ui/cards/tasks';

export interface ISignFilesProvider {
  /**
   * Запускает процесс подписания файлов для карточки и задачи.
   * Возвращает true, если все файлы успешно подписаны, иначе false.
   * @param cardModel Модель карточки, для которой выполняется подписание.
   * @param taskViewModel Модель задания, связанного с подписанием.
   */
  signFilesAction(cardModel: ICardModel, taskViewModel: TaskViewModel): Promise<boolean>;
}
