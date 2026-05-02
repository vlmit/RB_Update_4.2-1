import { Guid, IValidationResultBuilder, ValidationSequence } from '@tessa/core';
import { CardValidationKeys, ICardMetadata } from '@tessa/platform';
import { ActionCompletionOption } from './actionCompletionOption';
import { KrActionOptionRowSettingsBase } from './models/krActionOptionRowSettingsBase';
import { KrTaskOptionRowSettingsBase } from './models/krTaskOptionRowSettingsBase';

/**
 * Общие методы и типы, используемые при создании действий.
 * @helper
 */
export namespace KrWorkflowActionsHelper {
  /**
   * Инициализирует указанный список строк, содержащий параметры обработки вариантов завершения действий, заданными вариантами завершения.
   * @param completionOptions Коллекция, содержащая информацию о вариантах завершения.
   * @param rows Инициализируемый список строк.
   * @param optionIds Перечисление идентификаторов вариантов завершения.
   * @param actionOptionRowFactory Функция, создающая новую строку, добавляемую в {@link rows}.
   */
  export function initializeActionCompletionOptions<T extends KrActionOptionRowSettingsBase<T>>(
    completionOptions: ReadonlyMap<string, ActionCompletionOption>,
    rows: T[],
    optionIds: string[],
    actionOptionRowFactory: () => T
  ): void {
    if (completionOptions.size === 0 || rows.length > 0) {
      return;
    }

    let index = 0;
    for (const optionId of optionIds) {
      const coInfo = completionOptions.get(optionId);

      if (!coInfo) {
        continue;
      }

      const actionOptionRow = actionOptionRowFactory();
      actionOptionRow.actionOption = { key: coInfo.id, value: coInfo.caption };
      actionOptionRow.order = index++;

      rows.push(actionOptionRow);
    }
  }

  /**
   * Инициализирует указанный список строк, содержащий параметры обработки вариантов завершения заданий указанных типов.
   * @param cardMetadata Метаинформация, необходимая для использования типов карточек совместно с пакетом карточек.
   * @param rows Инициализируемый список строк.
   * @param taskTypeIds Идентификаторы типов заданий.
   * @param validationResult Результат выполнения.
   * @param optionRowFactory Функция, создающая новую строку, добавляемую в {@link rows}.
   * @param validationResultObject Название объекта используемое в сообщении валидации.
   * @param isSetTaskTypeInfo Значение `true`, если необходимо задать информацию о типе задания, иначе - `false`.
   */
  export function initializeTaskCompletionOptions<T extends KrTaskOptionRowSettingsBase<T>>(
    cardMetadata: ICardMetadata,
    rows: T[],
    taskTypeIds: string[],
    validationResult: IValidationResultBuilder,
    optionRowFactory: () => T,
    validationResultObject: string | null = null,
    isSetTaskTypeInfo = false
  ): void {
    if (rows.length > 0 || taskTypeIds.length === 0) {
      return;
    }

    const metaCompletionOptions = cardMetadata.enumerations.completionOptions;

    let index = 0;
    for (const taskTypeId of taskTypeIds) {
      const taskType = cardMetadata.cardTypes.getCardTypeById(taskTypeId);

      if (!taskType) {
        ValidationSequence.begin(validationResult)
          .setObjectName(validationResultObject)
          .error(CardValidationKeys.UnknownCardType, taskTypeId)
          .end();

        return;
      }

      for (const completionOption of taskType.completionOptions) {
        const optionId = completionOption.typeId;
        const metaCompletionOption = metaCompletionOptions.find(i => Guid.equals(i.id, optionId));

        if (!metaCompletionOption) {
          ValidationSequence.begin(validationResult)
            .setObjectName(validationResultObject)
            .error(CardValidationKeys.UnknownCompletionOption, optionId)
            .end();

          return;
        }

        const optionRow = optionRowFactory();
        optionRow.option = { key: optionId, value: metaCompletionOption.caption };

        if (isSetTaskTypeInfo) {
          optionRow.taskType = {
            key: taskTypeId,
            value: taskType.name,
            caption: taskType.caption
          };
        }

        optionRow.order = index++;

        rows.push(optionRow);
      }
    }
  }
}
