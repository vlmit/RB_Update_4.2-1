#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI
{
    /// <summary>
    /// Объект, выполняющий сортировку этапов.
    /// </summary>
    /// <remarks>
    /// Правило сортировки.<para/>
    ///
    /// Сортировка сначала разделяет все этапы по группам.
    /// Сортировка для групп происходит по паре (<see cref="Stage.StageGroupOrder"/>, <see cref="Stage.StageGroupID"/>),
    /// что позволяет получить уникальность каждого элемента и стабильность сортировки.<para/>
    ///
    /// В каждой группе проводится сортировка по следующим признакам:<para/>
    /// <see cref="GroupPosition.AtFirst"/> &amp; !<see cref="Stage.CanChangeOrder"/><para/>
    /// <see cref="GroupPosition.AtFirst"/> &amp; <see cref="Stage.CanChangeOrder"/><para/>
    /// <see cref="GroupPosition.Unspecified"/> &amp; <see cref="Stage.CanChangeOrder"/><para/>
    /// <see cref="GroupPosition.AtLast"/> &amp; <see cref="Stage.CanChangeOrder"/><para/>
    /// <see cref="GroupPosition.AtLast"/> &amp; !<see cref="Stage.CanChangeOrder"/><para/>
    ///
    /// <see cref="GroupPosition.Unspecified"/> &amp; !<see cref="Stage.CanChangeOrder"/> - положение не меняют.<para/>
    ///
    /// Внутри каждой такой подгруппы производится дополнительная сортировка по <see cref="Stage.TemplateOrder"/>.
    /// Это поле хранит TemplateOrder из карточки шаблона этапов KrStageTemplates. Данный <see cref="Stage.TemplateOrder"/>
    /// не имеет отношения к обычному Order в таблице этапов карточки.<para/>
    ///
    /// Последним ключом сортировки является <see cref="Stage.TemplateStageOrder"/> этапов из шаблона.
    /// Это необходимо для переноса порядка сортировки из дочернего маршрута шаблона в целый маршрут документа.<para/>
    ///
    /// Важным свойством является стабильность <see cref="System.Linq.Enumerable.OrderBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/> сортировки.
    /// Источник: https://docs.microsoft.com/ru-ru/dotnet/api/system.linq.enumerable.orderby абзац "комментарии", предпоследнее предложение.
    /// "This method performs a stable sort; that is, if the keys of two elements are equal, the order of the elements is preserved."
    /// Это позволяет при сортировке сохранить порядок Unspecified этапов таким, каким его указал пользователь.
    /// </remarks>
    /// <seealso href="https://tessa.ru/docs/4.1/adm/admin/routes/#template-stages"/>
    public sealed class StageOrderComparer :
        Comparer<Stage>
    {
        #region IComparer Members

        /// <inheritdoc/>
        public override int Compare(Stage? x, Stage? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var stageGroupOrderCompare = x.StageGroupOrder.CompareTo(y.StageGroupOrder);
            if (stageGroupOrderCompare != 0)
            {
                return stageGroupOrderCompare;
            }

            var stageGroupIDCompare = x.StageGroupID.CompareTo(y.StageGroupID);
            if (stageGroupIDCompare != 0)
            {
                return stageGroupIDCompare;
            }

            if (x.IsPositionUnspecified
                && y.IsPositionUnspecified)
            {
                return 0;
            }

            var groupPositionCompare = x.GroupPosition.CompareTo(y.GroupPosition);
            if (groupPositionCompare != 0)
            {
                return groupPositionCompare;
            }

            // Сравнение для сортировки этапов согласования в подгруппах "в начале", "в конце", "не определено".
            // Если GroupPosition.AtFirst, то порядок !Stage.CanChangeOrder меньше  Stage.CanChangeOrder.
            // Если GroupPosition.AtLast, то порядок   Stage.CanChangeOrder меньше !Stage.CanChangeOrder.
            // Если GroupPosition.Unspecified то       Stage.CanChangeOrder == !Stage.CanChangeOrder.
            int groupPositionCanChangeOrderCompare;
            if (x.CanChangeOrder == y.CanChangeOrder)
            {
                /*x.GroupPosition == y.GroupPosition*/
                groupPositionCanChangeOrderCompare = 0;
            }
            else if (x.GroupPosition == GroupPosition.AtFirst && !x.CanChangeOrder && y.CanChangeOrder
                || x.GroupPosition == GroupPosition.AtLast && x.CanChangeOrder && !y.CanChangeOrder)
            {
                groupPositionCanChangeOrderCompare = -1;
            }
            else
            {
                groupPositionCanChangeOrderCompare = 1;
            }

            if (groupPositionCanChangeOrderCompare != 0)
            {
                return groupPositionCanChangeOrderCompare;
            }

            var templateOrderCompare = Nullable.Compare(x.TemplateOrder, y.TemplateOrder);
            if (templateOrderCompare != 0)
            {
                return templateOrderCompare;
            }

            var templateStageOrderCompare = Nullable.Compare(x.TemplateStageOrder, y.TemplateStageOrder);
            if (templateStageOrderCompare != 0)
            {
                return templateStageOrderCompare;
            }

            return 0;
        }

        #endregion

        #region Instance Static Property

        /// <doc path='info[@type="object" and @item="Instance"]'/>
        public static StageOrderComparer Instance { get; } = new();

        #endregion
    }
}
