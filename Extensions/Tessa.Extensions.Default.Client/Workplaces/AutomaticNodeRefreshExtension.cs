#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Tessa.Extensions.Default.Shared.Workplaces;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Views;
using Tessa.UI.Views.Extensions;
using Tessa.UI.Views.Workplaces.Tree;

namespace Tessa.Extensions.Default.Client.Workplaces
{
    /// <summary>
    /// Расширение, предоставляющее возможность автоматического обновления узлов рабочего места.
    /// </summary>
    /// <remarks>
    /// У расширения есть конфигуратор <see cref="AutomaticNodeRefreshExtensionConfigurator"/>.
    /// </remarks>
    public class AutomaticNodeRefreshExtension :
        ViewModel<EmptyModel>,
        ITreeItemExtension,
        IWorkplaceExtensionSettingsRestore
    {
        #region Fields

        private bool refreshPending;

        private AutomaticNodeRefreshSettings settings;

        private DispatcherTimer? timer;

        private ITreeItem? treeItem;

        private readonly ISessionFailedChecker sessionFailedChecker;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="sessionFailedChecker"><inheritdoc cref="ISessionFailedChecker" path="/summary"/></param>
        public AutomaticNodeRefreshExtension(ISessionFailedChecker sessionFailedChecker)
        {
            this.settings = new();
            this.sessionFailedChecker = NotNullOrThrow(sessionFailedChecker);
        }

        #endregion

        #region IWorkplaceExtension Implementation

        /// <inheritdoc />
        public void Clone(ITreeItem source, ITreeItem cloned, ICloneableContext context)
        {
        }

        /// <inheritdoc />
        public void Initialize(ITreeItem model) =>
            this.treeItem = model;

        /// <inheritdoc />
        public void Initialized(ITreeItem model)
        {
            this.timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromSeconds(this.settings.RefreshInterval)
            };

            this.timer.Tick += async (s, e) => await this.UpdateByTimerAsync(false);

            this.SubscribeToEvents(model);
            if (model?.Workplace.IsActive is true)
            {
                // вкладка с рабочим местом активна на момент запуска приложения
                this.StartTimer();
            }
        }

        #endregion

        #region IWorkplaceExtensionSettingsRestore Implementation

        /// <inheritdoc />
        public void Restore(Dictionary<string, object?> metadata) =>
            this.settings = metadata.FromSerializedDictionary<AutomaticNodeRefreshSettings>();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override bool OnReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType != typeof(PropertyChangedEventManager))
            {
                return base.OnReceiveWeakEvent(managerType, sender, e);
            }

            var eventArgs = (PropertyChangedEventArgs) e;
            switch (eventArgs.PropertyName)
            {
                case "Parent":
                    var treeItem = (ITreeItem) sender;
                    if (treeItem.Parent is null)
                    {
                        this.UnSubscribeFromEvents(treeItem);
                        this.StopTimer();
                    }
                    else
                    {
                        this.SubscribeToEvents(treeItem);
                        this.StartTimer();
                    }

                    break;
                case "IsActive":
                    var workplace = (IWorkplaceViewModel) sender;
                    if (workplace.IsActive)
                    {
                        this.StartAndUpdateIfRequired(true);
                    }

                    break;
                case "IsExpanded":
                    var isExpanded = ((ITreeItem) sender).IsExpanded;
                    if (isExpanded && this.treeItem?.IsVisibleInPath() is true)
                    {
                        this.StartAndUpdateIfRequired(false);
                    }

                    break;
                case "LastUpdateTime":
                    if (this.timer?.IsEnabled is true)
                    {
                        this.StopTimer();
                        this.StartTimer();
                        this.refreshPending = false;
                    }

                    break;
                case "IsSelected":
                    var isSelected = ((ITreeItem) sender).IsSelected;
                    if (isSelected && this.timer?.IsEnabled is not true)
                    {
                        this.StartAndUpdateIfRequired(false);
                    }

                    break;
            }

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Осуществляет подписку на события изменения рабочего места и активности узла.
        /// </summary>
        /// <param name="treeItem">Узел дерева рабочего места.</param>
        /// <exception cref="ArgumentNullException"/>
        private void SubscribeToEvents(ITreeItem treeItem)
        {
            ThrowIfNull(treeItem);

            PropertyChangedEventManager.AddListener(treeItem, this, "LastUpdateTime");
            PropertyChangedEventManager.AddListener(treeItem, this, "IsExpanded");
            PropertyChangedEventManager.AddListener(treeItem, this, "IsSelected");
            PropertyChangedEventManager.AddListener(treeItem, this, "Parent");
            if (treeItem.Workplace is not null)
            {
                PropertyChangedEventManager.AddListener(treeItem.Workplace, this, "IsActive");
            }

            var currentNode = treeItem.Parent;
            while (currentNode is not null)
            {
                PropertyChangedEventManager.AddListener(currentNode, this, "IsExpanded");
                currentNode = currentNode.Parent;
            }

            this.SubscribeToChildEvents(treeItem);
        }

        private void SubscribeToChildEvents(ITreeItem treeItem)
        {
            this.SubscribeToChildChangingEvents(treeItem, true);

            foreach (var child in treeItem.Items)
            {
                this.SubscribeToChildChangingEvents(child, false);

                PropertyChangedEventManager.AddListener(child, this, "IsExpanded");
                PropertyChangedEventManager.AddListener(child, this, "IsSelected");

                if (child is ISubsetTreeItem subsetTreeItem)
                {
                    foreach (var subsetChild in subsetTreeItem.Items)
                    {
                        PropertyChangedEventManager.AddListener(subsetChild, this, "IsSelected");
                    }
                }
            }
        }

        private void SubscribeToChildChangingEvents(ITreeItem treeItem, bool firstLevel)
        {
            ((INotifyCollectionChanged) treeItem.Items).CollectionChanged += (_, e) =>
            {
                if (e.NewItems is { Count: > 0 } newItems)
                {
                    var isExpanded = false;
                    foreach (var item in newItems.Cast<ITreeItem>())
                    {
                        PropertyChangedEventManager.AddListener(item, this, "IsSelected");

                        if (firstLevel)
                        {
                            PropertyChangedEventManager.AddListener(item, this, "IsExpanded");

                            if (item is ISubsetTreeItem subsetTreeItem)
                            {
                                this.SubscribeToChildChangingEvents(subsetTreeItem, false);
                            }
                        }

                        isExpanded |= item.IsExpanded;
                    }

                    if (firstLevel && isExpanded)
                    {
                        this.StartAndUpdateIfRequired(false);
                    }
                }

                if (e.OldItems is { Count: > 0 } oldItems)
                {
                    foreach (var item in oldItems.Cast<ITreeItem>())
                    {
                        PropertyChangedEventManager.RemoveListener(item, this, "IsSelected");

                        if (firstLevel)
                        {
                            PropertyChangedEventManager.RemoveListener(item, this, "IsExpanded");

                            if (item is ISubsetTreeItem subsetTreeItem)
                            {
                                foreach (var subsetChild in subsetTreeItem.Items)
                                {
                                    PropertyChangedEventManager.RemoveListener(subsetChild, this, "IsSelected");
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Отписывается от событий влияющих на запуск автоматического обновления.
        /// </summary>
        /// <param name="treeItem">Узел дерева.</param>
        /// <exception cref="ArgumentNullException"/>
        private void UnSubscribeFromEvents(ITreeItem treeItem)
        {
            ThrowIfNull(treeItem);

            PropertyChangedEventManager.RemoveListener(treeItem, this, "Parent");
            PropertyChangedEventManager.RemoveListener(treeItem, this, "LastUpdateTime");
            PropertyChangedEventManager.RemoveListener(treeItem, this, "IsSelected");
            if (treeItem.Workplace is not null)
            {
                PropertyChangedEventManager.RemoveListener(treeItem.Workplace, this, "IsActive");
            }

            var currentNode = treeItem.Parent;
            while (currentNode is not null)
            {
                PropertyChangedEventManager.RemoveListener(currentNode, this, "IsExpanded");
                currentNode = currentNode.Parent;
            }

            foreach (var child in treeItem.Items)
            {
                PropertyChangedEventManager.RemoveListener(child, this, "IsExpanded");
                PropertyChangedEventManager.RemoveListener(child, this, "IsSelected");

                if (child is ISubsetTreeItem subsetTreeItem)
                {
                    foreach (var subsetChild in subsetTreeItem.Items)
                    {
                        PropertyChangedEventManager.RemoveListener(subsetChild, this, "IsSelected");
                    }
                }
            }
        }

        /// <summary>
        /// Вызывается при срабатывании таймера.
        /// </summary>
        /// <param name="skipUpdateTable">
        /// Признак необходимости отменить обновление таблицы.
        /// </param>
        private async Task UpdateByTimerAsync(bool skipUpdateTable)
        {
            // Если задача не успела отработать или узел находится в процессе обновления, то просто выходим из задачи.
            // При сравнении использован коэффициент 0.98 (время уменьшено на 1/50), потому что обновление
            // treeItem.LastUpdateTime происходит через некоторое время после того, как предыдущее расширение отработало,
            // поэтому при проверке нужен доверительный интервал.
            if (this.treeItem is null ||
                this.treeItem.InUpdate ||
                (DateTime.UtcNow - this.treeItem.LastUpdateTime).TotalSeconds < this.settings.RefreshInterval * 0.98 ||
                await this.sessionFailedChecker.IsCurrentSessionFailedAsync())
            {
                return;
            }

            // Останавливаемся, если узел РМ не активно или узел не виден в дереве.
            if (!this.treeItem.Workplace.IsActive
                || !this.treeItem.IsVisibleInPath())
            {
                this.refreshPending = true;
                this.StopTimer();
                return;
            }

            // Останавливаемся, если не включена настройка "Обновлять всегда" и не выбран элемент с указанным расширением и он не раскрыт вместе с сабсетами.
            if (!this.settings.AlwaysRefresh
                && !this.treeItem.HasSelection()
                && (!this.treeItem.IsExpanded || !this.treeItem.Items.Any(i => i.IsExpanded)))
            {
                this.refreshPending = true;
                this.StopTimer();
                return;
            }

            await using var _ = SessionRequestTypeContext.Create(SessionRequestType.Background);

            await this.treeItem.RefreshNodeAsync(onCompletedAsync: async t =>
            {
                if (this.treeItem.HasSelection())
                {
                    await this.RefreshTableContentAsync(skipUpdateTable);
                }
            });
        }

        /// <summary>
        /// Запускает таймер.
        /// </summary>
        private void StartTimer()
        {
            if (this.timer is not null && !this.timer.IsEnabled)
            {
                this.timer.Start();
            }
        }

        /// <summary>
        /// Останавливает таймер.
        /// </summary>
        private void StopTimer() =>
            this.timer?.Stop();

        /// <summary>
        /// Обновляет контент в таблице.
        /// </summary>
        /// <param name="skipUpdateTable">Пропустить обновление.</param>
        private Task RefreshTableContentAsync(bool skipUpdateTable)
        {
            this.refreshPending = false;

            return this.settings.WithContentDataRefreshing && !skipUpdateTable
                ? RefreshContentAsync(this.treeItem?.Workplace)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Вызывает обновление содержимого табличной части.
        /// </summary>
        /// <param name="workplaceViewModel">Модель рабочего места.</param>
        private static async Task RefreshContentAsync(IWorkplaceViewModel? workplaceViewModel)
        {
            if (workplaceViewModel is null
                || workplaceViewModel.Context.ViewContext is not { } viewContext)
            {
                return;
            }

            // Получаем верхнюю вью (от которой зависят остальные)
            var rootContext = viewContext.GetRoot();
            var viewComponent = rootContext.TryGetViewContainer();

            if (viewComponent is null || viewComponent.CurrentPage == 1)
            {
                // либо вью не поддерживает пейджинг, либо страница и так первая, либо это какой-то кастом
                // если кастом, то надеемся, что он поддерживает RefreshCommand
                await rootContext.RefreshViewAsync();
            }
            else
            {
                // Refresh будет автоматом при изменении номера страницы на первую
                viewComponent.CurrentPage = 1;
            }
        }

        /// <summary>
        /// Вызывает обновление и стартует таймер в случае необходимости.
        /// </summary>
        /// <param name="skipUpdateTable">Пропустить обновление табличной части.</param>
        private void StartAndUpdateIfRequired(bool skipUpdateTable)
        {
            if (this.refreshPending)
            {
                //this.refreshPending = false;
                var _ = this.UpdateByTimerAsync(skipUpdateTable);
            }

            this.StartTimer();
        }

        #endregion
    }
}
