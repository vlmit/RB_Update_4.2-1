import { ViewCriteriaOperators } from '@tessa/platform';

import { FilterViewDialogDescriptor } from '../views/filterViewDialogDescriptor';

/**
 * Предоставляет объекты типа {@link FilterViewDialogDescriptor}.
 */
export namespace AbFilterViewDialogDescriptors {
  /**
   * Дескриптор, описывающий специальный диалог с параметрами фильтрации представления РМ Администратор/Тестирование/Автомобили.
   */
  export const cars: FilterViewDialogDescriptor = {
    dialogName: 'AbCarViewParameters',
    parametersMapping: [
      { alias: 'CarName', valueSectionName: 'Parameters', valueFieldName: 'Name' },
      {
        alias: 'CarMaxSpeed',
        valueSectionName: 'Parameters',
        valueFieldName: 'MaxSpeed',
        criteriaOperator: ViewCriteriaOperators.EqualsTo
      },
      {
        alias: 'Driver',
        valueSectionName: 'Parameters',
        valueFieldName: 'DriverID',
        displayValueSectionName: 'Parameters',
        displayValueFieldName: 'DriverName'
      },
      {
        alias: 'CarReleaseDateFrom',
        valueSectionName: 'Parameters',
        valueFieldName: 'ReleaseDateFrom'
      },
      { alias: 'CarReleaseDateTo', valueSectionName: 'Parameters', valueFieldName: 'ReleaseDateTo' }
    ]
  };
}
