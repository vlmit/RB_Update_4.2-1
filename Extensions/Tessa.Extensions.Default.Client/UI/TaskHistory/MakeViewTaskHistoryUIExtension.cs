#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Tessa.Cards;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Extensions.Platform.Client.UI;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Cards.Tasks;
using Tessa.UI.Controls;
using Tessa.UI.Menu;
using Tessa.UI.Views.Content;

namespace Tessa.Extensions.Default.Client.UI.TaskHistory
{
    /// <summary>
    /// Реализация расширения типа карточки для добавления представлению функционала Истории заданий
    /// Добавляет стандартное контексное меню, тултип, обработку нажатий и открытие окна с деталями.
    /// Вторая часть расширения типа в WfTaskHistoryViewUIExtension
    /// </summary>
    public sealed class MakeViewTaskHistoryUIExtension(ICardDialogManager dialogManager) : CardUIExtension
    {
        #region Fields

        private readonly ICardDialogManager dialogManager = NotNullOrThrow(dialogManager);

        #endregion

        #region Type extension methods

        private static Task ExecuteInitializingAsync(ITypeExtensionContext typeContext)
        {
            var context = (ICardUIExtensionContext) typeContext.ExternalContext!;
            var settings = typeContext.Settings;
            var viewControlAlias = settings?.TryGet<string>(CardTypeExtensionSettings.ViewControlAlias);
            if (string.IsNullOrEmpty(viewControlAlias))
            {
                return Task.CompletedTask;
            }

            context.Model.ControlInitializers.Add((control, m, r, ct) =>
            {
                if (control is CardViewControlViewModel viewControl)
                {
                    if (viewControl.Name == viewControlAlias && viewControl.DataProvider is { } controlDataProvider)
                    {
                        var dataProvider = new TaskHistoryViewDataProvider(controlDataProvider);
                        viewControl.Refreshing += (s, e) => dataProvider.ResetCache();
                        viewControl.DataProvider = dataProvider;
                    }
                }

                return ValueTask.CompletedTask;
            });

            return Task.CompletedTask;
        }

        private Task ExecuteInitializedAsync(ITypeExtensionContext typeContext)
        {
            var context = (ICardUIExtensionContext) typeContext.ExternalContext!;

            var settings = typeContext.Settings;
            var viewControlAlias = settings?.TryGet<string>(CardTypeExtensionSettings.ViewControlAlias);
            if (string.IsNullOrEmpty(viewControlAlias))
            {
                return Task.CompletedTask;
            }

            if (context.Model.Controls.TryGet<CardViewControlViewModel>(viewControlAlias) is not { } taskHistoryView)
            {
                context.ValidationResult.AddException(this,
                    new ArgumentException($"Control ViewModel with Name='{viewControlAlias}' not found.", nameof(viewControlAlias)));
                return Task.CompletedTask;
            }

            var quickSearchIndex = taskHistoryView.TopItems.Items.IndexOf(i => i is QuickSearchViewModel);
            if (quickSearchIndex != -1)
            {
                taskHistoryView.TopItems.Items.RemoveAt(quickSearchIndex);
            }

            var clientQuickSearch = new ClientQuickSearchViewModel(taskHistoryView);
            taskHistoryView.TopItems.Items.Insert(quickSearchIndex != -1 ? quickSearchIndex : 0, clientQuickSearch);

            var expandAllButton = new ExpandAllButtonViewModel { ViewModel = taskHistoryView };
            taskHistoryView.TopItems.Add(expandAllButton);

            var collapseGroups = settings?.TryGet<bool>(CardTypeExtensionSettings.CollapseGroups) ?? false;

            var leftRowColumns = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(settings?.TryGet<string>(CardTypeExtensionSettings.LeftRowColumns)))
            {
                leftRowColumns = settings.TryGet<string>(CardTypeExtensionSettings.LeftRowColumns)?.Split(' ');
            }

            var rightRowColumns = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(settings?.TryGet<string>(CardTypeExtensionSettings.RightRowColumns)))
            {
                rightRowColumns = settings.TryGet<string>(CardTypeExtensionSettings.RightRowColumns)?.Split(' ');
            }

            var bottomColumns = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(settings?.TryGet<string>(CardTypeExtensionSettings.BottomColumns)))
            {
                bottomColumns = settings.TryGet<string>(CardTypeExtensionSettings.BottomColumns)?.Split(' ');
            }

            var taskHistoryViewHelper = new TaskHistoryViewHelper(taskHistoryView, leftRowColumns, rightRowColumns, bottomColumns, collapseGroups);
            taskHistoryView.CreateRowFunc = taskHistoryViewHelper.CreateRow;

            taskHistoryView.RowContextMenuGenerators.Add(ctx =>
            {
                ctx.MenuActions.AddRange(
                    new MenuAction(
                        TaskHistoryMenuActionNames.Copy,
                        "$UI_Common_Copy",
                        Icon.Empty,
                        new DelegateCommand(p => CopyToClipboard(ctx.RowViewModel)),
                        "Ctrl+C"),
                    new MenuAction(
                        TaskHistoryMenuActionNames.ShowDetails,
                        "$UI_Cards_TaskHistory_ShowDetails",
                        Icon.Empty,
                        new DelegateCommand(p => { this.dialogManager.ShowTaskHistoryItem(taskHistoryViewHelper.CreateDetailsDialog(ctx.RowViewModel)); })),
                    new MenuAction(
                        TaskHistoryMenuActionNames.CollapseGroups,
                        "$UI_Cards_TaskHistory_ExpandGroups",
                        Icon.Empty,
                        new DelegateCommand(p => { ExpandGroups(ctx.RowViewModel); }),
                        isCollapsed: ctx.RowViewModel.Items.Count == 0),
                    new MenuAction(
                        TaskHistoryMenuActionNames.CollapseGroups,
                        "$UI_Cards_TaskHistory_CollapseGroups",
                        Icon.Empty,
                        new DelegateCommand(p => { CollapseGroups(ctx.RowViewModel); }),
                        isCollapsed: ctx.RowViewModel.Items.Count == 0)
                );

                return new ValueTask();
            });

            taskHistoryView.MiddleClickCommand = new DelegateCommand(o =>
            {
                var clickInfo = (IViewClickInfo) o;
                var row = (ViewControlRowViewModel) clickInfo.Row;
                if (row != null)
                {
                    this.dialogManager.ShowTaskHistoryItem(taskHistoryViewHelper.CreateDetailsDialog(row));
                }
            });

            taskHistoryView.KeyDownHandlers.Add((item, data, e) =>
            {
                if (data is ViewControlRowViewModel row)
                {
                    if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        e.Handled = true;
                        CopyToClipboard(row);
                    }
                }
            });

            return Task.CompletedTask;
        }

        #endregion

        #region Private methods

        private static void CollapseGroups(TableRowViewModel row)
        {
            foreach (var item in row.Items)
            {
                CollapseGroups(item);
            }

            row.IsExpanded = false;
        }


        private static void ExpandGroups(TableRowViewModel row)
        {
            foreach (var item in row.Items)
            {
                ExpandGroups(item);
            }

            row.IsExpanded = true;
        }


        private static void CopyToClipboard(ViewControlRowViewModel row)
        {
            var sb = StringBuilderHelper.Acquire(256);

            foreach (var column in row.GridViewModel.Columns?.Cast<TableColumnViewModel>().Where(x => x.Visibility) ?? [])
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb
                    .Append(column.Header)
                    .Append(": ")
                    .Append(Localize(row.CellsByColumnName[column.ColumnName].Text));
            }

            ClipboardSafe.SetText(sb.ToStringAndRelease());
        }

        #endregion

        #region Base overrides

        public override async Task Initializing(ICardUIExtensionContext context)
        {
            var result = await CardHelper
                .ExecuteTypeExtensionsAsync(
                    DefaultCardTypeExtensionTypes.MakeViewTaskHistory,
                    context.Card,
                    context.Model.CardMetadata,
                    ExecuteInitializingAsync,
                    context,
                    cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        public override async Task Initialized(ICardUIExtensionContext context)
        {
            var result = await CardHelper
                .ExecuteTypeExtensionsAsync(
                    DefaultCardTypeExtensionTypes.MakeViewTaskHistory,
                    context.Card,
                    context.Model.CardMetadata,
                    this.ExecuteInitializedAsync,
                    context,
                    cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(result);
        }

        #endregion
    }
}
