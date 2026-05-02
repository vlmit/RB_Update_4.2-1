import { computed } from 'mobx';
import { inject, localize } from '@tessa/application';
import { extension } from '@tessa/application/lib/extensions';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import {
  ViewButtonViewModel,
  ContentPlaceArea,
  ContentPlaceOrder,
  TableGridViewModelBase
} from 'tessa/ui/views/content';
import { ShowMode, WorkplaceViewSettingsDataProvider } from 'tessa/views/workplaces';
import { Visibility } from 'tessa/platform';
import { WorkspaceStorage } from 'tessa/workspaceStorage';
import { Alert, AlertPositionPoint, AlertType } from 'tessa/ui/alerts';
import { DashboardWorkspace, WidgetTemplatesWorkspace } from 'tessa/ui/dashboard';
import { IViewWidgetClipboardFilter$, IViewWidgetDashboardClipboard$ } from '../widgetInjects';
import { IViewWidgetClipboardFilter, IViewWidgetDashboardClipboard } from './viewWidgetTypes';

@extension({ name: 'CopyViewSettingsToClipboardViewExtension' })
export class CopyViewSettingsToClipboardViewExtension extends WorkplaceViewComponentExtension {
  constructor(
    @inject(IViewWidgetDashboardClipboard$)
    private readonly _clipboard: IViewWidgetDashboardClipboard,
    @inject(IViewWidgetClipboardFilter$, {
      optional: true,
      multi: true
    })
    private readonly _filters: IViewWidgetClipboardFilter[] | null
  ) {
    super();
  }

  getExtensionName(): string {
    return 'CopyViewSettingsToClipboardViewExtension';
  }

  shouldExecute(model: IWorkplaceViewComponent): boolean {
    return (
      model.workplace.showMode !== ShowMode.SelectionMode &&
      (this._filters?.every(filter => filter.filter(model)) ?? true)
    );
  }

  initialize(model: IWorkplaceViewComponent): void {
    model.groupContainer.createGroup('CopyViewSettingsToClipboard', {
      order: ContentPlaceOrder.BeforeAll
    });
    model.contentFactories.set('CopyViewSettingsToClipboardViewExtension', c => {
      return new CopySettingsButtonViewModel(c, this._clipboard, 'CopyViewSettingsToClipboard');
    });
  }
}

class CopySettingsButtonViewModel extends ViewButtonViewModel {
  //#region fields

  private readonly _settingsProvider: WorkplaceViewSettingsDataProvider;

  //#endregion

  //#region ctor

  constructor(
    viewComponent: IWorkplaceViewComponent,
    clipboard: IViewWidgetDashboardClipboard,
    groupName: string,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.BeforeAll
  ) {
    super(viewComponent, area, order);

    this.icon = 'm-copy';
    this.type = 'small';
    this.theme = 'yellow';
    this.caption = '$Dashboard_Widget_View_CopyButton_Caption';
    this.captionPosition = 'after';
    this.tooltip = localize('$Dashboard_Widget_View_CopyButton_Tooltip');
    this.onClick = async () => {
      const viewMetadata = this.viewComponent.getViewMetadata(this.viewComponent);
      if (viewMetadata) {
        const parameters = this.viewComponent.parameters.parameters.filter(p => p.readOnly);
        const columnSettings = this._settingsProvider.getSettings();

        await clipboard.saveSettings({
          viewAlias: viewMetadata?.alias,
          viewCaption: viewMetadata.caption,
          parameters: parameters,
          columnSettings: columnSettings
        });

        Alert.show({
          text: localize('$Dashboard_Widget_View_CopyButton_Notification'),
          type: AlertType.Info,
          duration: 5000,
          closeable: true,
          position: {
            point: AlertPositionPoint.Center
          }
        });
      }
    };
    this._name = 'CopyViewSettingsButtonViewModel';
    this.setGroupName(groupName);

    this._settingsProvider = new WorkplaceViewSettingsDataProvider(viewComponent);
  }

  //#endregion

  //#region props

  @computed
  get visibility(): Visibility {
    const content = this.viewComponent.contentByArea.get(ContentPlaceArea.ContentPanel);
    // Не показываем для представлений с каким-то кастомным контентом вместо таблицы
    if (!content?.every(c => c instanceof TableGridViewModelBase)) {
      return Visibility.Collapsed;
    }

    const dashboardTab = WorkspaceStorage.instance.orderedStorage.find(
      w => w.id === DashboardWorkspace.id
    ) as DashboardWorkspace;

    const widgetTemplatesTab = WorkspaceStorage.instance.orderedStorage.find(
      w => w.id === WidgetTemplatesWorkspace.id
    ) as WidgetTemplatesWorkspace;

    return (dashboardTab?.initialized && dashboardTab.dashboard.editMode) || !!widgetTemplatesTab
      ? Visibility.Visible
      : Visibility.Collapsed;
  }

  //#endregion
}
