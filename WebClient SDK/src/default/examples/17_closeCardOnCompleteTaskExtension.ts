import { FieldType, Guid, StorageHelper, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardStoreExtension, ICardStoreExtensionContext, CardTaskAction } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

/**
 * Закрывает карточку выбранного типа после завершения определенного типа задания.
 *
 * Результат работы расширения:
 * В карточке "Автомобиль":
 * - Создайте задачу через тайл на левой панели - “Тестовое согласование”.
 * - Затем возьмите её в работу, выберите один из существующих вариантов. При выборе варианта
 * задача завершится и карточка закроется.
 */
@extension()
export class CloseCardOnCompleteTaskStoreExtension extends CardStoreExtension {
  private _taskComplete = false;

  override async beforeRequest(context: ICardStoreExtensionContext): Promise<void> {
    const card = context.request.card;
    // если задачи в карточке отсутствуют, то ничего не делаем
    const tasks = card.tryGetTasks();
    if (!tasks) {
      return;
    }

    // проверяем, есть ли среди существующих задач "Тестовое согласование"
    this._taskComplete = tasks.some(
      x =>
        Guid.equals(x.typeId, '929e345c-acdf-41ea-acb6-6bb308de73ae') && // TestTask1
        x.action === CardTaskAction.Complete
    );
  }

  override async afterRequest(context: ICardStoreExtensionContext): Promise<void> {
    if (!context.requestIsSuccessful || !context.request || !this._taskComplete) {
      return;
    }

    // добавляем флажок о закрытии в хранилище
    context.request.info['.closeCardAfterStore'] = TypedField.createBoolean(true);
  }
}

@extension()
export class CloseCardOnCompleteTaskUIExtension extends CardUIExtension {
  override async reopening(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся получить дынные хранилища
    const request = context.getRequest;
    if (!request) {
      return;
    }

    // проверяем наличие флажка о закрытии в хранилище
    const close =
      StorageHelper.tryGetValue(request.info, '.closeCardAfterStore', FieldType.Boolean) ?? false;
    if (close) {
      const editor = context.uiContext.cardEditor;
      if (editor) {
        // закрываем карточку
        editor.closePending = true;
      }
    }
  }
}
