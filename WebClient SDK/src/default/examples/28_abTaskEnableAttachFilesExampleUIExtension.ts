import { extension } from '@tessa/application';
import { Guid } from '@tessa/core';
import { CardFileDeletionMode, CardFileFlags, CardRowState } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms/defaultFormTabWithTasksViewModel';

/**
 * При сохранении выбранного типа карточки добавленные/измененные/подписанные файлы файлового
 * контрола задания переносятся в основную карточку.
 *
 * Дополнительная информация по данному расширению находится в документации kb_106.md.
 *
 * Результат работы расширения:
 * Для карточки "Автомобиль":
 * - добавьте файловый контрол для задачи "Тестовое согласование"
 * - создайте задачу через тайл на левой панели - “Тестовое согласование”.
 * - возьмите её в работу, добавьте файл в файловый контрол.
 * - при сохранении карточки файлы из файлового контрола задания переместятся в основную карточку.
 */
@extension({ name: 'AbTaskEnableAttachFilesExampleUIExtension' })
export class AbTaskEnableAttachFilesExampleUIExtension extends CardUIExtension {
  override async saving(context: ICardUIExtensionContext): Promise<void> {
    // Если в форме с карточкой нет задач, то ничего не делаем.
    if (
      !(context.model.mainForm instanceof DefaultFormTabWithTasksViewModel) ||
      !context.model.mainForm.tasks.length
    ) {
      return;
    }

    // Перебираем файлы в карточках задач главной формы.
    for (const taskViewModel of context.model.mainForm.tasks) {
      await taskViewModel.taskModel.fileContainer.ensureAllContentModified();
      const filesCount = taskViewModel.taskModel.card.files.length;
      for (let i = 0; i < filesCount; i++) {
        const fileCard = taskViewModel.taskModel.card.files[i];
        // Отправляем в контейнер только измененные файлы, либо те, у которых была добавлена или снята электронная подпись.
        if (
          fileCard.hasChanges() ||
          fileCard.flags !== CardFileFlags.None ||
          fileCard.card.sections
            .tryGet('FileSignatures')
            ?.rows.some(x => x.state !== CardRowState.None)
        ) {
          const file = taskViewModel.taskModel.fileContainer.files.find(x =>
            Guid.equals(x.id, fileCard.rowId)
          );
          if (file) {
            // Добавляем файл в контейнер главной карточки, если он есть.
            await file.lastVersion.ensureContentDownloaded();
            await context.fileContainer.addFile(file);
          }

          // Добавляем файл в главную карточку.
          const newFileCard = context.storeRequest.card.files.add(fileCard);

          // Записываем в поле TaskID идентификатор текущей задачи.
          newFileCard.taskId = taskViewModel.id;

          // Указываем, что файл удаляется без восстановления
          newFileCard.deletionMode = CardFileDeletionMode.None;
        }
      }
    }
  }
}
