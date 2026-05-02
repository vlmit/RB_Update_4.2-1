using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.UI;
using Tessa.UI.Menu;
using Tessa.UI.Views;
using Tessa.UI.Views.Content;
using Tessa.UI.Views.Workplaces.Tree;
using Tessa.Views;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Расширение, демонстрирующее добавление элементов контекстного меню.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="ViewsContextMenuExtensionConfigurator"/>.
    /// </remarks>
    public sealed class ViewsContextMenuExtension : IWorkplaceViewComponentExtension
    {
        #region Private Constants

        private const string ClearFilterCaption = "$Views_Table_ClearFilter_ToolTip";

        private const string ClearFilterIconName = "Thin65";

        private const string ClearFilterName = "ClearFilter";

        private const string FilterIconName = "Thin100";

        private const string FilterName = "Filter";

        private const string FilterSeparatorName = "FilterSeparator";

        private const string OpenCardIconName = "Thin13";

        private const string OpenCardSeparatorName = "OpenCardSeparator";

        private const string RefreshIconName = "Thin412";

        private const string RefreshName = "Refresh";

        private const string UITilesFilter = "$UI_Tiles_Filter";

        private const string UITilesOpenCard = "$UI_Tiles_OpenCard";

        private const string UITilesRefresh = "$UI_Tiles_Refresh";

        #endregion

        #region Fields

        private readonly Func<IUIHost> hostGetter;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ViewsContextMenuExtension"/>.
        /// </summary>
        /// <param name="hostGetter">
        /// Фабрика для получения <see cref="IUIHost"/>.
        /// </param>
        public ViewsContextMenuExtension(Func<IUIHost> hostGetter)
        {
            this.hostGetter = NotNullOrThrow(hostGetter);
        }

        #endregion

        #region IWorkplaceViewComponentExtension Members

        /// <inheritdoc />
        public void Clone(IWorkplaceViewComponent source, IWorkplaceViewComponent cloned, ICloneableContext context)
        {
        }

        /// <inheritdoc />
        public void Initialize(IWorkplaceViewComponent model) =>
            model.ContextMenuGenerators.AddRange(
                GetRefreshMenuAction(),
                GetFilterMenuAction(),
                GetClearFilterMenuAction(),
                this.GetOpenCardMenuAction());

        /// <inheritdoc />
        public void Initialized(IWorkplaceViewComponent model)
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Возвращает действие, создающее пункт меню "Сбросить фильтр".
        /// </summary>
        /// <returns>
        /// Действие, создающее пункт меню "Сбросить фильтр".
        /// </returns>
        private static Func<ViewContextMenuContext, ValueTask> GetClearFilterMenuAction() =>
            c =>
                {
                    if (c.ViewContext.Parameters.All(p => p.ReadOnly))
                    {
                        return ValueTask.CompletedTask;
                    }

                    c.MenuActions.Add(
                        new MenuAction(
                            ClearFilterName,
                            Localize(ClearFilterCaption),
                            c.MenuContext.Icons.Get(ClearFilterIconName),
                            new DelegateCommand(
                                async o => await c.ViewContext.ClearFilterAsync(c.ViewContext.Parameters),
                                o => c.ViewContext.CanClearFilter(c.ViewContext.Parameters))));

                    return ValueTask.CompletedTask;
                };

        /// <summary>
        /// Возвращает действие, создающее пункт меню "Фильтр".
        /// </summary>
        /// <returns>
        /// Действие, создающее пункт меню "Фильтр".
        /// </returns>
        private static Func<ViewContextMenuContext, ValueTask> GetFilterMenuAction() =>
            c =>
                {
                    if (c.ViewContext.Parameters.Metadata.All(p => p.Hidden))
                    {
                        return ValueTask.CompletedTask;
                    }

                    c.MenuActions.Add(new MenuSeparatorAction(FilterSeparatorName));
                    c.MenuActions.Add(
                        new MenuAction(
                            FilterName,
                            Localize(UITilesFilter),
                            c.MenuContext.Icons.Get(FilterIconName),
                            new DelegateCommand(async o => await c.ViewContext.FilterViewAsync(), o => c.ViewContext.CanFilterView())));

                    return ValueTask.CompletedTask;
                };

        /// <summary>
        /// Возвращает действие, создающее пункт меню "Обновить".
        /// </summary>
        /// <returns>
        /// Действие, создающее пункт меню "Обновить".
        /// </returns>
        private static Func<ViewContextMenuContext, ValueTask> GetRefreshMenuAction() =>
            c =>
                {
                    c.MenuActions.Add(
                        new MenuAction(
                            RefreshName,
                            Localize(UITilesRefresh),
                            c.MenuContext.Icons.Get(RefreshIconName),
                            new DelegateCommand(
                                async o => await c.ViewContext.RefreshViewAsync(),
                                o => c.ViewContext.CanRefreshView())));

                    return ValueTask.CompletedTask;
                };


        /// <summary>
        /// Возвращает действие, создающее пункт меню "Открыть карточку".
        /// </summary>
        /// <returns>
        /// Действие, создающее пункт меню "Открыть карточку".
        /// </returns>
        private Func<ViewContextMenuContext, ValueTask> GetOpenCardMenuAction() =>
            async c =>
                {
                    if (c.ViewContext is not { RefSection: null, View: { } view, SelectedRow: { } selectedObject } ||
                        await view.GetMetadataAsync(c.CancellationToken) is not { } metadata ||
                        metadata.References.Where(x => x.IsCard).ToArray() is not { Length: > 0 } cardRefs)
                    {
                        return;
                    }

                    var separatorAdded = false;
                    var addedIDs = new List<Guid>();
                    foreach (var cardRef in cardRefs)
                    {
                        var cardId = selectedObject.GetValueID(cardRef.ColPrefix);
                        if (cardId is null or DBNull)
                        {
                            return;
                        }

                        var displayValue = selectedObject.GetDisplayValue(metadata.Columns, cardRef);

                        Guid id;
                        if (cardId is Guid guid)
                        {
                            id = guid;
                        }
                        else if (!Guid.TryParse(cardId.ToString(), out id))
                        {
                            continue;
                        }

                        if (addedIDs.Contains(id))
                        {
                            continue;
                        }

                        addedIDs.Add(id);

                        if (!separatorAdded)
                        {
                            c.MenuActions.Add(new MenuSeparatorAction(OpenCardSeparatorName));
                            separatorAdded = true;
                        }

                        c.MenuActions.Add(
                            new MenuAction(
                                "OpenCard_" + id,
                                string.Format(
                                    "{0}: {1}", 
                                    await LocalizeAsync(UITilesOpenCard), 
                                    await LocalizeFormatAsync(displayValue)),
                                c.MenuContext.Icons.Get(OpenCardIconName),
                                new DelegateCommand(
                                    async o =>
                                    await c.MenuContext.UIContextExecutorAsync(async (ctx, ct) =>
                                        await this.OpenCardInUIHostAsync(id, displayValue, ctx, ct)))));
                    }
                };

        /// <summary>
        /// Отображает карточку в UI.
        /// </summary>
        /// <param name="id">
        /// Идентификатор карточки.
        /// </param>
        /// <param name="displayValue">
        /// Отображаемая строка.
        /// </param>
        /// <param name="context">
        /// Контекст.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task OpenCardInUIHostAsync(
            Guid id,
            string displayValue,
            IUIContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using ISplash splash = TessaSplash.Create(TessaSplashMessage.OpeningCard);

                IUIHost uiHost = this.hostGetter();
                await uiHost.OpenCardAsync(
                    id,
                    options: new OpenCardOptions
                    {
                        DisplayValue = displayValue,
                        UIContext = context,
                        Splash = splash,
                    },
                    cancellationToken: cancellationToken);
            }
            catch (NotSupportedException)
            {
                // используется FakeUIHost, например, мы открыты в TessaAdmin, игнорируем ошибку.
            }
        }

        #endregion
    }
}
