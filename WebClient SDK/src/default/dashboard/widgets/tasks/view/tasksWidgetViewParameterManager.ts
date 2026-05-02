import {
  IViewMetadata,
  ViewCriteriaOperator,
  ViewCriteriaOperators,
  ViewCriteriaValue,
  ViewRequestCriteria,
  ViewRequestParameter,
  ViewRequestParameterBuilder
} from '@tessa/platform';
import { Primitive } from '@tessa/core';
import { injectable } from '@tessa/application';
import { IViewParameters } from 'tessa/ui/views/parameters';
import { ITasksWidgetViewParameterManager } from '../tasksTypes';

/** Менеджер для управления параметрами виджета {@link TasksWidget}. */
@injectable()
export class TasksWidgetViewParameterManager implements ITasksWidgetViewParameterManager {
  //#region ITasksWidgetViewParameterManager

  generateParameters(
    viewMetadata: IViewMetadata,
    operator: ViewCriteriaOperator,
    parameters: ReadonlyMap<string, string>
  ): ViewRequestParameter[] {
    const requestParameters: ViewRequestParameter[] = [];

    for (const [key, value] of parameters) {
      const parameterMetadata = viewMetadata.parameters.tryGet(key);
      if (parameterMetadata) {
        requestParameters.push(
          new ViewRequestParameterBuilder()
            .withMetadata(parameterMetadata)
            .addCriteria(operator, value, value)
            .readOnly(true)
            .asRequestParameter()
        );
      }
    }

    return requestParameters;
  }

  modifyParameter(
    viewParameters: IViewParameters,
    operator: ViewCriteriaOperator,
    alias: string,
    value: Primitive | null,
    include: boolean
  ): void {
    if (include) {
      // 1. Поиск параметра среди текущих
      const parameter = viewParameters.parameters.find(p => p.name === alias);
      if (parameter) {
        // 2. Поиск критерия, у которого значение совпадает с необходимым
        const criteria = this.getCriteria(value, parameter);
        if (!criteria) {
          // 3. Если критерий отсутствует, то необходимо его добавить в параметр
          this.addCriteria(value, parameter);
          // 4. Замена обновлённого параметра
          this.replaceParameter(viewParameters, parameter);
        }

        return;
      }

      const metadata = viewParameters.metadata.find(m => m.alias === alias);
      if (!metadata) {
        return;
      }

      // 2. Если параметр отсутствует, то она добавляется
      viewParameters.addParameters(
        new ViewRequestParameterBuilder()
          .withMetadata(metadata)
          .addCriteria(operator, `${value}`, value)
          .readOnly(true)
          .asRequestParameter()
      );

      return;
    }

    // 1. Поиск параметра среди текущих
    const parameter = viewParameters.parameters.find(p => p.name === alias);
    if (!parameter) {
      return;
    }

    // 2. Поиск критерия, у которого значение совпадает с необходимым
    const criteria = this.getCriteria(value, parameter);
    if (!criteria) {
      return;
    }

    // 3. Удаление значения критерия из параметра из параметра
    this.removeCriteria(criteria, parameter);

    parameter.criteriaValues.length > 0
      ? // 4. Замена обновлённого параметра
        this.replaceParameter(viewParameters, parameter)
      : // 3. Если критерий один, то необходимо удалить весь параметр
        viewParameters.removeParameters(parameter);
  }

  //#endregion

  //#region private methods

  private removeCriteria(criteria: ViewRequestCriteria, parameter: ViewRequestParameter): void {
    const index = parameter.criteriaValues.findIndex(c => c === criteria);
    index >= 0 && parameter.criteriaValues.splice(index, 1);
  }

  private addCriteria(value: Primitive | null, parameter: ViewRequestParameter): void {
    const criteriaValue = new ViewCriteriaValue(`${value}`, value, true);
    const criteriaOperator = ViewCriteriaOperators.EqualsTo.name;
    const criteria = new ViewRequestCriteria(criteriaOperator, true, [criteriaValue]);
    parameter.criteriaValues.push(criteria);
  }

  private getCriteria(
    value: Primitive | null,
    parameter: ViewRequestParameter
  ): ViewRequestCriteria | null {
    for (const criteria of parameter.criteriaValues) {
      for (const criteriaValue of criteria.values) {
        if (criteriaValue.value === value) {
          return criteria;
        }
      }
    }

    return null;
  }

  private replaceParameter(viewParameters: IViewParameters, parameter: ViewRequestParameter): void {
    // Альтернативный вариант - выполнение в одной транзации runInAction.
    let disposer: VoidFunction | undefined | null = null;
    try {
      disposer = viewParameters.suspendChangesNotification();
      viewParameters.removeParameters(parameter);
      disposer();
      disposer = null;
      viewParameters.addParameters(parameter);
    } finally {
      disposer?.();
    }
  }

  //#endregion
}
