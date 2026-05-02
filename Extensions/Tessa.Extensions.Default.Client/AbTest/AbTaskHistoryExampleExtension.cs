#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Forms;
using Tessa.UI.Data;

namespace Tessa.Extensions.Default.Client.AbTest
{
    public sealed class AbTaskHistoryExampleExtension : CardUIExtension
    {
        #region Nested Types

        private sealed class DateTimeToStringConverter : ValueConverter<DateTime, string>
        {
            #region Fields

            private Dictionary<long, string>? cachedValues;

            #endregion

            #region ValueConverter<DateTime, string> Members

            protected override string Convert(DateTime value, object parameter, CultureInfo culture)
            {
                // Получаем количество тиков, которые будем использовать в качестве ключа в кеше строк.
                // Это позволит нам обойтись без интернирования строк с его проблемами, а также
                // позволит более экономно расходовать память.
                long ticks = new DateTime(value.Year, value.Month, 1).Ticks;
                string? cachedValue;

                if (this.cachedValues == null)
                {
                    this.cachedValues = new Dictionary<long, string> { { ticks, cachedValue = value.ToString("y") } };
                }
                else
                {
                    if (!this.cachedValues.TryGetValue(ticks, out cachedValue))
                    {
                        this.cachedValues.Add(ticks, cachedValue = value.ToString("y"));
                    }
                }

                return cachedValue;
            }

            #endregion
        }

        #endregion

        #region Base Overrides

        public override async Task Initialized(ICardUIExtensionContext context)
        {
            if (context.Model.MainForm is not DefaultFormTabWithTaskHistoryViewModel mainForm)
            {
                return;
            }

            ICollectionView collectionView = CollectionViewSourceHelper.GetGlobalView(mainForm.TaskHistory.Items);

            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Created", new DateTimeToStringConverter()));
            collectionView.SortDescriptions.Add(new SortDescription("Created", ListSortDirection.Ascending));
        }

        #endregion
    }
}
