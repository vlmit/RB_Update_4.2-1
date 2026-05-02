import { runInAction } from 'mobx';
import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { AutoCompleteEntryViewModel, AutoCompleteTableViewModel } from 'tessa/ui/cards/controls';

/**
 * Показываем выбранный автокомплит всегда в диалоговом окне для определенного типа карточки.
 *
 * Результат работы расширения:
 * Проверяем, что значения типа карточки и алиаса контрола те, что нам нужны и если так,
 * то добавляем флаг, который указывает автокомплиту работать в режиме диалога.
 */
@extension()
export class DialogModeAutocompleteUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // находим контрол с автокомплитом
    const driverNameControl = context.model.controls.get(
      'DriverName2'
    ) as AutoCompleteEntryViewModel;
    if (driverNameControl) {
      // runInAction нужен в том случае если мы пытаемся поменять observable поле,
      // для которого уже созданы observers
      runInAction(() => {
        driverNameControl.alwaysShowInDialog = true;
      });
    }

    // находим контрол с автокомплитом
    const ownersListControl = context.model.controls.get('Owners') as AutoCompleteTableViewModel;
    if (ownersListControl) {
      // runInAction нужен в том случае если мы пытаемся поменять observable поле,
      // для которого уже созданы observers
      runInAction(() => {
        ownersListControl.alwaysShowInDialog = true;
      });
    }
  }
}
