using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Themes;
using Tessa.UI;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Views.Content;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Client.UI.TaskHistory
{
    internal class TaskHistoryViewHelper
    {
        #region Private Fields & Nested Classes

        private readonly List<ColumnInfo> leftRowContent = [];

        private readonly List<ColumnInfo> rightRowContent = [];

        private readonly List<ColumnInfo> bottomRowContent = [];

        private class ColumnInfo(string alias, string caption, bool localizable)
        {
            public bool Localizable { get; } = localizable;

            public string Alias { get; } = alias;

            public string Caption { get; } = caption;
        }

        private readonly CardViewControlViewModel taskHistoryView;

        public ViewControlRowViewModel CreateRow(TableRowCreationOptions options)
        {
            var row = new ViewControlRowViewModel(options);
            var toolTip = new ToolTip();
            toolTip.Style = UIHelper.CreateToolTipStyle(toolTip, 700);
            toolTip.Content = this.GetToolTipContent(row.Data);
            row.IsExpanded = !this.CollapseGroups;
            row.ToolTip = toolTip;

            var cardInfo = this.taskHistoryView.ViewMetadata?.TryGetCardIdInfo()
                ?? this.taskHistoryView.ViewMetadata?.TryGetViewReferenceInfo();

            if (cardInfo is { } cardInfoValue && row.Data.TryGetValue(cardInfoValue.ColumnId, out var columnId))
            {
                row.AutomationId = $"{cardInfoValue.ColumnId}:{columnId}";
                var columnMetadata = this.taskHistoryView.ViewMetadata?.Columns.First(col => col.Alias == cardInfoValue.ColumnDisplayName);
                row.AutomationName = $"{ViewRowHelper.ConvertCellValue(columnMetadata, row.Data.TryGet<object>(cardInfoValue.ColumnDisplayName))}";
            }
            else
            {
                row.AutomationId = $"{row.Data["RowID"]}";
                row.AutomationName = string.Join('|', row.Data.Values);
            }

            return row;
        }

        #endregion

        #region Constructor

        public TaskHistoryViewHelper(
            CardViewControlViewModel taskHistoryView,
            IEnumerable<string> leftRowColumns,
            IEnumerable<string> rightRowColumns,
            IEnumerable<string> bottomColumns,
            bool collapseGroups
        )
        {
            this.CollapseGroups = collapseGroups;
            this.taskHistoryView = taskHistoryView;
            var columns = taskHistoryView.ViewMetadata.Columns;
            this.ResultColumnExists = columns.Any(x => x.Alias == "Result");
            if (this.ResultColumnExists)
            {
                this.ResultColumnVisible = !columns.First(x => x.Alias == "Result").Hidden;
            }

            foreach (var column in leftRowColumns)
            {
                var columnInfo = columns.First(x => x.Alias == column);
                this.leftRowContent.Add(new ColumnInfo(column, Localize(columnInfo.Caption), columnInfo.Localizable));
            }

            foreach (var column in rightRowColumns)
            {
                var columnInfo = columns.First(x => x.Alias == column);
                this.rightRowContent.Add(new ColumnInfo(column, Localize(columnInfo.Caption), columnInfo.Localizable));
            }

            foreach (var column in bottomColumns)
            {
                var columnInfo = columns.First(x => x.Alias == column);
                this.bottomRowContent.Add(new ColumnInfo(column, Localize(columnInfo.Caption), columnInfo.Localizable));
            }
        }

        #endregion

        #region Public Properties

        public bool ResultColumnExists { get; }
        public bool ResultColumnVisible { get; set; }

        public bool CollapseGroups { get; set; }

        public CardViewControlViewModel TaskHistoryView => this.taskHistoryView;

        #endregion

        #region Public Methods

        public TaskHistoryViewDetailsViewModel CreateDetailsDialog(ViewControlRowViewModel row)
        {
            var rowData = row.Data;
            var detailsDialog = new TaskHistoryViewDetailsViewModel();
            foreach (var item in this.leftRowContent)
            {
                detailsDialog.LeftRow.Add(new TaskHistoryViewDetailsElementViewModel(
                    item.Alias,
                    item.Caption,
                    FormatValue(rowData[item.Alias], item.Localizable),
                    "LeftRow"));
            }

            foreach (var item in this.rightRowContent)
            {
                detailsDialog.RightRow.Add(new TaskHistoryViewDetailsElementViewModel(
                    item.Alias,
                    item.Caption,
                    FormatValue(rowData[item.Alias], item.Localizable),
                    "RightRow"));
            }

            foreach (var item in this.bottomRowContent)
            {
                detailsDialog.BottomElements.Add(new TaskHistoryViewDetailsElementViewModel(
                    item.Alias,
                    item.Caption,
                    FormatValue(rowData[item.Alias], item.Localizable),
                    "BottomRow"));
            }

            return detailsDialog;
        }

        public void ChangeResultColumnVisibility()
        {
            if (!this.ResultColumnExists)
            {
                return;
            }

            if (this.taskHistoryView.Table.Columns?.Cast<TableColumnViewModel>()
                    .First(x => x.ColumnName == "Result") is { } column)
            {
                column.Visibility = !this.ResultColumnVisible;
            }

            this.ResultColumnVisible = !this.ResultColumnVisible;
        }

        #endregion

        #region Private Methods

        private static string FormatValue(object value, bool localizable) =>
            value is DateTime dt ? FormatDateTimeWithoutSeconds(dt) : FormatToString(value, localizable);

        private object GetToolTipContent(IDictionary<string, object> rowData)
        {
            Brush captionBrush = ThemeManager.Current.Theme.CreateSolidColorBrush(ThemeProperty.CardControlCaption);
            Brush textBrush = Brushes.Black;
            const double fontSize = 13.0;

            // общая информация сверху
            string topText = LocalizeName("UI_Cards_TaskHistory_ToolTipHeader");

            var topTextBlock = new TextBlock
            {
                Text = topText,
                Foreground = captionBrush,
                FontSize = fontSize,
                Margin = new Thickness(0.0, 0.0, 0.0, 15.0),
            };

            Grid.SetRow(topTextBlock, 0);
            Grid.SetColumn(topTextBlock, 0);
            Grid.SetColumnSpan(topTextBlock, 4);

            // информация в левой колонке
            var builder = StringBuilderHelper.Acquire(256);

            #region Initializing left row content

            for (int i = 0; i < this.leftRowContent.Count; i++)
            {
                if (i != this.leftRowContent.Count() - 1)
                {
                    builder.AppendLine(this.leftRowContent[i].Caption);
                }
                else
                {
                    builder.Append(this.leftRowContent[i].Caption);
                }
            }

            var leftLabelTextBlock = new TextBlock
            {
                Text = builder.ToString(),
                Foreground = captionBrush,
                FontSize = fontSize,
            };

            Grid.SetRow(leftLabelTextBlock, 1);
            Grid.SetColumn(leftLabelTextBlock, 0);

            builder.Clear();
            for (int i = 0; i < this.leftRowContent.Count; i++)
            {
                if (i != this.leftRowContent.Count() - 1)
                {
                    builder.AppendLine(FormatValue(
                        rowData[this.leftRowContent[i].Alias],
                        this.leftRowContent[i].Localizable));
                }
                else
                {
                    builder.Append(FormatValue(
                        rowData[this.leftRowContent[i].Alias],
                        this.leftRowContent[i].Localizable));
                }
            }


            var leftValueTextBlock = new TextBlock
            {
                Text = builder.ToString(),
                Foreground = textBrush,
                FontSize = fontSize,
                Margin = new Thickness(15.0, 0.0, 50.0, 0.0),
            };

            Grid.SetRow(leftValueTextBlock, 1);
            Grid.SetColumn(leftValueTextBlock, 1);

            #endregion

            builder.Clear();

            #region Initializing right row content

            for (int i = 0; i < this.rightRowContent.Count; i++)
            {
                if (i != this.rightRowContent.Count() - 1)
                {
                    builder.AppendLine(this.rightRowContent[i].Caption);
                }
                else
                {
                    builder.Append(this.rightRowContent[i].Caption);
                }
            }

            var rightLabelTextBlock = new TextBlock
            {
                Text = builder.ToString(),
                Foreground = captionBrush,
                FontSize = fontSize,
            };

            Grid.SetRow(rightLabelTextBlock, 1);
            Grid.SetColumn(rightLabelTextBlock, 2);

            builder.Clear();
            for (int i = 0; i < this.rightRowContent.Count; i++)
            {
                if (i != this.rightRowContent.Count() - 1)
                {
                    builder.AppendLine(FormatValue(
                        rowData[this.rightRowContent[i].Alias],
                        this.rightRowContent[i].Localizable));
                }
                else
                {
                    builder.Append(FormatValue(
                        rowData[this.rightRowContent[i].Alias],
                        this.rightRowContent[i].Localizable));
                }
            }

            var rightValueTextBlock = new TextBlock
            {
                Text = builder.ToString(),
                Foreground = textBrush,
                FontSize = fontSize,
                Margin = new Thickness(15.0, 0.0, 0.0, 0.0),
            };

            Grid.SetRow(rightValueTextBlock, 1);
            Grid.SetColumn(rightValueTextBlock, 3);

            #endregion

            builder.Release();

            // собираем грид
            var result = new Grid();

            result.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            result.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            result.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            result.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            result.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            result.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            result.Children.Add(topTextBlock);
            result.Children.Add(leftLabelTextBlock);
            result.Children.Add(leftValueTextBlock);
            result.Children.Add(rightLabelTextBlock);
            result.Children.Add(rightValueTextBlock);

            int currentRow = 2;
            foreach (var culumnInfo in this.bottomRowContent)
            {
                result.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                result.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var bottomLabelTextBlock = new TextBlock
                {
                    Text = culumnInfo.Caption,
                    Foreground = captionBrush,
                    FontSize = fontSize,
                    Margin = new Thickness(0.0, 5.0, 0.0, 0.0)
                };
                Grid.SetRow(bottomLabelTextBlock, currentRow++);
                Grid.SetColumn(bottomLabelTextBlock, 0);
                Grid.SetColumnSpan(bottomLabelTextBlock, 4);
                result.Children.Add(bottomLabelTextBlock);

                var bottomTextBlock = new TextBlock
                {
                    Text = FormatValue(rowData[culumnInfo.Alias], culumnInfo.Localizable),
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Foreground = textBrush,
                    FontSize = fontSize,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MaxWidth = 550.0,
                    Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
                };
                Grid.SetRow(bottomTextBlock, currentRow++);
                Grid.SetColumn(bottomTextBlock, 0);
                Grid.SetColumnSpan(bottomTextBlock, 4);
                result.Children.Add(bottomTextBlock);
            }

            return result;
        }

        #endregion
    }
}
