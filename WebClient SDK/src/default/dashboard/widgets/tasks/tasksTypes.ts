import { Primitive } from '@tessa/core';
import { IViewMetadata, ViewCriteriaOperator, ViewRequestParameter } from '@tessa/platform';
import { IViewParameters } from 'tessa/ui/views/parameters/viewParameters';

/** Менеджер для управления параметрами виджета {@link TasksWidget}. */
export interface ITasksWidgetViewParameterManager {
  /**
   * Генерирует параметры запроса на основе метаданных представления, оператора и переданных параметров.
   * @param viewMetadata Метаданные представления, которые определяют, как должны быть сгенерированы параметры.
   * @param operator Условный оператор для применения в критериях выбора.
   * @param parameters Строковые параметры представления в виде словаря, на основе которых генерируются параметры запроса.
   * @returns Коллекция сгенерированных параметров запроса.
   */
  generateParameters(
    viewMetadata: IViewMetadata,
    operator: ViewCriteriaOperator,
    parameters: ReadonlyMap<string, string>
  ): ViewRequestParameter[];

  /**
   * Добавляет/исключает критерий отбора для параметр запроса к представлению.
   * @param viewParameters Существующие параметры представления, которые подлежат изменению.
   * @param operator Условный оператор для применения в критериях выбора.
   * @param alias Псевдоним параметра для его идентификации.
   * @param value Значение параметра (может быть `null`).
   * @param include Указывает, следует ли включить параметр в итоговый запрос.
   */
  modifyParameter(
    viewParameters: IViewParameters,
    operator: ViewCriteriaOperator,
    alias: string,
    value: Primitive | null,
    include: boolean
  ): void;
}
