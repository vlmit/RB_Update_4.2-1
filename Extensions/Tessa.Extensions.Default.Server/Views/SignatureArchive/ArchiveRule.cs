using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Tessa.Cards;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.Views.SignatureArchive
{
    /// <summary>
    /// Правило архивации ЭП.
    /// </summary>
    /// <param name="archiveRules">Коллекция, частью которой является правило.</param>
    /// <param name="rule">Строка правила.</param>
    /// <param name="types">Типы карточке в правилах хранения.</param>
    /// <param name="categories">Категории файлов в правилах хранения.</param>
    public class ArchiveRule(
        ArchiveRuleCollection archiveRules,
        CardRow rule,
        IEnumerable<CardRow> types,
        IEnumerable<CardRow> categories)
    {
        #region Constants and Readonly Fields

        /// <summary>
        /// Плейсхолдер для подстановки условия проверки по идентификатору объекта.
        /// Должен содержать имя идентификатора объекта.<para/>
        /// Заменяется на <c>"Имя колонки" = "Алиас таблицы внешнего запроса (DocumentCommonInfo)".CardID</c>.<para/>
        /// </summary>
        /// <remarks>
        /// Пример использования:
        /// #when_cardid([TableName].[ID])
        /// </remarks>
        private const string whenCardIDPlaceholder = "#when_cardid";

        /// <summary>
        /// Имя группы с колонкой плейсхолдера <see cref="whenCardIDPlaceholder"/>.
        /// </summary>
        private const string whenCardIDGroup = "WhenCardID";

        /// <summary>
        /// Имя группы колонки плейсхолдера <see cref="whenCardIDPlaceholder"/>.
        /// </summary>
        private const string whenCardIDGroupKey = "CardIDKey";

        /// <summary>
        /// Плейсхолдер для подстановки условия проверки по идентификатору объекта.
        /// Должен содержать имя идентификатора объекта.<para/>
        /// Заменяется на <c>"Имя колонки" = "Алиас таблицы внешнего запроса (Files)".FileID</c>.<para/>
        /// </summary>
        /// <remarks>
        /// Пример использования:
        /// #when_fileid([TableName].[ID])
        /// </remarks>
        private const string whenFileIDPlaceholder = "#when_fileid";

        /// <summary>
        /// Имя группы с колонкой плейсхолдера <see cref="whenFileIDPlaceholder"/>.
        /// </summary>
        private const string whenFileIDGroup = "WhenFileID";

        /// <summary>
        /// Имя группы колонки плейсхолдера <see cref="whenFileIDPlaceholder"/>.
        /// </summary>
        private const string whenFileIDGroupKey = "FileIDKey";

        /// <summary>
        /// Паттерн для поиска плейсхолдеров.
        /// </summary>
        private static readonly Regex pattern = new(
            @$"(?<{whenCardIDGroup}>{whenCardIDPlaceholder}\((?<{whenCardIDGroupKey}>.+?)\))|(?<{whenFileIDGroup}>{whenFileIDPlaceholder}\((?<{whenFileIDGroupKey}>.+?)\))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private const string nearestCertValidToColumnName = "NearestCertValidTo";

        #endregion

        #region Properties

        public Guid RowID { get; } = rule.RowID;

        public int DaysBeforeExpiry { get; } = rule.Fields.Get<int>("DaysBeforeCertExpiry");

        public bool Disabled { get; } = rule.Fields.Get<bool>("Disabled");

        public string? SqlCondition { get; } = rule.Fields.TryGet<string?>("SqlCondition");

        public IEnumerable<Guid>? DocTypeIDs { get; } = types.Select(static x => x.Get<Guid>("TypeID"));

        public IEnumerable<Guid>? FileCategories { get; } = categories.Select(static x => x.Get<Guid>("CategoryID"));

        public ArchiveRuleCollection ArchiveRules { get; } = NotNullOrThrow(archiveRules);

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет в билдер запроса условие на основе правила.
        /// </summary>
        /// <param name="builder"><inheritdoc cref="IQueryBuilder" path="/summary"/></param>
        public void ToQueryExpression(IQueryBuilder builder)
        {
            builder
                .StartExpression()
                .C(this.ArchiveRules.OuterTableName, nearestCertValidToColumnName)
                .LessOrEquals()
                .V(DateTime.UtcNow.Date.AddDays(this.DaysBeforeExpiry))
                .And()
                .C(this.ArchiveRules.OuterTableName, nearestCertValidToColumnName)
                .GreaterOrEquals()
                .V(DateTime.UtcNow.Date);

            if (this.ArchiveRules.ShowNullCertValidTo)
            {
                builder
                    .Or()
                    .C(this.ArchiveRules.OuterTableName, nearestCertValidToColumnName)
                    .IsNull();
            }

            builder
                .EndExpression()
                .And()
                .C(this.ArchiveRules.OuterTableName, "CardTypeID").In(this.DocTypeIDs);

            if (this.FileCategories.Any())
            {
                builder
                    .And()
                    .C(this.ArchiveRules.OuterTableName, "CategoryID").In(this.FileCategories);
            }

            if (string.IsNullOrEmpty(this.SqlCondition))
            {
                return;
            }

            builder
                .And()
                .Exists(e =>
                {
                    var matches = pattern.Matches(this.SqlCondition);
                    var currentIndex = 0;

                    for (var i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];

                        if (!match.Success)
                        {
                            continue;
                        }

                        if (currentIndex != match.Index)
                        {
                            e.Append(this.SqlCondition[currentIndex..match.Index]);
                        }

                        currentIndex = match.Index + match.Length;

                        if (match.Groups[whenCardIDGroup].Success)
                        {
                            e
                            .C(this.ArchiveRules.OuterTableName, "CardID")
                            .Equals()
                            .Append(match.Groups[whenCardIDGroupKey].Value);
                        }
                        else if (match.Groups[whenFileIDGroup].Success)
                        {
                            e
                            .C(this.ArchiveRules.OuterTableName, "FileID")
                            .Equals()
                            .Append(match.Groups[whenFileIDGroupKey].Value);
                        }
                    }

                    if (currentIndex < this.SqlCondition.Length)
                    {
                        e.Append(this.SqlCondition[currentIndex..]);
                    }
                });
        }

        #endregion
    }
}
