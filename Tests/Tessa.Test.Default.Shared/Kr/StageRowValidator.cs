#nullable enable

using System;
using NUnit.Framework;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Test.Default.Shared.Kr.Routes;
using Tessa.Test.Default.Shared.Validation;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Объект, предоставляющий методы для проверки значений полей <see cref="CardRow"/> этапов маршрутов.
    /// </summary>
    public sealed class StageRowValidator :
        RowValidatorBase<StageRowValidator>
    {
        #region Public Methods

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.RowChanged"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckRowChanged(bool expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.RowChanged,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.OrderChanged"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckOrderChanged(bool expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.OrderChanged,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.BasedOnStageTemplateGroupPositionID"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageTemplateGroupPosition(GroupPosition expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.BasedOnStageTemplateGroupPositionID,
                Is.EqualTo(expectedValue.ID));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.BasedOnStageRowID"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageRowID(Guid? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.BasedOnStageRowID,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.BasedOnStageTemplateOrder"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageTemplateOrder(int? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.BasedOnStageTemplateOrder,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.BasedOnStageTemplateID"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageTemplateID(Guid? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.BasedOnStageTemplateID,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.BasedOnStageTemplateName"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageTemplateName(string? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.BasedOnStageTemplateName,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.StageGroupID"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckStageGroupID(Guid? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.StageGroupID,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.StageGroupName"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckStageGroupName(string? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.StageGroupName,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.StageGroupOrder"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckStageGroupOrder(int? expectedValue)
        {
            return this.Check(
                KrConstants.KrStages.StageGroupOrder,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <see cref="KrConstants.KrStages.StageGroupOrder"/>.
        /// </summary>
        /// <param name="builder"><inheritdoc cref="KrStageGroupBuilder" path="/summary"/></param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckStageGroup(KrStageGroupBuilder builder)
        {
            ThrowIfNull(builder);

            return this
                .CheckStageGroupID(builder.CardID)
                .CheckStageGroupName(builder.GetName())
                .CheckStageGroupOrder(builder.GetOrder())
                ;
        }

        /// <summary>
        /// Проверяет значения полей для случая отсутствия изменений в порядке этапа.
        ///
        /// <list type="table">
        /// <listheader>
        ///     <description>Поле</description>
        ///     <description>Ожидаемое значение</description>
        /// </listheader>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateID"/></description>
        ///     <description><see cref="ICardLifecycleCompanion.CardID"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateName"/></description>
        ///     <description><see cref="KrStageTemplateBuilder.GetName"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateOrder"/></description>
        ///     <description><see cref="KrStageTemplateBuilder.GetOrder"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateGroupPositionID"/></description>
        ///     <description><see cref="KrStageTemplateBuilder.GetGroupPosition"/></description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="template"><inheritdoc cref="KrStageTemplateBuilder" path="/summary"/></param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageTemplate(
            KrStageTemplateBuilder template)
        {
            ThrowIfNull(template);

            return this
                .CheckBasedOnStageTemplateID(template.CardID)
                .CheckBasedOnStageTemplateName(template.GetName())
                .CheckBasedOnStageTemplateOrder(template.GetOrder())
                .CheckBasedOnStageTemplateGroupPosition(template.GetGroupPosition())
                ;
        }

        /// <summary>
        /// Проверяет значения полей для случая изменения порядка этапа.
        ///
        /// <list type="table">
        /// <listheader>
        ///     <description>Поле</description>
        ///     <description>Ожидаемое значение</description>
        /// </listheader>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateID"/></description>
        ///     <description><see cref="ICardLifecycleCompanion.CardID"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateName"/></description>
        ///     <description><see cref="KrStageTemplateBuilder.GetName"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateOrder"/></description>
        ///     <description><see langword="null"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateGroupPositionID"/></description>
        ///     <description><see langword="null"/></description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="template"><inheritdoc cref="KrStageTemplateBuilder" path="/summary"/></param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public StageRowValidator CheckBasedOnStageTemplateWhenOrderChanged(
            KrStageTemplateBuilder template)
        {
            ThrowIfNull(template);

            return this
                .CheckBasedOnStageTemplateID(template.CardID)
                .CheckBasedOnStageTemplateName(template.GetName())
                .CheckBasedOnStageTemplateOrder(null)
                .CheckBasedOnStageTemplateGroupPosition(GroupPosition.Unspecified)
                ;
        }

        /// <summary>
        /// Проверяет значения полей для случая, когда этап не связан с шаблоном этапов.
        ///
        /// <list type="table">
        /// <listheader>
        ///     <description>Поле</description>
        ///     <description>Ожидаемое значение</description>
        /// </listheader>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateID"/></description>
        ///     <description><see langword="null"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateName"/></description>
        ///     <description><see langword="null"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateOrder"/></description>
        ///     <description><see langword="null"/></description>
        /// </item>
        /// <item>
        ///     <description><see cref="KrConstants.KrStages.BasedOnStageTemplateGroupPositionID"/></description>
        ///     <description><see langword="null"/></description>
        /// </item>
        /// </list>
        /// </summary>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        /// <remarks>В текущей версии данная ситуация возможна только для этапа добавленного вручную.</remarks>
        public StageRowValidator CheckBasedOnStageTemplateWhenUnbindTemplate()
        {
            return this
                .CheckBasedOnStageTemplateID(null)
                .CheckBasedOnStageTemplateName(null)
                .CheckBasedOnStageTemplateOrder(null)
                .CheckBasedOnStageTemplateGroupPosition(GroupPosition.Unspecified)
                ;
        }

        #endregion
    }
}
