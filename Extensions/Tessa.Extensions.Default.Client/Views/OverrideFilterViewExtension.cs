#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Tessa.Platform.Runtime;
using Tessa.UI;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.MessagingServices.Commands;
using Tessa.UI.Views.MessagingServices.Queries;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Расширение, переопределяющее диалог фильтрации представления.
    /// </summary>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="advancedFilterViewDialogManager"><inheritdoc cref="IAdvancedFilterViewDialogManager" path="/summary"/></param>
    /// <param name="filterViewDialogDescriptorRegistry"><inheritdoc cref="IFilterViewDialogDescriptorRegistry" path="/summary"/></param>
    /// <param name="parametersConverter"><inheritdoc cref="IParameterContentConverter" path="/summary"/></param>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="OverrideFilterViewExtensionConfigurator"/>.
    /// </remarks>
    public class OverrideFilterViewExtension(
        ISession session,
        IAdvancedFilterViewDialogManager advancedFilterViewDialogManager,
        IFilterViewDialogDescriptorRegistry filterViewDialogDescriptorRegistry,
        IParameterContentConverter parametersConverter)
        : IWorkplaceViewComponentExtension
    {
        #region Fields

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IAdvancedFilterViewDialogManager advancedFilterViewDialogManager = NotNullOrThrow(advancedFilterViewDialogManager);

        private readonly IFilterViewDialogDescriptorRegistry filterViewDialogDescriptorRegistry = NotNullOrThrow(filterViewDialogDescriptorRegistry);

        private readonly IParameterContentConverter parametersConverter = NotNullOrThrow(parametersConverter);

        #endregion

        #region Private Methods

        private FilterButtonContent CreateFilterButtonContent(
            FilterViewDialogDescriptor descriptor,
            IWorkplaceViewComponent component,
            IEnumerable<IPlaceArea>? placeAreas = null,
            Func<IPlaceArea, DataTemplate>? dataTemplateFunc = null,
            int ordering = PlacementOrdering.BeforeAll) =>
            new(new DelegateCommand(
                    async _ => await this.advancedFilterViewDialogManager.OpenAsync(descriptor, component.Parameters, CancellationToken.None),
                    _ => component.SubmitQuery(new CanFilterQuery(component.Parameters.Metadata))),
                component.Parameters, placeAreas, dataTemplateFunc, ordering);

        private FilterTextViewContent CreateFilterTextViewContent(
            FilterViewDialogDescriptor descriptor,
            IWorkplaceViewComponent component,
            IParameterContentConverter parametersConverter,
            IEnumerable<IPlaceArea>? placeAreas = null,
            Func<IPlaceArea, DataTemplate>? dataTemplateFunc = null,
            int ordering = PlacementOrdering.BeforeAll) =>
            new(filterCommand: new DelegateCommand(
                    async _ => await this.advancedFilterViewDialogManager.OpenAsync(descriptor, component.Parameters, CancellationToken.None),
                    _ => component.SubmitQuery(new CanFilterQuery(component.Parameters.Metadata))),
                clearFilterCommand: new DelegateCommand(
                    async _ => await component.SubmitCommandAsync(new ClearParametersCommand(component.Parameters)),
                    _ => component.SubmitQuery(new CanClearParametersQuery(component.Parameters))),
                component.Parameters, parametersConverter, placeAreas, dataTemplateFunc, ordering);

        #endregion

        #region IWorkplaceViewComponentExtension Members

        /// <inheritdoc/>
        public void Initialize(IWorkplaceViewComponent model)
        {
            // В TessaAdmin специально созданный диалог не будет работать из-за отсутствия рабочей реализации IUIHost (используется FakeUIHost).
            // Для тестирования представление в TessaAdmin в режиме "Просмотр", не переопределяем делегат
            // отвечающий за создание кнопки отображения диалога настройки параметров фильтрации.
            if (this.session.Token?.ApplicationID == ApplicationIdentifiers.TessaAdmin
                || this.filterViewDialogDescriptorRegistry.TryGet(model.Id) is not { } descriptor)
            {
                return;
            }

            // Замена стандартного диалога настройки параметров фильтрации, вызываемого при нажатии на соответствующую кнопку,
            // расположенную над представлением, на заданный.
            model.ContentFactories[StandardViewComponentContentItemFactory.FilterButton] = c =>
                this.CreateFilterButtonContent(descriptor, c);

            // Замена стандартного диалога настройки параметров фильтрации, вызываемого при нажатии на соответствующую кнопку,
            // расположенную над представлением при применённом фильтре (отображается при наведении указателя на область текущих параметров фильтрации), на заданный.
            model.ContentFactories[StandardViewComponentContentItemFactory.FilterTextView] = c =>
                this.CreateFilterTextViewContent(descriptor, c, this.parametersConverter);
        }

        /// <inheritdoc/>
        public void Initialized(
            IWorkplaceViewComponent model)
        {
        }

        /// <inheritdoc/>
        public void Clone(
            IWorkplaceViewComponent source,
            IWorkplaceViewComponent cloned,
            ICloneableContext context)
        {
        }

        #endregion
    }
}
