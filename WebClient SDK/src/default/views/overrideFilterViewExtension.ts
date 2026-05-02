import {
  QuickSearchBlockViewModel,
  QuickSearchViewModel,
  TableGridViewModel
} from 'tessa/ui/views/content';
import { IWorkplaceViewComponent, StandardViewComponentContentItemFactory } from 'tessa/ui/views';
import Platform, { PlatformSize } from 'common/platform';

import { AdvancedFilterViewDialogManager } from './advancedFilterViewDialogManager';
import { CustomOpenFilterDialogButtonViewModel } from './customFilterButtonViewModel';
import { FilterViewDialogDescriptorRegistry } from './filterViewDialogDescriptorRegistry';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { extension } from '@tessa/application';

/**
 * Расширение, переопределяющее диалог фильтрации представления.
 */
@extension()
export class OverrideFilterViewExtension extends WorkplaceViewComponentExtension {
  //#region base overrides

  getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.OverrideFilterViewExtension';
  }

  initialized(model: IWorkplaceViewComponent): void {
    const descriptor = FilterViewDialogDescriptorRegistry.instance.tryGet(model.id);
    const table = model.content.get(StandardViewComponentContentItemFactory.Table);

    if (table instanceof TableGridViewModel && descriptor) {
      model.onFiltering.add(params => {
        if (
          params.dialogOptions?.popoverProps ||
          (params.dialogOptions?.filterByColumn && Platform.size < PlatformSize.sm)
        ) {
          return;
        }
        params.filter = async () => {
          const customFilterDialogButton = new CustomOpenFilterDialogButtonViewModel(
            async cc =>
              await AdvancedFilterViewDialogManager.instance.open(descriptor, cc.parameters),
            table.viewComponent
          );

          await customFilterDialogButton.openFilterDialog();
        };
      });
    }
  }

  initialize(model: IWorkplaceViewComponent): void {
    const descriptor = FilterViewDialogDescriptorRegistry.instance.tryGet(model.id);

    if (!descriptor) {
      return;
    }

    // Замена стандартного диалога настройки параметров фильтрации,
    // вызываемого при нажатии на кнопку фильтрации внутри блока.
    model.contentFactories.set(
      StandardViewComponentContentItemFactory.QuickSearch,
      c =>
        new QuickSearchBlockViewModel(
          c,
          new QuickSearchViewModel(c),
          new CustomOpenFilterDialogButtonViewModel(
            async cc =>
              await AdvancedFilterViewDialogManager.instance.open(descriptor, cc.parameters),
            c
          )
        )
    );
  }

  //#endregion
}
