import { reaction } from 'mobx';
import {
  DisposeList,
  FieldStorageMapChangedEventArgs,
  FieldType,
  Flags,
  Guid,
  ListChangedEventArgs,
  StorageArray,
  TypedField
} from '@tessa/core';
import { extension, inject } from '@tessa/application';
import {
  Card,
  CardHelper,
  CardRow,
  CardRowState,
  CardRowStateChangedEventArgs,
  CardSection,
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper,
  StageTypeDescriptors
} from '@tessa/platform';
import { designTimeCard, markName, unmarkName } from '../../workflow/krProcess/krUIHelper';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  ICardModel,
  isIFormWithBlocksViewModel
} from 'tessa/ui/cards';
import {
  GridViewModel,
  GridRowEventArgs,
  GridRowAction,
  AutoCompleteTableViewModel,
  RowAutoCompleteItem
} from 'tessa/ui/cards/controls';
import { Visibility } from 'tessa/platform';
import { IRuntimeBlockViewModel } from 'tessa/ui/formEditor/types';
import { RuntimeItemWithStateViewModel } from 'tessa/ui/formEditor/controls/runtimeItemWithStateViewModel';

@extension({ name: 'KrAdditionalApprovalCardUIExtension' })
export class KrAdditionalApprovalCardUIExtension extends CardUIExtension {
  //#region fields

  private _disposes = new DisposeList();
  private _card: Card;
  private _handleManager: HandleManager;
  private _lastSelectedItem: RowAutoCompleteItem | null;

  //#endregion

  //#region ctor

  constructor(@inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  async initialized(context: ICardUIExtensionContext): Promise<void> {
    const model = context.model;

    if (!(await this.isCardAvailableForExtension(model))) {
      return;
    }

    const approvalTab = model.mainForm
      ? model.forms.find(x => x.name === 'ApprovalProcess')
      : model.wysiwygForm?.getRootBlock()?.getItem<IRuntimeBlockViewModel>('ApprovalProcess');
    if (!approvalTab) {
      return;
    }

    this._card = context.card;

    // Находим блок с этапами и подписываемся на открытие строки с этапом
    const approvalStagesTable = isIFormWithBlocksViewModel(approvalTab)
      ? (approvalTab.blocks.find(x => x.name === 'ApprovalStagesBlock')!
          .controls[0] as GridViewModel)
      : approvalTab
          .getItem<RuntimeItemWithStateViewModel>('ApprovalStagesTable')
          ?.getCurrent<GridViewModel>();
    if (approvalStagesTable) {
      this._disposes.add(
        approvalStagesTable.rowInvoked.addWithDispose(this.approvalStagesTable_RowInvoked)!
      );
    }
  }

  async finalized(): Promise<void> {
    this._disposes.dispose();
  }

  //#endregion

  //#region methods

  private async isCardAvailableForExtension(model: ICardModel): Promise<boolean> {
    if (designTimeCard(model.card.typeId)) {
      return true;
    }

    const usedComponents = await KrComponentsHelper.getKrComponentsByCard(
      model.card,
      this._krTypesCache
    );
    return Flags.hasFlag(usedComponents, KrComponents.Routes);
  }

  private approvalStagesTable_RowInvoked = (e: GridRowEventArgs) => {
    if (e.action !== GridRowAction.Inserted && e.action !== GridRowAction.Opening) {
      return;
    }

    // Проверим, что все необходимые блоки есть
    if (
      !e.rowModel!.blocks.has('PerformersBlock') ||
      !e.rowModel!.blocks.has('AdditionalApprovalBlock')
    ) {
      return;
    }

    if (e.row.tryGet('StageTypeID') !== StageTypeDescriptors.approvalDescriptor.id) {
      return;
    }

    this._lastSelectedItem = null;

    // Получим необходимые блоки
    const approvalBlock = e.rowModel!.blocks.get('PerformersBlock')!;
    const additionalApprovalBlock = e.rowModel!.blocks.get('AdditionalApprovalBlock')!;

    // Скроем блок с доп. согласующими
    additionalApprovalBlock.blockVisibility = Visibility.Collapsed;

    // Найдём контрол с согласующими
    const approversControl = approvalBlock.controls.find(
      x => x.name === 'MultiplePerformersTableAC'
    ) as AutoCompleteTableViewModel;
    if (!approversControl) {
      return;
    }

    // контрол будет открывать меню выбора по двойному клику, чтобы не мешался
    approversControl.hasSelectionAction = true;

    // Найдём контрол с доп. согласующими
    const additionalApproversControl = additionalApprovalBlock.controls.find(
      x => x.name === 'AdditionalApprovers'
    ) as AutoCompleteTableViewModel;
    if (!additionalApproversControl) {
      return;
    }

    // контрол будет открывать меню выбора по двойному клику, чтобы не мешался
    additionalApproversControl.hasSelectionAction = true;

    // Получаем секцию с согласующими
    const approversVirtualSection =
      this._card.typeId === '4a377758-2366-47e9-98ac-c5f553974236'
        ? this._card.sections.get('KrPerformersVirtual')
        : this._card.sections.get('KrPerformersVirtual_Synthetic');

    // Получаем секции, которые будут отображать инфо по доп. согласующим.
    const infoUsersVirtualSection = this._card.sections.get(
      'KrAdditionalApprovalInfoUsersCardVirtual_Synthetic'
    );

    // Получаем секции для хранения доп. согласующих.
    const additionalApprovalUsersVirtualSection = this._card.sections.get(
      'KrAdditionalApprovalUsersCardVirtual_Synthetic'
    );

    let name: string;
    for (const row of approversVirtualSection.rows) {
      if (
        additionalApprovalUsersVirtualSection.rows.some(
          x => x.get('MainApproverRowID') === row.rowId && x.state !== CardRowState.Deleted
        ) &&
        !!(name = row.get<string>('PerformerName')!)
      ) {
        row.set('PerformerName', markName(name)!, FieldType.String);
      }
    }

    let isClosed = false;

    this._handleManager = new HandleManager(e.rowModel!, () => {
      isClosed = true;

      this.transferData(
        e.row,
        infoUsersVirtualSection,
        additionalApprovalUsersVirtualSection,
        approversControl.selectedItem as RowAutoCompleteItem
      );

      // Стираем лишние отметки о доп. согласовании
      for (const row of approversVirtualSection.rows) {
        if (
          additionalApprovalUsersVirtualSection.rows.every(
            x => x.get('MainApproverRowID') !== row.rowId || x.state === CardRowState.Deleted
          )
        ) {
          const name = row.get<string>('PerformerName')!;
          row.set('PerformerName', unmarkName(name)!, FieldType.String);
        }
      }
    });

    this._disposes.add(
      approversControl.valueDeleted.addWithDispose(e => {
        const item = e.item as RowAutoCompleteItem;
        if (approversControl.selectedItem === item) {
          infoUsersVirtualSection.rows.clear();
          additionalApprovalBlock.blockVisibility = Visibility.Collapsed;
        }

        CardHelper.clearStorageRows(additionalApprovalUsersVirtualSection.rows, row =>
          Guid.equals(row.get('MainApproverRowID'), item.row.rowId)
        );
      })!
    );

    this._disposes.add(
      additionalApproversControl.valueDeleted.addWithDispose(e => {
        // Смена Display текста при удалении доп. согласующих.
        const mainApprover = approversControl.selectedItem as RowAutoCompleteItem;

        if (!mainApprover) {
          return;
        }

        const item = e.item as RowAutoCompleteItem;

        if (
          infoUsersVirtualSection.rows.every(
            x =>
              x.state === CardRowState.Deleted ||
              x.get('MainApproverRowID') !== mainApprover.row.rowId ||
              x.rowId === item.row.rowId
          )
        ) {
          const name = mainApprover.row.get<string>('PerformerName')!;
          mainApprover.row.set('PerformerName', unmarkName(name)!, FieldType.String);
        }
      })!
    );

    this._disposes.add(
      reaction(
        () => approversControl.selectedItem,
        (selectedItem: RowAutoCompleteItem | null) => {
          // потерю фокуса с выделенного элемента не обрабатываем
          if (!selectedItem) {
            return;
          }

          this._handleManager.unhandleAllAdditionalApproverItemRows();
          this._handleManager.unhandleFirstIsResponsible();

          // Получаем последний выделенный элемент
          const item = selectedItem;

          this._handleManager.handleAdditionalApproversListItemChanged(
            infoUsersVirtualSection.rows,
            e => {
              if (isClosed) {
                return;
              }

              // Смена Display текста в зависимости от изменения списка доп. согласующих.
              const mainApproverRow = approversVirtualSection.rows.find(
                x => x.rowId === item!.row.rowId
              );
              if (mainApproverRow) {
                const name = mainApproverRow.get<string>('PerformerName')!;
                if (infoUsersVirtualSection.rows.length > 0) {
                  mainApproverRow.set('PerformerName', markName(name)!, FieldType.String);
                }
              }

              const items = e.added.length > 0 ? e.added : e.removed;
              for (const changedItem of items) {
                if (changedItem.state === CardRowState.None) {
                  this._handleManager.handleAdditionalApproverItemRow(changedItem, e => {
                    if (
                      e.newState === CardRowState.Inserted ||
                      e.newState === CardRowState.Modified
                    ) {
                      changedItem.set('MainApproverRowID', item!.row.rowId, FieldType.Guid);
                      this._handleManager.unhandleAdditionalApproverItemRow(changedItem);
                    }
                  });
                } else if (
                  (changedItem.state === CardRowState.Inserted ||
                    changedItem.state === CardRowState.Modified) &&
                  !changedItem.get('MainApproverRowID')
                ) {
                  changedItem.set('MainApproverRowID', item!.row.rowId, FieldType.Guid);
                }
              }
            }
          );

          // Переносим данные в хранение и очищаем
          this.transferData(
            e.row,
            infoUsersVirtualSection,
            additionalApprovalUsersVirtualSection,
            this._lastSelectedItem
          );
          this._lastSelectedItem = selectedItem;

          // Наполняем строки автокомплита с доп. согласующими.
          if (additionalApprovalUsersVirtualSection.rows.length > 0) {
            const sortedRows = additionalApprovalUsersVirtualSection.rows
              .filter(x => x.get('MainApproverRowID') === item!.row.rowId)
              .sort((a, b) => a.get<number>('Order')! - b.get<number>('Order')!);

            for (const row of sortedRows) {
              if (row.state !== CardRowState.Deleted) {
                infoUsersVirtualSection.rows.add(row);
              }
            }
          }

          const value =
            infoUsersVirtualSection.rows.length > 0 &&
            infoUsersVirtualSection.rows.some(x => !!x.get('IsResponsible'));
          e.row.set('KrApprovalSettingsVirtual__FirstIsResponsible', value, FieldType.Boolean);

          // Если блок скрыт - показываем
          if (additionalApprovalBlock.blockVisibility === Visibility.Collapsed) {
            additionalApprovalBlock.blockVisibility = Visibility.Visible;
          }
        }
      )
    );
  };

  private transferData(
    mainRow: CardRow,
    infoUsersVirtualSection: CardSection,
    additionalApprovalUsersVirtualSection: CardSection,
    selectedItem: RowAutoCompleteItem | null
  ) {
    if (!selectedItem) {
      return;
    }

    if (infoUsersVirtualSection.rows.length > 0) {
      const infoUsersVirtualSectionOrderedRows = infoUsersVirtualSection.rows
        .map(x => x)
        .sort((a, b) => a.get<number>('Order')! - b.get<number>('Order')!);

      if (mainRow.get('KrApprovalSettingsVirtual__FirstIsResponsible')) {
        // находим старого ответственного
        const oldResponsibleRow = infoUsersVirtualSection.rows.find(x => !!x.get('IsResponsible'));
        if (infoUsersVirtualSection.rows.some(x => x.state !== CardRowState.Deleted)) {
          // находим минимальный ордер среди не удалённых
          const notDeletedRowsMinOrderValue = Math.min(
            ...infoUsersVirtualSection.rows
              .filter(x => x.state !== CardRowState.Deleted)
              .map(x => x.get<number>('Order')!)
          );
          // если есть старый ответственный и его порядок не соответствует минимальном ордеру среди не удалённых
          // снимаем ему галочку
          if (oldResponsibleRow) {
            if (oldResponsibleRow.get('Order') !== notDeletedRowsMinOrderValue) {
              oldResponsibleRow.set('IsResponsible', TypedField.falseBoolean);

              // находим нового ответственного
              const newResponsibleRow = infoUsersVirtualSection.rows.find(
                x => x.get('Order') === notDeletedRowsMinOrderValue
              );
              // ставим ему флаг ответственности
              newResponsibleRow!.set('IsResponsible', TypedField.trueBoolean);
            }
          } else {
            // находим нового ответственного
            const newResponsibleRow = infoUsersVirtualSection.rows.find(
              x => x.get('Order') === notDeletedRowsMinOrderValue
            );
            // ставим ему флаг ответственности
            newResponsibleRow!.set('IsResponsible', TypedField.trueBoolean);
          }
        }
      } else {
        // находим старого ответственного
        const oldResponsibleRow = infoUsersVirtualSection.rows.find(x => !!x.get('IsResponsible'));
        if (oldResponsibleRow) {
          oldResponsibleRow.set('IsResponsible', TypedField.falseBoolean);
        }
      }

      // Запоминаем старые удалённые элементы
      const deletedRows = additionalApprovalUsersVirtualSection.rows.filter(
        x => x.state === CardRowState.Deleted
      );
      for (const deletedRow of deletedRows) {
        additionalApprovalUsersVirtualSection.rows.remove(deletedRow);
      }

      for (let i = additionalApprovalUsersVirtualSection.rows.length; i > 0; i--) {
        const row = additionalApprovalUsersVirtualSection.rows[i - 1];
        if (row.get('MainApproverRowID') === selectedItem.row.rowId) {
          additionalApprovalUsersVirtualSection.rows.remove(row);
        }
      }

      for (const row of infoUsersVirtualSectionOrderedRows) {
        additionalApprovalUsersVirtualSection.rows.add(row);
      }

      // Восстанавливаем старые удалённые элементы
      for (const deletedRow of deletedRows) {
        additionalApprovalUsersVirtualSection.rows.add(deletedRow);
      }
    } else {
      for (let i = additionalApprovalUsersVirtualSection.rows.length; i > 0; i--) {
        const row = additionalApprovalUsersVirtualSection.rows[i - 1];
        if (row.get('MainApproverRowID') === selectedItem.row.rowId) {
          additionalApprovalUsersVirtualSection.rows.remove(row);
        }
      }
    }

    // Чистим данные перед наполнением
    infoUsersVirtualSection.rows.clear();
    mainRow.setChanged('KrApprovalSettingsVirtual__FirstIsResponsible', false);
  }

  //#endregion
}

class HandleManager {
  //#region ctor

  constructor(model: ICardModel, formCloseAction: Function) {
    this.model = model;
    this.formCloseAction = formCloseAction;
    this.model.mainForm!.closed.add(this.mainForm_Closed);
    this.additionalApproverHandlers = new Map();
  }

  //#endregion

  //#region fields

  private model: ICardModel;
  private formCloseAction: Function;

  private additionalApproverHandlers: Map<CardRow, (e: CardRowStateChangedEventArgs) => void>;

  private additionalApproversListRows: StorageArray<CardRow> | null;
  private additionalApproversListRowsHandler: ((e: ListChangedEventArgs<CardRow>) => void) | null;

  private firstIsResponsibleRow: CardRow | null;
  private firstIsResponsibleRowHandler: ((e: FieldStorageMapChangedEventArgs) => void) | null;

  //#endregion

  //#region methods

  private mainForm_Closed = () => {
    this.formCloseAction();
    this.unhandleAdditionalApproversListItemChanged();
    this.unhandleFirstIsResponsible();
    this.model.mainForm!.closed.remove(this.mainForm_Closed);
  };

  public handleAdditionalApproversListItemChanged(
    rows: StorageArray<CardRow>,
    handler: (e: ListChangedEventArgs<CardRow>) => void
  ): void {
    if (this.additionalApproversListRows && this.additionalApproversListRowsHandler) {
      this.additionalApproversListRows.collectionChanged.remove(
        this.additionalApproversListRowsHandler
      );
    }
    this.additionalApproversListRows = rows;
    this.additionalApproversListRowsHandler = handler;

    this.additionalApproversListRows.collectionChanged.add(this.additionalApproversListRowsHandler);
  }

  private unhandleAdditionalApproversListItemChanged() {
    if (!this.additionalApproversListRowsHandler) {
      return;
    }

    this.additionalApproversListRows!.collectionChanged.remove(
      this.additionalApproversListRowsHandler
    );
    this.additionalApproversListRows = null;
    this.additionalApproversListRowsHandler = null;
  }

  public handleAdditionalApproverItemRow(
    row: CardRow,
    handler: (e: CardRowStateChangedEventArgs) => void
  ): void {
    if (this.additionalApproverHandlers.has(row)) {
      return;
    }
    row.stateChanged.add(handler);
    this.additionalApproverHandlers.set(row, handler);
  }

  public unhandleAdditionalApproverItemRow(row: CardRow): void {
    const handler = this.additionalApproverHandlers.get(row)!;
    row.stateChanged.remove(handler);
    this.additionalApproverHandlers.delete(row);
  }

  public unhandleAllAdditionalApproverItemRows(): void {
    for (const pair of this.additionalApproverHandlers) {
      pair[0].stateChanged.remove(pair[1]);
    }
    this.additionalApproverHandlers.clear();
  }

  public handleFirstIsResponsible(
    row: CardRow,
    handler: (e: FieldStorageMapChangedEventArgs) => void
  ): void {
    if (this.firstIsResponsibleRow && this.firstIsResponsibleRowHandler) {
      this.firstIsResponsibleRow.fieldChanged.remove(this.firstIsResponsibleRowHandler);
    }
    this.firstIsResponsibleRow = row;
    this.firstIsResponsibleRowHandler = handler;

    this.firstIsResponsibleRow.fieldChanged.add(this.firstIsResponsibleRowHandler);
  }

  public unhandleFirstIsResponsible(): void {
    if (!this.firstIsResponsibleRowHandler) {
      return;
    }

    this.firstIsResponsibleRow!.fieldChanged.remove(this.firstIsResponsibleRowHandler);
    this.firstIsResponsibleRow = null;
    this.firstIsResponsibleRowHandler = null;
  }

  //#endregion
}
