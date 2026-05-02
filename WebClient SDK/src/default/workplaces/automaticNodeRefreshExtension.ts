import { reaction } from 'mobx';
import { TreeItemExtension } from 'tessa/ui/views/extensions';
import {
  ISubsetTreeItem,
  ITreeItem,
  isTreeItemVisibleInPath
} from 'tessa/ui/views/workplaces/tree';
import { IStorage } from 'tessa/platform/storage';
import { tryGetFromSettings } from 'tessa/ui';
import { IWorkplaceViewModel, IWorkplaceViewComponent } from 'tessa/ui/views';
import {
  extension,
  HttpHooksContext,
  HttpRequestHeaders,
  inject,
  ISessionHealth,
  ISessionHealth$
} from '@tessa/application';

@extension()
export class AutomaticNodeRefreshExtension extends TreeItemExtension {
  private _settings: AutomaticNodeRefreshSettings;

  private _timer: any | null;

  private _refreshPending = false;

  private _treeItem: ITreeItem;

  private _disposes: Function[] = [];

  //#region ctor

  constructor(@inject(ISessionHealth$) private readonly _sessionHealth: ISessionHealth) {
    super();
  }

  //#endregion

  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Workplaces.AutomaticNodeRefreshExtension';
  }

  public initialized(model: ITreeItem): void {
    this._treeItem = model;
    this._settings = new AutomaticNodeRefreshSettings(this.settingsStorage);
    this.subscribeToEvents(model);
    reaction(
      () => model.parent,
      parent => {
        this.unsubscribeFromEvents();
        if (!parent) {
          this.stopTimer();
        } else {
          this.subscribeToEvents(model);
          this.startTimer();
        }
      }
    );
    if (model.workplace && model.workplace.isActive) {
      // вкладка с рабочим местом активна на момент запуска приложения
      this.startTimer();
    }
  }

  private addIsExpandedListener(treeItem: ITreeItem) {
    this._disposes.push(
      reaction(
        () => treeItem.isExpanded,
        isExpanded => {
          if (isExpanded && isTreeItemVisibleInPath(treeItem)) {
            if (this._refreshPending) {
              this.updateByTimer(false);
            }
            this.startTimer();
          }
        }
      )
    );
  }

  private addIsSelectedListener(treeItem: ITreeItem) {
    this._disposes.push(
      reaction(
        () => treeItem.isSelected,
        isSelected => {
          if (isSelected) {
            if (this._refreshPending) {
              this.updateByTimer(false);
            }
            this.startTimer();
          }
        }
      )
    );
  }

  private subscribeToEvents(treeItem: ITreeItem) {
    this._disposes.push(
      reaction(
        () => treeItem.workplace.isActive,
        isActive => {
          if (isActive) {
            if (this._refreshPending) {
              this.updateByTimer(true);
            }
            this.startTimer();
          }
        }
      )
    );

    this._disposes.push(
      reaction(
        () => treeItem.isLoading,
        isLoading => {
          if (!isLoading && !this._timer) {
            this.stopTimer();
            this.startTimer();
            this._refreshPending = false;
          }
        }
      )
    );

    let currentNode = treeItem.parent;
    while (currentNode) {
      this.addIsExpandedListener(currentNode);
      currentNode = currentNode.parent;
    }

    this.addIsExpandedListener(treeItem);
    this.addIsSelectedListener(treeItem);

    this.subscribeToChildEvents(treeItem);
  }

  private subscribeToChildEvents(treeItem: ITreeItem) {
    this.subscribeToChildChangingEvents(treeItem, true);

    for (const child of treeItem.items) {
      this.subscribeToChildChangingEvents(child, false);

      this.addIsExpandedListener(child);
      this.addIsSelectedListener(child);

      const subsetTreeItem = child as ISubsetTreeItem;
      if (subsetTreeItem) {
        for (const subsetChild of subsetTreeItem.items) {
          this.addIsSelectedListener(subsetChild);
        }
      }
    }
  }

  private subscribeToChildChangingEvents(treeItem: ITreeItem, firstLevel: boolean) {
    this._disposes.push(
      reaction(
        () => treeItem.visibleItems,
        items => {
          let isExpanded = false;
          for (const child of items) {
            this.addIsSelectedListener(child);
            if (firstLevel) {
              this.addIsExpandedListener(child);

              const subsetTreeItem = child as ISubsetTreeItem;
              if (subsetTreeItem) {
                this.subscribeToChildChangingEvents(subsetTreeItem, false);
              }
            }

            isExpanded = isExpanded || child.isExpanded;
          }
          if (firstLevel && isExpanded) {
            if (this._refreshPending) {
              this.updateByTimer(false);
            }
            this.startTimer();
          }
        }
      )
    );
  }

  private unsubscribeFromEvents() {
    for (const dispose of this._disposes) {
      dispose();
    }
    this._disposes.length = 0;
  }

  private startTimer() {
    if (!this._timer) {
      this._timer = setTimeout(
        () => this.updateByTimer(false),
        this._settings.refreshInterval * 1000
      );
    }
  }

  private stopTimer() {
    if (this._timer) {
      clearTimeout(this._timer);
      this._timer = null;
    }
  }

  private async updateByTimer(skipUpdateTable: boolean) {
    this.stopTimer();
    if (await this.updateByTimerCore(skipUpdateTable)) {
      this.startTimer();
    }
  }

  private async updateByTimerCore(skipUpdateTable: boolean): Promise<boolean> {
    // Если задача не успела отработать или узел находится в процессе обновления, то просто выходим из задачи.
    // При сравнении использован коэффициент 0.98 (время уменьшено на 1/50), потому что обновление
    // treeItem.LastUpdateTime происходит через некоторое время после того, как предыдущее расширение отработало,
    // поэтому при проверке нужен доверительный интервал.
    if (
      this._treeItem.isLoading ||
      Date.now() - this._treeItem.lastUpdateTime < this._settings.refreshInterval * 1000 * 0.98 ||
      !this._sessionHealth.isHealthy
    ) {
      return true;
    }

    // Останавливаемся, если узел РМ не активно или узел не виден в дереве.
    if (!this._treeItem.workplace.isActive || !isTreeItemVisibleInPath(this._treeItem)) {
      this._refreshPending = true;
      return false;
    }

    // Останавливаемся, если не включена настройка "Обновлять всегда" и не выбран элемент с указанным расширением и он не раскрыт вместе с сабсетами.
    if (
      !this._settings.alwaysRefresh &&
      !this._treeItem.hasSelection() &&
      (!this._treeItem.isExpanded || !this._treeItem.items.some(i => i.isExpanded))
    ) {
      this._refreshPending = true;
      return false;
    }

    const scope = HttpHooksContext.create(
      new HttpHooksContext([
        {
          beforeRequest: async ctx =>
            ctx.request.headers.append(HttpRequestHeaders.RequestType, 'background')
        }
      ])
    );
    scope.run(async () => {
      await this._treeItem.refreshNode();
      if (this._treeItem.hasSelection()) {
        this.refreshTableContent(skipUpdateTable);
      }
    });

    return true;
  }

  private async refreshTableContent(skipUpdateTable: boolean): Promise<void> {
    this._refreshPending = false;
    if (this._settings.withContentDataRefreshing && !skipUpdateTable) {
      await this.refreshContent(this._treeItem.workplace);
    }
  }

  private async refreshContent(workplaceViewModel: IWorkplaceViewModel): Promise<void> {
    if (!workplaceViewModel) {
      return;
    }

    // обновляем содержимое (таблицы)
    const viewContext = workplaceViewModel.context.viewContext;
    if (viewContext) {
      // получаем верхнюю вью (от которой зависят остальные)
      let rootContext = viewContext;
      while (rootContext.parentContext) {
        rootContext = rootContext.parentContext;
      }
      const viewComponent = rootContext as IWorkplaceViewComponent;

      if (viewComponent.currentPage == undefined || viewComponent.currentPage === 1) {
        // либо вью не поддерживает пейджинг, либо страница и так первая, либо это какой-то кастом
        // если кастом, то надеемся, что он поддерживает RefreshCommand
        await rootContext.refreshView();
      } else {
        // Refresh будет автоматом при изменении номера страницы на первую
        viewComponent.currentPage = 1;
      }
    }
  }
}

class AutomaticNodeRefreshSettings {
  constructor(storage: IStorage) {
    this.refreshInterval = tryGetFromSettings(storage, 'RefreshInterval', 300);
    this.withContentDataRefreshing = tryGetFromSettings(storage, 'WithContentDataRefreshing', true);
    this.alwaysRefresh = tryGetFromSettings(storage, 'AlwaysRefresh', false);
  }

  public refreshInterval: number;

  public withContentDataRefreshing: boolean;

  public alwaysRefresh: boolean;
}
