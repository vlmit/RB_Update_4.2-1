import { TableViewSorting } from './tableViewSorting';
import { tryGetFromSettings } from 'tessa/ui';
import {
  ViewControlInitializationContext,
  ViewControlInitializationStrategy
} from 'tessa/ui/cards/controls';
import { Paging } from 'tessa/views';
import { ViewMetadata, ViewSelectionMode } from 'tessa/views/metadata';

export class CardTableViewInitializationStrategy extends ViewControlInitializationStrategy {
  initializeMetadata(context: ViewControlInitializationContext): void {
    const viewMetadata = new ViewMetadata();
    viewMetadata.alias = context.controlViewModel.name ?? '';
    viewMetadata.paging = context.controlViewModel.pagingMode;
    viewMetadata.pageLimit = context.controlViewModel.pageLimit;
    context.controlViewModel.viewMetadata = viewMetadata;
  }

  initializeDataProvider(_context: ViewControlInitializationContext): void {}

  initializeSorting(context: ViewControlInitializationContext): void {
    context.controlViewModel.sorting = new TableViewSorting();
  }

  initializeContent(context: ViewControlInitializationContext): void {
    super.initializeContent(context);

    if (context.controlViewModel.table) {
      context.controlViewModel.table.overlays.length = 0;
    }
    context.controlViewModel.bottomItems.length = 0;
  }

  initializePaging(context: ViewControlInitializationContext): void {
    super.initializePaging(context);
    // В стандартной стратегии инициализации страницы подсчитываются, если задан PageCountSubset.
    // В случае генерации данных представления на клиенте подсчет страниц зависит от настройки пейджинга.
    context.controlViewModel.pageCountStatus =
      context.controlViewModel.pagingMode === Paging.Always;
  }

  initializeTable(context: ViewControlInitializationContext): void {
    const viewModel = context.controlViewModel;
    viewModel.firstRowSelection = tryGetFromSettings<boolean>(
      context.controlSettings,
      'FirstRowSelection',
      true
    );
    viewModel.selectionMode = ViewSelectionMode.Row;
  }
}
