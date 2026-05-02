#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.AbTest
{
    /// <summary>
    /// Пример программного представления.
    /// </summary>
    public sealed class AbClientProgramView(IViewParameterFormatter parameterFormatter) : ITessaView
    {
        #region Nested Types

        private sealed class DataRow(string[] names, string[] cities, string[] streets, string[] creators)
        {
            private Guid ID { get; } = Guid.NewGuid();

            public string Name { get; } = names[Random.Shared.Next(names.Length)];

            private string City { get; } = cities[Random.Shared.Next(cities.Length)];

            private string Street { get; } = streets[Random.Shared.Next(streets.Length)];

            private int Number { get; } = Random.Shared.Next();

            private DateTime CreationDate { get; } = DateTime.Now;

            private string Creator { get; } = creators[Random.Shared.Next(creators.Length)];

            public List<object?> GetRow() => [this.ID, this.Name, this.City, this.Street, this.Number, this.CreationDate, this.Creator];
        }

        private abstract class FilterOperation
        {
            public abstract bool IsSatisfied(DataRow row);
        }

        private sealed class NoFilterOperation : FilterOperation
        {
            public override bool IsSatisfied(DataRow row) => true;
        }

        private sealed class EqualsFilterOperation : FilterOperation
        {
            public required string? Text { get; init; }

            public override bool IsSatisfied(DataRow row) =>
                string.Equals(row.Name, this.Text, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ContainsFilterOperation : FilterOperation
        {
            public required string? Text { get; init; }

            public override bool IsSatisfied(DataRow row) =>
                !string.IsNullOrEmpty(this.Text)
                && row.Name.Contains(this.Text, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class BetweenFilterOperation : FilterOperation
        {
            public required string? StartText { get; init; }

            public required string? EndText { get; init; }

            public override bool IsSatisfied(DataRow row) =>
                string.Compare(this.StartText, row.Name, StringComparison.OrdinalIgnoreCase) <= 0
                && string.Compare(this.EndText, row.Name, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region Constants and Fields

        private readonly IViewParameterFormatter parameterFormatter = NotNullOrThrow(parameterFormatter);

        /// <summary>
        /// Алиас представления
        /// </summary>
        private const string ClientProgramViewAlias = "AbClientProgramView";

        private static readonly string[] names = ["ООО Одуванчик", "ОАО Фиалка", "ОАО Фиалка-1", "ОАО Фиалка-2", "ЗАО Георгин", "ЗАО Георгины", "ООО Гвоздика", "ЧП Роза"];

        private static readonly string[] cities = ["Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Ростов-на-Дону"];

        private static readonly string[] streets = ["ул. Васильков", "ул. Лютиков", "ул. им. Сирени", "ул. им Дефенбахии", "пер. Розовый"];

        private static readonly string[] creators = ["Admin", "System", "Иванов", "Петров", "Сидоров"];

        /// <summary>
        /// Данные представления.
        /// Ленивая инициализация используется только для уменьшения времени создания представления.
        /// </summary>
        private readonly Lazy<IEnumerable<DataRow>> data = new(GenerateRandomData, LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Метаданные представления.
        /// Ленивая инициализация используется только уменьшения времени создания данного элемента.
        /// </summary>
        private readonly Lazy<IViewMetadata> metadata = new(CreateViewMetadata, LazyThreadSafetyMode.PublicationOnly);

        #endregion

        #region Private Methods

        private static ViewMetadata CreateViewMetadata()
        {
            var metadata = new ViewMetadata { Alias = ClientProgramViewAlias, Caption = "Client program view" };

            metadata.Columns.Add(new ViewColumnMetadata { Alias = "CustomerID", Caption = "ID", SchemeType = SchemeType.Guid });
            metadata.Columns.Add(new ViewColumnMetadata { Alias = "CustomerName", Caption = "Customer", SchemeType = SchemeType.String });
            metadata.Columns.Add(new ViewColumnMetadata { Alias = "City", Caption = "City", SchemeType = SchemeType.String });
            metadata.Columns.Add(new ViewColumnMetadata { Alias = "Street", Caption = "Street", SchemeType = SchemeType.String });
            metadata.Columns.Add(new ViewColumnMetadata { Alias = "Number", Caption = "Number", SchemeType = SchemeType.Int32 });
            metadata.Columns.Add(new ViewColumnMetadata { Alias = "CreationDate", Caption = "Creation date", SchemeType = SchemeType.DateTime });
            metadata.Columns.Add(new ViewColumnMetadata { Alias = "Creator", Caption = "Creator", SchemeType = SchemeType.String });

            metadata.Parameters.Add(
                new ViewParameterMetadata
                {
                    Alias = "Name",
                    Caption = "Name",
                    Multiple = false,
                    RefSection = { "Customer" },
                    AllowedOperands = { CriteriaOperatorConst.Contains, CriteriaOperatorConst.EqualsTo, CriteriaOperatorConst.Between },
                    SchemeType = SchemeType.String
                });

            metadata.References.Add(
                new ViewReferenceMetadata
                {
                    ColPrefix = "Customer",
                    DisplayValueColumn = "CustomerName",
                    RefSection = { "customer", "partner" }
                });

            metadata.Seal();
            return metadata;
        }

        private static List<DataRow> GenerateRandomData()
        {
            var result = new List<DataRow>();
            var count = Random.Shared.Next(100);
            for (int i = 0; i < count; i++)
            {
                result.Add(new(names, cities, streets, creators));
            }

            return result;
        }

        /// <summary>
        /// Возвращает операцию фильтрации представлению.
        /// Поддерживаются операции: равенство, диапазон, содержимое.
        /// Для остальных операций будет возвращен весь список элементов
        /// </summary>
        /// <param name="request">Запрос к представлению.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Операция фильтрации.</returns>
        private async ValueTask<FilterOperation> GetFilteringOperationAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var noFilterOperation = new NoFilterOperation();
            if (request.Parameters.FindByName("Name") is not { } parameter
                || parameter.CriteriaValues.FirstOrDefault() is not { } criteria
                || this.metadata.Value.Parameters.FindByName(parameter.Name) is not { } parameterMetadata)
            {
                return noFilterOperation;
            }

            return criteria.CriteriaName switch
            {
                CriteriaOperatorConst.EqualsTo => new EqualsFilterOperation
                {
                    Text = await this.parameterFormatter.FormatAsync(criteria.Values[0], parameterMetadata, cancellationToken).ConfigureAwait(false)
                },
                CriteriaOperatorConst.Between => new BetweenFilterOperation
                {
                    StartText = await this.parameterFormatter.FormatAsync(criteria.Values[0], parameterMetadata, cancellationToken).ConfigureAwait(false),
                    EndText = await this.parameterFormatter.FormatAsync(criteria.Values[1], parameterMetadata, cancellationToken).ConfigureAwait(false)
                },
                CriteriaOperatorConst.Contains => new ContainsFilterOperation
                {
                    Text = await this.parameterFormatter.FormatAsync(criteria.Values[0], parameterMetadata, cancellationToken).ConfigureAwait(false)
                },
                _ => noFilterOperation
            };
        }

        /// <summary>
        /// Осуществляет фильтрацию данных <paramref name="data"/> в соответствии
        /// с операцией фильтрации <paramref name="filterOperation"/>.
        /// </summary>
        /// <param name="filterOperation">Операция фильтрации.</param>
        /// <param name="data">Список входящих данных.</param>
        /// <returns>Результат отбора данных.</returns>
        private TessaViewResult GetResult(FilterOperation filterOperation, IEnumerable<DataRow> data) =>
            new(this.metadata.Value)
            {
                Rows = data.Where(filterOperation.IsSatisfied).Select(x => x.GetRow()).ToList()
            };

        #endregion

        #region ITessaView Members

        /// <inheritdoc/>
        public string Alias => ClientProgramViewAlias;

        /// <inheritdoc />
        public ValueTask<IViewMetadata> GetMetadataAsync(CancellationToken cancellationToken = default) =>
            new(this.metadata.Value);

        /// <inheritdoc />
        public async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default) =>
            this.GetResult(await this.GetFilteringOperationAsync(request, cancellationToken).ConfigureAwait(false), this.data.Value);

        #endregion
    }
}
