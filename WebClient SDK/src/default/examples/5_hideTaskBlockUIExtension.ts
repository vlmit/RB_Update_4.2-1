import { Guid } from '@tessa/core';
import { CardSection } from '@tessa/platform';
import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { Visibility } from 'tessa/platform';
import { TaskWorkspaceState, TaskViewModel } from 'tessa/ui/cards/tasks';

/**
 * Скрывать\показывать какой-либо элемент управления задания в зависимости от данных задания
 * для определенного типа карточки.
 *
 * Результат работы расширения:
 * При использовании типового процесса отправки задач для тестовой карточки "Автомобиль"
 * на этапе отправки проверяем поле "Комментарий" на наличие данных:
 * - Если поле "Комментарий" содержит текст, то скрываем контрол "Вернуть на роль".
 * - Если комментария нет, то показываем контрол "Вернуть на роль".
 */
@extension()
export class HideTaskBlockUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся получить все задания
    let tasks = context.model.tryGetTasks();
    if (!tasks) {
      return;
    }

    // фильтруем задания с типом WfResolutionProject
    tasks = tasks.filter(x =>
      Guid.equals(x.taskModel.cardType.id, 'c989d91f-7ddd-455c-ae16-3bb380132ba8')
    );
    if (tasks.length === 0) {
      return;
    }

    for (const task of tasks) {
      // получаем секцию из карточки задания
      const section = task.taskModel.card.sections.tryGet('WfResolutions');
      if (!section) {
        continue;
      }

      // скрываем контрол
      // подписываемся на изменения формы в задании
      this.disposeList.add(
        task.modifyWorkspace(e => HideTaskBlockUIExtension.modifyWorkspace(section, e.task))!
      );
      // подписываемся на изменение поля в секции
      this.disposeList.add(
        section.fields.fieldChanged.add(e => {
          if (e.fieldName === 'Comment') {
            HideTaskBlockUIExtension.modifyWorkspace(section, task);
          }
        })
      );
    }
  }

  override async finalized(_context: ICardUIExtensionContext): Promise<void> {
    this.disposeList.dispose();
  }

  private static modifyWorkspace(section: CardSection, task: TaskViewModel) {
    if (
      task.taskWorkspace.state === TaskWorkspaceState.OptionForm &&
      task.taskWorkspace.form &&
      task.taskWorkspace.form.name === 'SendToPerformer'
    ) {
      // пытаемся найти поле "Комментарий" секции с заданием
      const commentExists = !!section.fields.get('Comment');
      // пытаемся найти контрол "Вернуть на роль"
      const control = task.taskModel.controls.get('Controller_WithControl');
      if (control) {
        // скрываем контрол "Вернуть на роль" при наличии поля "Комментарий"
        control.controlVisibility = commentExists ? Visibility.Collapsed : Visibility.Visible;
      }
    }
  }
}
