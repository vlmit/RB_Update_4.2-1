#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tessa.Platform;
using Tessa.Platform.Formatting;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Вспомогательные методы для работы с текстом.
    /// </summary>
    public static class TestTextHelper
    {
        #region Public Methods

        /// <summary>
        /// Объединяет значения из <paramref name="values"/> так, что каждое из них выводится с новой строки. Если <paramref name="values"/> не содержит элементов, то возвращает <see cref="FormattingHelper.EmptyText"/>.
        /// </summary>
        /// <typeparam name="T">Тип значений.</typeparam>
        /// <param name="values">Значения.</param>
        /// <returns>Срока, содержащая объединённые значения.</returns>
        public static string JoinOrEmpty<T>(IEnumerable<T> values)
        {
            ThrowIfNull(values);

            var str = string.Join(Environment.NewLine, values);
            return string.IsNullOrEmpty(str) ? FormattingHelper.EmptyText : str;
        }

        /// <summary>
        /// Добавляет таблицу в <paramref name="sb"/>.
        /// </summary>
        /// <param name="sb"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="rows">Строки.</param>
        /// <param name="headers">Заголовки столбцов.</param>
        /// <param name="settings">Параметры или значение <see langword="null"/>, если используются параметры по умолчанию.</param>
        /// <remarks>Строки могу содержать разное число элементов. Элементы c порядковым номером большим чем число элементов в <paramref name="headers"/> игнорируются.</remarks>
        public static void PrintTable(
            StringBuilder sb,
            IReadOnlyCollection<IReadOnlyCollection<string?>> rows,
            IReadOnlyCollection<string> headers,
            TableSettings? settings = null)
        {
            ThrowIfNull(sb);
            ThrowIfNull(rows);
            ThrowIfNull(headers);
            settings?.Validate();

            var headersCount = headers.Count;
            if (headersCount == 0)
            {
                return;
            }

            settings ??= new();

            // Вычисление ширины столбцов.
            var columnsWidth = GetColumnsWidth(
                rows,
                headers,
                settings);

            // columnSeparator.Length + two whitespaces = 3
            var totalTableWidth = 3 * (headersCount - 1) + columnsWidth.Sum();

            // Заголовок таблицы.
            AddRow(
                sb,
                headersCount,
                headers,
                settings,
                columnsWidth);

            sb
                .AppendLine()
                .Append(settings.LineSeparator, totalTableWidth);

            // Тело таблицы.
            foreach (var row in rows)
            {
                sb.AppendLine();

                AddRow(
                    sb,
                    headersCount,
                    row,
                    settings,
                    columnsWidth);
            }

            sb.AppendLine();
        }

        #endregion

        #region Private Methods

        private static int[] GetColumnsWidth(
            IReadOnlyCollection<IReadOnlyCollection<string?>> rows,
            IReadOnlyCollection<string> headers,
            TableSettings settings)
        {
            var headersCount = headers.Count;
            var columnsWidth = new int[headersCount];

            var i = 0;
            foreach (var header in headers)
            {
                columnsWidth[i] = Math.Min(
                    Math.Max(settings.MinColumnWidth, header.Length),
                    settings.MaxColumnWidth);

                i++;
            }

            foreach (var row in rows)
            {
                var columnsCount = Math.Min(row.Count, headersCount);
                var hasMaxLength = false;
                i = -1;

                foreach (var value in row)
                {
                    i++;

                    if (i >= columnsCount)
                    {
                        break;
                    }

                    var valueWidth = value?.Length ?? 0;

                    if (valueWidth <= columnsWidth[i])
                    {
                        continue;
                    }

                    if (valueWidth < settings.MaxColumnWidth)
                    {
                        columnsWidth[i] = valueWidth;
                        continue;
                    }

                    columnsWidth[i] = settings.MaxColumnWidth;
                    hasMaxLength = true;
                }

                if (hasMaxLength
                    && headersCount == columnsWidth.Count(x => x == settings.MaxColumnWidth))
                {
                    break;
                }
            }

            return columnsWidth;
        }

        private static void AddRow(
            StringBuilder sb,
            int columnsCount,
            IEnumerable<string?> values,
            TableSettings settings,
            int[] columnsWidth)
        {
            using var en = values.GetEnumerator();

            if (!en.MoveNext())
            {
                return;
            }

            var columnWidth = columnsWidth[0];
            var value = en.Current.Limit(columnWidth);

            sb.Append(value);

            for (var i = 1; i < columnsCount; i++)
            {
                // Дополнение значения до ширины столбца + один пробел перед разделителем столбцов.
                var valueAlignment = columnWidth - (value?.Length ?? 0) + 1;

                sb
                    .Append(' ', valueAlignment)
                    .Append(settings.ColumnSeparator);

                columnWidth = columnsWidth[i];
                value = en.MoveNext()
                    ? en.Current.Limit(columnWidth)
                    : null;

                if (!string.IsNullOrEmpty(value)
                    || i < columnsCount - 1)
                {
                    sb
                        .Append(' ')
                        .Append(value);
                }
            }
        }

        #endregion
    }
}
