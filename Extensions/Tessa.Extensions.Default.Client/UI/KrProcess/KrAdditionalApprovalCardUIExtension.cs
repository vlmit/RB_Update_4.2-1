#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Cards.Controls.AutoComplete;
using Tessa.UI.Controls.AutoCompleteCtrl;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Client.UI.KrProcess
{
    public sealed class KrAdditionalApprovalCardUIExtension :
        CardUIExtension
    {
        #region Internal Objects

        private sealed class HandleManager
        {
            private AutoCompleteTableViewModel? approversControl;
            private PropertyChangedEventHandler? approversControlHandler;
            private NotifyCollectionChangedEventHandler? approversControlCollectionChangedHandler;

            private ListStorage<CardRow>? additionalApproversListRows;
            private EventHandler<ListStorageItemEventArgs<CardRow>>? additionalApproversListRowsHandler;

            private CardRow? firstIsResponsibleRow;
            private EventHandler<CardFieldChangedEventArgs>? firstIsResponsibleRowHandler;

            private readonly Dictionary<CardRow, EventHandler<CardRowStateEventArgs>> additionalApproverHandlers;
            private readonly Action formCloseAction;

            private readonly ICardModel model;

            public HandleManager(ICardModel model, Action formCloseAction)
            {
                this.model = model;
                this.formCloseAction = formCloseAction;
                this.model.MainForm.Closed += this.MainForm_Closed;
                this.additionalApproverHandlers = new Dictionary<CardRow, EventHandler<CardRowStateEventArgs>>();
            }

            private void MainForm_Closed(object? sender, EventArgs e)
            {
                this.formCloseAction.Invoke();
                this.UnhandleAdditionalApproversListItemChanged();
                this.DetachFirstIsResponsible();
                this.UnhandleApproversControld();
                this.model.MainForm.Closed -= this.MainForm_Closed;
            }

            public void HandleApproversControl(
                AutoCompleteTableViewModel control,
                NotifyCollectionChangedEventHandler collectionChangedHandler,
                PropertyChangedEventHandler handler)
            {
                if (this.approversControl is not null
                    && this.approversControlHandler is not null
                    && this.approversControlCollectionChangedHandler is not null)
                {
                    ((INotifyPropertyChanged) this.approversControl.ItemsSource).PropertyChanged -= this.approversControlHandler;
                    this.approversControl.Items.CollectionChanged -= this.approversControlCollectionChangedHandler;
                }

                this.approversControl = control;
                this.approversControlHandler = handler;
                this.approversControlCollectionChangedHandler = collectionChangedHandler;

                this.approversControl.Items.CollectionChanged += this.approversControlCollectionChangedHandler;
                ((INotifyPropertyChanged) this.approversControl.ItemsSource).PropertyChanged += this.approversControlHandler;
            }

            private void UnhandleApproversControld()
            {
                if (this.approversControlHandler is null
                    || this.approversControl is null)
                {
                    return;
                }

                ((INotifyPropertyChanged) this.approversControl.ItemsSource).PropertyChanged -= this.approversControlHandler;
                this.approversControlHandler = null;

                if (this.approversControlCollectionChangedHandler is null)
                {
                    this.approversControl = null;
                    return;
                }

                this.approversControl.Items.CollectionChanged -= this.approversControlCollectionChangedHandler;
                this.approversControl = null;
            }

            public void HandleAdditionalApproversListItemChanged(ListStorage<CardRow> rows, EventHandler<ListStorageItemEventArgs<CardRow>> handler)
            {
                if (this.additionalApproversListRows is not null
                    && this.additionalApproversListRowsHandler is not null)
                {
                    this.additionalApproversListRows.ItemChanged -= this.additionalApproversListRowsHandler;
                }

                this.additionalApproversListRows = rows;
                this.additionalApproversListRowsHandler = handler;

                this.additionalApproversListRows.ItemChanged += this.additionalApproversListRowsHandler;
            }

            private void UnhandleAdditionalApproversListItemChanged()
            {
                if (this.additionalApproversListRowsHandler is null
                    || this.additionalApproversListRows is null)
                {
                    return;
                }

                this.additionalApproversListRows.ItemChanged -= this.additionalApproversListRowsHandler;
                this.additionalApproversListRows = null;
                this.additionalApproversListRowsHandler = null;
            }

            public void HandleAdditionalApproverItemRow(CardRow row, EventHandler<CardRowStateEventArgs> handler)
            {
                if (this.additionalApproverHandlers.ContainsKey(row))
                {
                    return;
                }

                row.StateChanged += handler;
                this.additionalApproverHandlers.Add(row, handler);
            }

            public void DetachAdditionalApproverItemRow(CardRow row)
            {
                var handler = this.additionalApproverHandlers[row];
                row.StateChanged -= handler;
                this.additionalApproverHandlers.Remove(row);
            }

            public void DetachAllAdditionalApproverItemRows()
            {
                foreach (var handler in this.additionalApproverHandlers)
                {
                    handler.Key.StateChanged -= handler.Value;
                }

                this.additionalApproverHandlers.Clear();
            }

            public void HandleFirstIsResponsible(CardRow row, EventHandler<CardFieldChangedEventArgs> handler)
            {
                if (this.firstIsResponsibleRow is not null
                    && this.firstIsResponsibleRowHandler is not null)
                {
                    this.firstIsResponsibleRow.FieldChanged -= this.firstIsResponsibleRowHandler;
                }

                this.firstIsResponsibleRow = row;
                this.firstIsResponsibleRowHandler = handler;

                this.firstIsResponsibleRow.FieldChanged += this.firstIsResponsibleRowHandler;
            }

            public void DetachFirstIsResponsible()
            {
                if (this.firstIsResponsibleRowHandler is null
                    || this.firstIsResponsibleRow is null)
                {
                    return;
                }

                this.firstIsResponsibleRow.FieldChanged -= this.firstIsResponsibleRowHandler;
                this.firstIsResponsibleRow = null;
                this.firstIsResponsibleRowHandler = null;
            }
        }

        #endregion

        #region Fields

        private readonly IKrTypesCache typesCache;

        private HandleManager? handleManager;

        private Card? card;

        private RowAutoCompleteItem? lastSelectedItem;

        #endregion

        #region Constructors

        public KrAdditionalApprovalCardUIExtension(IKrTypesCache typesCache)
        {
            this.typesCache = NotNullOrThrow(typesCache);
        }

        #endregion

        #region Event Handlers

        private void ApprovalStagesTable_RowInvoked(object? sender, GridRowEventArgs evt)
        {
            if (this.card is null
                || evt.Action is not GridRowAction.Inserted and not GridRowAction.Opening
                || evt.Row.TryGet<Guid?>(KrStages.StageTypeID) != StageTypeDescriptors.ApprovalDescriptor.ID)
            {
                return;
            }

            var approvalBlock = evt
                .RowModel
                .Blocks
                .FirstOrDefault(static p => p.Key == Ui.KrPerformersBlockAlias)
                .Value;

            if (approvalBlock is null)
            {
                return;
            }

            var additionalApprovalBlock = evt
                .RowModel
                .Blocks
                .FirstOrDefault(static p => p.Key == Ui.AdditionalApprovalBlock)
                .Value;

            if (additionalApprovalBlock is null)
            {
                return;
            }

            this.lastSelectedItem = null;

            // Скроем блок с доп. согласующими
            additionalApprovalBlock.BlockVisibility = Visibility.Collapsed;
            evt.RowModel.MainFormWithBlocks.RearrangeSelf();

            // Найдём контрол с согласующими
            var approversControl = (AutoCompleteTableViewModel?) approvalBlock
                .Controls
                .FirstOrDefault(static p => p.Name == Ui.KrMultiplePerformersTableAcAlias);

            if (approversControl is null)
            {
                return;
            }

            // Получаем секцию с согласующими
            var approversVirtualSection = this.card.GetPerformersSection();

            var infoUsersVirtualSection = this.card.Sections[KrAdditionalApprovalInfoUsersCardVirtual.Synthetic];

            var additionalApprovalUsersVirtualSection = this.card.Sections[KrAdditionalApprovalUsersCardVirtual.Synthetic];

            // Проставляем отображение тех согласующих, которые имеют доп. согласование
            foreach (var row in approversVirtualSection.Rows)
            {
                if (additionalApprovalUsersVirtualSection.Rows.Any(p =>
                        p.Get<Guid>(KrAdditionalApprovalUsersCardVirtual.MainApproverRowID) == row.RowID
                        && p.State != CardRowState.Deleted)
                    && row.Fields[KrPerformersVirtual.PerformerName] is string name)
                {
                    row.Fields[KrPerformersVirtual.PerformerName] = KrAdditionalApprovalMarker.Mark(name);
                }
            }

            this.handleManager = new HandleManager(
                evt.RowModel,
                () =>
                {
                    TransferData(
                        evt.Row,
                        infoUsersVirtualSection,
                        additionalApprovalUsersVirtualSection,
                        (RowAutoCompleteItem) approversControl.ItemsSource.SelectedItem);

                    // Стираем лишние отметки о доп-согласовании
                    foreach (var row in approversVirtualSection.Rows)
                    {
                        if (additionalApprovalUsersVirtualSection.Rows.All(p =>
                                p.Get<Guid>(KrAdditionalApprovalUsersCardVirtual.MainApproverRowID) != row.RowID
                                || p.State == CardRowState.Deleted))
                        {
                            var name = row.Fields.Get<string>(KrPerformersVirtual.PerformerName);
                            row.Fields[KrPerformersVirtual.PerformerName] = KrAdditionalApprovalMarker.Unmark(name);
                        }
                    }

                    evt.Control.Rearrange();
                });

            // Через менеджер подписываемся на изменение выбранного элемента и изменение коллекции контрола
            this.handleManager.HandleApproversControl(
                approversControl,
                (_, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Remove
                        && args.OldItems is { Count: > 0 })
                    {
                        foreach (var argItem in args.OldItems)
                        {
                            if (argItem is not RowAutoCompleteItem item)
                            {
                                continue;
                            }

                            if (approversControl.ItemsSource.SelectedItem == item)
                            {
                                // Скроем блок с доп. согласующими
                                infoUsersVirtualSection.Rows.Clear();
                                additionalApprovalBlock.BlockVisibility = Visibility.Collapsed;
                                evt.RowModel.MainFormWithBlocks.RearrangeSelf();
                            }

                            for (var i = additionalApprovalUsersVirtualSection.Rows.Count - 1; i >= 0; i--)
                            {
                                var row = additionalApprovalUsersVirtualSection.Rows[i];

                                if (row.Get<Guid>(KrAdditionalApprovalUsersCardVirtual.MainApproverRowID) == item.Row.RowID)
                                {
                                    if (row.State != CardRowState.Inserted)
                                    {
                                        row.State = CardRowState.Deleted;
                                    }
                                    else
                                    {
                                        additionalApprovalUsersVirtualSection.Rows.RemoveAt(i);
                                    }
                                }
                            }
                        }
                    }
                },
            (dataSource, e) =>
            {
                if (e.PropertyName == "SelectedItem")
                {
                    this.handleManager.DetachAllAdditionalApproverItemRows();
                    this.handleManager.DetachFirstIsResponsible();

                    // Получаем последний выделенный элемент
                    var selectedRow = ((IAutoCompleteDataSource) dataSource!).SelectedItem;
                    var selectedRowID = ((RowAutoCompleteItem) selectedRow).Row.RowID;

                    // Подписываемся через менеджер на изменение элементов списка доп. согласующих
                    this.handleManager.HandleAdditionalApproversListItemChanged(
                        infoUsersVirtualSection.Rows,
                        (_, args) =>
                        {
                            if (args.Item is null)
                            {
                                return;
                            }

                            // Смена Display текста в зависимости от изменения списка доп. согласующих.
                            var mainApproverRow = approversVirtualSection.Rows.FirstOrDefault(p => p.RowID == selectedRowID);

                            if (mainApproverRow is not null)
                            {
                                var name = mainApproverRow.Fields.Get<string>(KrPerformersVirtual.PerformerName);

                                if (infoUsersVirtualSection.Rows.Count > 0)
                                {
                                    mainApproverRow.Fields[KrPerformersVirtual.PerformerName] = KrAdditionalApprovalMarker.Mark(name);
                                }
                                else
                                {
                                    mainApproverRow.Fields[KrPerformersVirtual.PerformerName] = KrAdditionalApprovalMarker.Unmark(name);
                                }
                            }

                            if (args.Item.State == CardRowState.None)
                            {
                                this.handleManager.HandleAdditionalApproverItemRow(
                                    args.Item,
                                    (_, eventArgs) =>
                                    {
                                        if (eventArgs.NewState is CardRowState.Inserted
                                            or CardRowState.Modified)
                                        {
                                            args.Item.Fields["MainApproverRowID"] = selectedRowID;
                                            this.handleManager.DetachAdditionalApproverItemRow(args.Item);
                                        }
                                    });
                            }
                            else if ((args.Item.State == CardRowState.Inserted
                                    || args.Item.State == CardRowState.Modified)
                                    && args.Item.Fields["MainApproverRowID"] is null)
                            {
                                args.Item.Fields["MainApproverRowID"] = selectedRowID;
                            }
                        });

                    // Переносим данные в хранение и очищаем
                    TransferData(
                        evt.Row,
                        infoUsersVirtualSection,
                        additionalApprovalUsersVirtualSection,
                        this.lastSelectedItem);

                    this.lastSelectedItem = (RowAutoCompleteItem) approversControl.ItemsSource.SelectedItem;

                    // Наполняем строки автокомплита с доп. согласующими.
                    if (additionalApprovalUsersVirtualSection.Rows.Count > 0)
                    {
                        foreach (var row in additionalApprovalUsersVirtualSection.Rows
                            .Where(p => p.Get<Guid>(KrAdditionalApprovalUsersCardVirtual.MainApproverRowID) == selectedRowID)
                            .OrderBy(static p => p.Get<int>(KrAdditionalApprovalUsersCardVirtual.Order)))
                        {
                            if (row.State != CardRowState.Deleted)
                            {
                                infoUsersVirtualSection.Rows.Add(row);
                            }
                        }
                    }

                    evt.Row.Fields[KrApprovalSettingsVirtual.FirstIsResponsible] = BooleanBoxes.Box(
                        infoUsersVirtualSection.Rows.Count > 0
                        && infoUsersVirtualSection.Rows.Any(static p => p.Get<bool>(KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible)));

                    // Если блок скрыт - показываем
                    if (additionalApprovalBlock.BlockVisibility == Visibility.Collapsed)
                    {
                        additionalApprovalBlock.BlockVisibility = Visibility.Visible;
                        evt.RowModel.MainFormWithBlocks.RearrangeSelf();
                    }
                }
            });
        }

        private static void TransferData(
            CardRow mainRow,
            CardSection infoUsersVirtualSection,
            CardSection additionalApprovalUsersVirtualSection,
            RowAutoCompleteItem? selectedItem)
        {
            if (selectedItem is null)
            {
                return;
            }

            if (infoUsersVirtualSection.Rows.Count > 0)
            {
                var infoUsersVirtualSectionOrdredRows = infoUsersVirtualSection
                    .Rows
                    .OrderBy(static p => p.Get<int>(KrAdditionalApprovalInfoUsersCardVirtual.Order));

                if (mainRow.Get<bool>(KrApprovalSettingsVirtual.FirstIsResponsible))
                {
                    // находим старого ответственного
                    var oldResponsibleRow = infoUsersVirtualSection
                        .Rows
                        .FirstOrDefault(static p => p.Get<bool>(KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible));

                    if (infoUsersVirtualSection.Rows.Any(q => q.State != CardRowState.Deleted))
                    {
                        // находим минимальный ордер среди не удалённых
                        var notDeletedRowsMinOrderValue = infoUsersVirtualSection
                            .Rows
                            .Where(q => q.State != CardRowState.Deleted)
                            .Min(static p => p.Get<int>(KrAdditionalApprovalInfoUsersCardVirtual.Order));

                        // если есть старый ответственный и его порядок не соответствует минимальном порядку среди не удалённых
                        // снимаем ему галочку
                        if (oldResponsibleRow is not null)
                        {
                            if (oldResponsibleRow.Get<int>(KrAdditionalApprovalInfoUsersCardVirtual.Order) != notDeletedRowsMinOrderValue)
                            {
                                oldResponsibleRow.Fields[KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible] = BooleanBoxes.False;

                                // находим нового ответственного
                                var newResponsibleRow = infoUsersVirtualSection.Rows.FirstOrDefault(p => p.Get<int>(KrAdditionalApprovalInfoUsersCardVirtual.Order) == notDeletedRowsMinOrderValue);

                                if (newResponsibleRow is not null)
                                {
                                    // ставим ему флаг ответственности
                                    newResponsibleRow.Fields[KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible] = BooleanBoxes.True;
                                }
                            }
                        }
                        else
                        {
                            // находим нового ответственного
                            var newResponsibleRow = infoUsersVirtualSection.Rows.FirstOrDefault(p => p.Get<int>(KrAdditionalApprovalInfoUsersCardVirtual.Order) == notDeletedRowsMinOrderValue);

                            if (newResponsibleRow is not null)
                            {
                                // ставим ему флаг ответственности
                                newResponsibleRow.Fields[KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible] = BooleanBoxes.True;
                            }
                        }
                    }
                }
                else
                {
                    // находим старого ответственного
                    var oldResponsibleRow = infoUsersVirtualSection
                        .Rows
                        .FirstOrDefault(static p => p.Get<bool>(KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible));

                    if (oldResponsibleRow is not null)
                    {
                        oldResponsibleRow.Fields[KrAdditionalApprovalInfoUsersCardVirtual.IsResponsible] = BooleanBoxes.False;
                    }
                }

                // Запоминаем старые удалённые элементы
                var deletedRows = additionalApprovalUsersVirtualSection
                    .Rows
                    .Where(static p => p.State == CardRowState.Deleted)
                    .ToArray();

                foreach (var deletedRow in deletedRows)
                {
                    additionalApprovalUsersVirtualSection.Rows.Remove(deletedRow);
                }

                for (var i = additionalApprovalUsersVirtualSection.Rows.Count; i > 0; i--)
                {
                    var row = additionalApprovalUsersVirtualSection.Rows[i - 1];
                    if (row.Get<Guid>(KrAdditionalApprovalUsersCardVirtual.MainApproverRowID) == selectedItem.Row.RowID)
                    {
                        additionalApprovalUsersVirtualSection.Rows.RemoveAt(i - 1);
                    }
                }

                foreach (var row in infoUsersVirtualSectionOrdredRows)
                {
                    additionalApprovalUsersVirtualSection.Rows.Add(row);
                }
                // Восстанавливаем старые удалённые элементы
                foreach (var deletedRow in deletedRows)
                {
                    additionalApprovalUsersVirtualSection.Rows.Add(deletedRow);
                }
            }
            else
            {
                for (var i = additionalApprovalUsersVirtualSection.Rows.Count; i > 0; i--)
                {
                    var row = additionalApprovalUsersVirtualSection.Rows[i - 1];
                    if (row.Get<Guid>(KrAdditionalApprovalUsersCardVirtual.MainApproverRowID) == selectedItem.Row.RowID)
                    {
                        additionalApprovalUsersVirtualSection.Rows.RemoveAt(i - 1);
                    }
                }
            }

            // Чистим данные перед наполнением
            infoUsersVirtualSection.Rows.Clear();
            mainRow.SetChanged(KrApprovalSettingsVirtual.FirstIsResponsible, false);
        }

        private async ValueTask<bool> IsCardAvailableForExtensionAsync(ICardModel model, CancellationToken cancellationToken = default)
        {
            if (KrProcessSharedHelper.DesignTimeCard(model.Card.TypeID))
            {
                return true;
            }

            var usedComponents = await KrComponentsHelper.GetKrComponentsAsync(
                model.Card,
                this.typesCache,
                cancellationToken);

            return usedComponents.Has(KrComponents.Routes);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task Initialized(ICardUIExtensionContext context)
        {
            var model = context.Model;

            if (!await this.IsCardAvailableForExtensionAsync(model, context.CancellationToken))
            {
                return;
            }

            if (!model.Forms.TryGet(Ui.KrApprovalProcessFormAlias, out var approvalTab))
            {
                return;
            }

            this.card = context.Card;

            // Находим блок с этапами и подписываемся на открытие строки с этапом
            var approvalStagesBlock = approvalTab
                .Blocks
                .FirstOrDefault(static p => p.Name == Ui.KrApprovalStagesBlockAlias);

            var approvalStagesTable = approvalStagesBlock
                ?.Controls
                .OfType<GridViewModel>()
                .FirstOrDefault();

            if (approvalStagesTable is not null)
            {
                approvalStagesTable.RowInvoked += this.ApprovalStagesTable_RowInvoked;
            }
        }

        #endregion
    }
}
