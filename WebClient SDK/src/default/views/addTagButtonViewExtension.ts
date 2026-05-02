import moment from 'moment';
import { extension, inject, localize } from '@tessa/application';
import { ITagManager, ITagManager$, Tag } from '@tessa/platform';
import { userSession } from 'common/utility';
import { LocalizationManager } from 'tessa/localization';
import { MenuAction, getRgbaFromDecimal, showNotEmpty } from 'tessa/ui';
import { selectOrCreateTag } from 'tessa/ui/tags';
import {
  IViewContextMenuContext,
  IWorkplaceViewComponent,
  StandardViewComponentContentItemFactory
} from 'tessa/ui/views';
import {
  ViewButtonViewModel,
  ContentPlaceArea,
  ContentPlaceOrder,
  TableGridViewModelBase,
  ITableRowViewModel
} from 'tessa/ui/views/content';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { ViewColumnMetadataSealed, ViewReferenceMetadataSealed } from 'tessa/views/metadata';
import { GridRowTagViewModel } from 'tessa/ui/views/content/gridRowTagViewModel';

@extension()
export class AddTagButtonViewExtension extends WorkplaceViewComponentExtension {
  constructor(@inject(ITagManager$) private readonly _tagManager: ITagManager) {
    super();
  }

  getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.AddTagButtonViewExtension';
  }

  private table: TableGridViewModelBase | null;

  initialize(model: IWorkplaceViewComponent): void {
    model.contentFactories.set(
      'AddTagButtonViewExtension',
      c =>
        new AddTagButtonViewExtensionViewModel(
          c,
          this.table,
          (
            model: IWorkplaceViewComponent,
            table: TableGridViewModelBase<IWorkplaceViewComponent>
          ): Promise<void> => {
            return this.addTag(model, table);
          }
        )
    );

    const tableFactory = model.contentFactories.get(StandardViewComponentContentItemFactory.Table);
    if (!tableFactory) {
      return;
    }
    model.contentFactories.set(StandardViewComponentContentItemFactory.Table, c => {
      const table = tableFactory(c);
      if (table instanceof TableGridViewModelBase) {
        this.table = table;
      }
      return table;
    });

    model.contextMenuGenerators.push(ctx => {
      this.createAddTagMenuAction(ctx, model);
    });
  }

  //#region private methods

  private createAddTagMenuAction(ctx: IViewContextMenuContext, model: IWorkplaceViewComponent) {
    ctx.menuActions.push(
      new MenuAction(
        `AddTag`,
        `${LocalizationManager.instance.localize('$Tags_ContextMenu_Add')}`,
        'ta icon-thin-020',
        () => {
          this.addTag(model, this.table, ctx.rowsInAction);
        },
        null,
        false
      )
    );
  }

  public async addTag(
    viewComponent: IWorkplaceViewComponent,
    table: TableGridViewModelBase | null,
    rowsInAction: ITableRowViewModel[] | undefined = undefined
  ): Promise<void> {
    const viewMeta = viewComponent.viewMetadata!;
    let cardRef: ViewReferenceMetadataSealed;
    let refColumn: ViewColumnMetadataSealed;
    if (
      (cardRef = Array.from(viewMeta.references.values()).find(x => x.isCard)!) &&
      (refColumn = viewMeta.columns.get(cardRef.colPrefix + 'ID')!)
    ) {
      const cardIds = rowsInAction
        ? rowsInAction.map(row => row.data.get(refColumn.alias))
        : viewComponent.selectedRows!.map(row => row.get(refColumn.alias));

      if (!cardIds) {
        return;
      }
      const tagInfos = await selectOrCreateTag(cardIds);

      if (!tagInfos) {
        return;
      }

      for (const cardId of cardIds) {
        const result = await this._tagManager.storeTags(
          tagInfos.map(x => {
            const tag = new Tag();
            tag.tagId = x.id;
            tag.cardId = cardId;
            tag.userId = userSession.UserID;
            tag.setAt = moment.utc().format();
            return tag;
          }),
          null
        );
        if (!(await showNotEmpty(result))) {
          for (const tagInfo of tagInfos) {
            let cardTags = viewComponent.tags.get(cardId);
            if (cardTags?.find(x => x.id === tagInfo.id)) {
              continue;
            }
            if (!cardTags) {
              cardTags = [];
              viewComponent.tags.set(cardId, cardTags);
            }
            const model = new GridRowTagViewModel({
              id: tagInfo.id,
              cardId: cardId,
              tooltip: tagInfo.isCommon
                ? LocalizationManager.instance.format('$Tags_Common_Tooltip', tagInfo.name)
                : tagInfo.name,
              name: tagInfo.name,
              icon: tagInfo.icon,
              isCommon: tagInfo.isCommon,
              clickMode: tagInfo.clickMode,
              background: getRgbaFromDecimal(tagInfo.background)
            });
            cardTags.push(model);
            await showNotEmpty(result);
          }
        }
      }
      table?.rebuild();
    }
  }

  //#endregion
}

class AddTagButtonViewExtensionViewModel extends ViewButtonViewModel {
  //#region ctor

  constructor(
    viewComponent: IWorkplaceViewComponent,
    table: TableGridViewModelBase | null,
    addTag: (
      viewComponent: IWorkplaceViewComponent,
      table: TableGridViewModelBase | null
    ) => Promise<void>,
    area: ContentPlaceArea = ContentPlaceArea.ToolBarPanel,
    order: number = ContentPlaceOrder.Middle
  ) {
    super(viewComponent, area, order);

    this.theme = 'control';
    this.type = 'small';
    this.icon = 'icon-thin-020';
    this.tooltip = localize('$Tags_UI_AddTagButton_Title');
    this.caption = localize('$Tags_UI_AddTagButton_Title');
    this.showCaption = false;
    this.onClick = async () => {
      if (!this.viewComponent.selectedRow || !this.viewComponent.view) {
        return;
      }
      await addTag(viewComponent, table);
    };
    this._name = 'AddTagButtonViewExtension';
  }

  //#endregion
}
