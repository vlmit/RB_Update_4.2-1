import { ViewControlInitializationContext } from 'tessa/ui/cards/controls/viewControl/viewControlInitializationContext';
import { ViewControlInitializationStrategy } from 'tessa/ui/cards/controls/viewControl/viewControlInitializationStrategy';
import { TasksWidgetViewDataProvider } from './tasksWidgetViewDataProvider';
import { TasksWidgetViewMetadata } from './tasksWidgetViewMetadata';

/** Стратегия инициализации представления "Мои задания" для виджета {@link TasksWidget}. */
export class TasksWidgetViewInitializationStrategy extends ViewControlInitializationStrategy {
  //#region base overrides

  override initializeMetadata(context: ViewControlInitializationContext): void {
    super.initializeMetadata(context);

    const viewMetadata = context.controlViewModel.viewMetadata;
    if (viewMetadata) {
      context.controlViewModel.viewMetadata = new TasksWidgetViewMetadata(viewMetadata);
    }
  }

  override initializeDataProvider(context: ViewControlInitializationContext): void {
    const view = this.getView(context.controlSettings);
    context.controlViewModel.dataProvider = view ? new TasksWidgetViewDataProvider(view) : null;
  }

  //#endregion
}
