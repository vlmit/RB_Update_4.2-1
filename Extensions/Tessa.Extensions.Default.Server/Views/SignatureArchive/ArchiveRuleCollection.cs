using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Cards;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Views;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Views.SignatureArchive
{
    /// <summary>
    /// Коллекция правил архивации ЭП.
    /// </summary>
    public class ArchiveRuleCollection : List<ArchiveRule>
    {
        #region Constructor

        /// <summary>
        /// <inheritdoc cref="ArchiveRuleCollection"/>
        /// </summary>
        /// <param name="rules">Список строк с правилами архивации.</param>
        /// <param name="types">Список строк с типами документов.</param>
        /// <param name="categories">Список строк с категориями файлов.</param>
        /// <param name="viewRequest"><inheritdoc cref="ITessaViewRequest" path="/summary"/></param>
        /// <param name="outerTableName">Название таблицы, по которой джоинится условие.</param>
        public ArchiveRuleCollection(
            IEnumerable<CardRow> rules,
            IEnumerable<CardRow> types,
            IEnumerable<CardRow> categories,
            ITessaViewRequest viewRequest,
            string outerTableName)
        {
            this.OuterTableName = outerTableName;
            this.ViewRequest = NotNullOrThrow(viewRequest);
            this.ShowNullCertValidTo = viewRequest.Parameters.Any(
                p => p.Name == "NearestCertValidTo" && p.CriteriaValues.Any(c => c.CriteriaName == IsNullCriteriaOperator.Instance.Name));

            foreach (var rule in rules)
            {
                this.Add(new(
                    this,
                    rule,
                    types.Where(x => x.Fields.TryGet<Guid>("ArchiveRuleRowID") == rule.RowID),
                    categories.Where(x => x.Fields.TryGet<Guid>("ArchiveRuleRowID") == rule.RowID)));
            }
        }

        #endregion

        #region Properties

        public string OuterTableName { get; init; }

        public ITessaViewRequest ViewRequest { get; init; }

        public bool ShowNullCertValidTo { get; init; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет в билдер запроса условия из правил.
        /// </summary>
        /// <param name="builder"><inheritdoc cref="IQueryBuilder" path="/summary"/></param>
        public void ToQueryExpression(IQueryBuilder builder)
        {
            var activeRules = this.Where(x => !x.Disabled).ToArray();

            if (activeRules.Length == 0)
            {
                builder.V(false).Equals().V(true);
                return;
            }

            builder.E(activeRules[0].ToQueryExpression);

            for (int i = 1; i < activeRules.Length; i++)
            {
                builder
                    .Or()
                    .E(activeRules[i].ToQueryExpression);
            }
        }

        #endregion
    }
}
