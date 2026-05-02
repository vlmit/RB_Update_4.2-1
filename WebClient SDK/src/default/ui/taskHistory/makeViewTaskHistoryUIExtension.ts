import { TaskHistoryViewDataProvider as TaskHistoryViewDataProvider } from './taskHistoryViewDataProvider';
import { ViewControlFilterableQuickSearchViewModel } from 'tessa/ui/cards/controls/viewControl/contents/viewControlFilterableQuickSearchViewModel';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { tryGetFromSettings, MenuAction, showViewModelDialog } from 'tessa/ui';
import {
  ViewControlBaseButtonViewModel,
  ViewControlBlockMenuContext,
  ViewControlMultiSelectButtonViewModel,
  ViewControlPagingViewModel,
  ViewControlRowMenuContext,
  ViewControlTableGridViewModel,
  ViewControlToolbarItem,
  ViewControlViewModel
} from 'tessa/ui/cards/controls';
import { ValidationResult } from 'tessa/platform/validation';
import {
  ITableBlockViewModel,
  ITableRowViewModel,
  MultiSelectButtonViewModel,
  QuickSearchViewModel
} from 'tessa/ui/views/content';
import { LocalizationManager } from 'tessa/localization';
import { getTaskHistoryTooltip, TaskHistoryItemInfo } from 'tessa/ui/cards/tasks';
import { ViewMetadataSealed } from 'tessa/views/metadata';
import { TaskHistoryDetailsDialog } from 'tessa/ui/cards/components/forms';
import { Visibility } from 'tessa/platform';
import { runInAction } from 'mobx';
import { extension, localize } from '@tessa/application';
import { CardHelper, ICardTypeExtensionContext, CardTypeExtensionTypes } from '@tessa/platform';
import { ClipboardHelper } from 'tessa/ui/clipboard';

@extension()
export class MakeViewTaskHistoryUIExtension extends CardUIExtension {
  private _disposes: Array<Function | null> = [];

  public async initializing(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.MakeViewTaskHistory,
      context.card,
      context.model.generalMetadata,
      this.executeInitializingAction,
      context
    );

    context.validationResult.add(result);
  }

  public async initialized(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.MakeViewTaskHistory,
      context.card,
      context.model.generalMetadata,
      this.executeInitializedAction,
      context
    );

    context.validationResult.add(result);
  }

  public finalized(): void {
    for (const dispose of this._disposes) {
      if (dispose) {
        dispose();
      }
    }
    this._disposes.length = 0;
  }

  executeInitializingAction = async (typeContext: ICardTypeExtensionContext): Promise<void> => {
    const context = typeContext.externalContext as ICardUIExtensionContext;
    const settings = typeContext.settings;
    const viewControlAlias = tryGetFromSettings<string>(settings, 'ViewControlAlias');
    if (!viewControlAlias) {
      return;
    }

    context.model.controlInitializers.push(async control => {
      if (control instanceof ViewControlViewModel) {
        if (control.name === viewControlAlias) {
          const defaultDataProvider = control.dataProvider;
          if (!defaultDataProvider) {
            throw new Error(
              `Control ViewModel with Name='${viewControlAlias}' has not default data provider.`
            );
          }
          const dataProvider = new TaskHistoryViewDataProvider(defaultDataProvider);
          control.dataProvider = dataProvider;
          control.multiSelect = false;
          this._disposes.push(control.onRefreshing.addWithDispose(() => dataProvider.resetCache()));
        }
      }
    });
  };

  executeInitializedAction = async (typeContext: ICardTypeExtensionContext): Promise<void> => {
    const context = typeContext.externalContext as ICardUIExtensionContext;
    const settings = typeContext.settings;
    const viewControlAlias = tryGetFromSettings<string>(settings, 'ViewControlAlias');
    if (!viewControlAlias) {
      return;
    }

    const taskHistoryView = context.model.controls.get(viewControlAlias) as ViewControlViewModel;
    if (!taskHistoryView) {
      context.validationResult.add(
        ValidationResult.fromText(`Control ViewModel with Name='${viewControlAlias}' not found.`)
      );
      return;
    }

    const viewMetadata = taskHistoryView.viewMetadata!;

    const collapseGroups = tryGetFromSettings<boolean>(settings, 'CollapseGroups', false);

    const leftRowColumns = tryGetFromSettings<string>(settings, 'LeftRowColumns', '').split(' ');
    const rightRowColumns = tryGetFromSettings<string>(settings, 'RightRowColumns', '').split(' ');
    const bottomColumns = tryGetFromSettings<string>(settings, 'BottomColumns', '').split(' ');

    if (taskHistoryView.table) {
      const table = taskHistoryView.table;

      const defaultRowAction = table.createRowAction;
      table.createRowAction = opt => {
        const row = defaultRowAction(opt);
        row.toolTip = getTaskHistoryTooltip(
          this.getTaskHistoryItemInfo(
            viewMetadata,
            leftRowColumns,
            rightRowColumns,
            bottomColumns,
            row.data
          )
        );
        row.onMouseDown = e => {
          if (e.button === 1) {
            e.preventDefault();
            showViewModelDialog(
              this.getTaskHistoryItemInfo(
                viewMetadata,
                leftRowColumns,
                rightRowColumns,
                bottomColumns,
                row.data
              ),
              TaskHistoryDetailsDialog
            );
          }
        };

        row.isToggled = !collapseGroups;

        return row;
      };

      table.rowContextMenuGenerators.push(ctx => {
        ctx.menuActions.push(
          MenuAction.create({
            type: 'normal',
            name: 'Copy',
            caption: '$UI_Common_Copy',
            action: () => this.copyToClipboard(ctx.row)
          }),
          MenuAction.create({
            type: 'normal',
            name: 'ShowDetails',
            caption: '$UI_Cards_TaskHistory_ShowDetails',
            action: () =>
              showViewModelDialog(
                this.getTaskHistoryItemInfo(
                  viewMetadata,
                  leftRowColumns,
                  rightRowColumns,
                  bottomColumns,
                  ctx.row.data
                ),
                TaskHistoryDetailsDialog
              )
          })
        );
        if (ctx.tableGrid.rows.length > 0) {
          ctx.menuActions.push(...this.createGroupMenu(ctx));
        }
      });

      table.blockContextMenuGenerators.push(ctx => {
        ctx.menuActions.push(...this.createGroupMenu(ctx));
      });

      // ctrl + c
      this._disposes.push(
        table.keyDown.addWithDispose(e => {
          const currentItem = e.control.rows.find(x => x.isSelected);
          if (!currentItem) {
            return;
          }

          const { event } = e;
          const code = event.keyCode || event.charCode;
          if (code === 67 && event.ctrlKey) {
            this.copyToClipboard(currentItem);
          }
        })
      );
    }

    const paging = taskHistoryView.bottomItems.find(
      x => x.content instanceof ViewControlPagingViewModel
    );
    if (paging) {
      (paging.content as ViewControlPagingViewModel).rowCountVisibility = Visibility.Collapsed;
    }

    const quickSearchIndex = taskHistoryView.bottomItems.findIndex(
      x => x.content instanceof QuickSearchViewModel
    );
    if (quickSearchIndex > -1) {
      taskHistoryView.bottomItems.splice(quickSearchIndex, 1);
    }

    const multiSelect = taskHistoryView.bottomItems.find(
      x => x.content instanceof MultiSelectButtonViewModel
    );
    if (multiSelect) {
      (multiSelect.content as ViewControlMultiSelectButtonViewModel).visibility =
        Visibility.Collapsed;
    }

    const quickSearch = new ViewControlFilterableQuickSearchViewModel(taskHistoryView);
    quickSearch.initialize();
    taskHistoryView.bottomItems.splice(
      0,
      0,
      new ViewControlToolbarItem(quickSearch, 'right', {
        align: 'end',
        stretch: true,
        isPermanent: true
      })
    );

    const expandAllButton = new ViewControlBaseButtonViewModel(taskHistoryView);
    expandAllButton.initialize();
    expandAllButton.icon = 'm-expand-all';
    expandAllButton.caption = localize('$UI_Common_ExpandAll');
    expandAllButton.captionPosition = 'after';
    expandAllButton.showCaption = true;
    expandAllButton.tooltip = localize('$UI_Controls_Views_ExpandAll');
    expandAllButton.theme = 'transparent';
    expandAllButton.type = 'small';
    expandAllButton.onClick = () => {
      const table = taskHistoryView.table;
      if (!table) {
        return;
      }
      runInAction(() => {
        for (const block of table.blocks) {
          block.isToggled = true;
        }
        for (const row of table.rows) {
          row.isToggled = true;
        }
      });
    };
    taskHistoryView.bottomItems.push(new ViewControlToolbarItem(expandAllButton, 'right'));

    const collapseAllButton = new ViewControlBaseButtonViewModel(taskHistoryView);
    collapseAllButton.initialize();
    collapseAllButton.icon = 'm-fold-all';
    collapseAllButton.caption = localize('$UI_Common_CollapseAll');
    collapseAllButton.captionPosition = 'after';
    collapseAllButton.showCaption = true;
    collapseAllButton.tooltip = localize('$UI_Controls_Views_CollapseAll');
    collapseAllButton.theme = 'transparent';
    collapseAllButton.type = 'small';
    collapseAllButton.onClick = () => {
      const table = taskHistoryView.table;
      if (!table) {
        return;
      }
      runInAction(() => {
        for (const block of table.blocks) {
          block.isToggled = false;
        }
        for (const row of table.rows) {
          row.isToggled = false;
        }
      });
    };
    taskHistoryView.bottomItems.push(new ViewControlToolbarItem(collapseAllButton, 'right'));
  };

  private getTaskHistoryItemInfo(
    metadata: ViewMetadataSealed,
    leftColumns: string[],
    rightColumns: string[],
    bottomColumns: string[],
    row: ReadonlyMap<string, any>
  ): TaskHistoryItemInfo {
    return {
      leftItems: leftColumns.map(column => ({
        caption: metadata.columns.get(column)!.caption ?? '',
        data: row.get(column)
      })),
      rightItems: rightColumns.map(column => ({
        caption: metadata.columns.get(column)!.caption ?? '',
        data: row.get(column)
      })),
      bottomItems: bottomColumns.map(column => ({
        caption: metadata.columns.get(column)!.caption ?? '',
        data: row.get(column)
      }))
    };
  }

  private copyToClipboard(row: ITableRowViewModel) {
    let sb = '';
    for (const column of row.grid.columns.filter(x => x.visibility)) {
      if (sb) {
        sb += '\n';
      }

      sb += `${LocalizationManager.instance.localize(column.header)}: ${
        row.getByName(column.columnName)!.convertedValue
      }`;
    }

    ClipboardHelper.copyToClipboard(sb);
  }

  private createGroupMenu(
    ctx: ViewControlRowMenuContext | ViewControlBlockMenuContext
  ): ReadonlyArray<MenuAction> {
    const block = (ctx as ViewControlBlockMenuContext).block;
    const row = (ctx as ViewControlRowMenuContext).row;
    const table = ctx.tableGrid;
    return [
      MenuAction.create({
        type: 'normal',
        name: 'ExpandGroups',
        caption: '$UI_Cards_TaskHistory_ExpandGroups',
        action: () => {
          runInAction(() => {
            if (block) {
              this.toggleBlock(table, block, true);
            }
            if (row) {
              this.toggleRow(table, row, true);
            }
          });
        }
      }),
      MenuAction.create({
        type: 'normal',
        name: 'CollapseGroups',
        caption: '$UI_Cards_TaskHistory_CollapseGroups',
        action: () => {
          runInAction(() => {
            if (block) {
              this.toggleBlock(table, block, false);
            }
            if (row) {
              this.toggleRow(table, row, false);
            }
          });
        }
      })
    ];
  }

  private toggleRow(
    table: ViewControlTableGridViewModel,
    row: ITableRowViewModel,
    isToggled: boolean
  ) {
    row.isToggled = isToggled;
    for (const childRow of table.rows.filter(x => x.parentRowId === row.id)) {
      this.toggleRow(table, childRow, isToggled);
    }
  }

  private toggleBlock(
    table: ViewControlTableGridViewModel,
    block: ITableBlockViewModel,
    isToggled: boolean
  ) {
    block.isToggled = isToggled;
    for (const childBlock of table.blocks.filter(x => x.parentBlockId === block.id)) {
      this.toggleBlock(table, childBlock, isToggled);
    }
    for (const childRow of table.rows.filter(x => x.blockId === block.id)) {
      this.toggleRow(table, childRow, isToggled);
    }
  }
}
