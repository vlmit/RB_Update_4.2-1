#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Tessa.Cards;
using Tessa.Cards.TypeSettings;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Extensions.Default.Shared.Views;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.UI;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Controls;
using Tessa.UI.Files;
using Tessa.UI.Files.Controls;
using Tessa.UI.Views.Content;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.UI.CardFiles
{
    /// <summary>
    /// Вспомогательные методы для обработки расширений типа карточки "Список файлов в представлении".
    /// </summary>
    public static class FilesViewGeneratorHelper
    {
        #region Public Methods

        /// <summary>
        /// Выполняет необходимые действия при инициализации представления с файлами.
        /// </summary>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="extensionContainer"><inheritdoc cref="IExtensionContainer" path="/summary"/></param>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="processNameResolver"><inheritdoc cref="IProcessNameResolver" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="ITypeExtensionContext" path="/summary"/></param>
        /// <param name="model">Модель карточки.</param>
        public static void ProcessInitializingFilesView(
            ISession session,
            IExtensionContainer extensionContainer,
            IViewService viewService,
            IProcessNameResolver processNameResolver,
            ITypeExtensionContext context,
            ICardModel model)
        {
            ThrowIfNull(session);
            ThrowIfNull(extensionContainer);
            ThrowIfNull(viewService);
            ThrowIfNull(processNameResolver);
            ThrowIfNull(context);

            var settings = context.Settings;
            var filesViewAlias = settings?.TryGet<string>(DefaultCardTypeExtensionSettings.FilesViewAlias);
            if (string.IsNullOrEmpty(filesViewAlias))
            {
                return;
            }

            var options = new FileControlCreationParams
            {
                CategoriesViewAlias = settings?.TryGet<string>(DefaultCardTypeExtensionSettings.CategoriesViewAlias) is { Length: not 0 } s
                    ? s
                    : CardControlSettings.FileCategoriesFilteredViewAlias,
                PreviewControlName = settings?.TryGet<string>(DefaultCardTypeExtensionSettings.PreviewControlName),
                IsCategoriesEnabled = settings?.TryGet<bool>(DefaultCardTypeExtensionSettings.IsCategoriesEnabled) ?? false,
                IsIgnoreExistingCategories = settings?.TryGet<bool>(DefaultCardTypeExtensionSettings.IsIgnoreExistingCategories) ?? false,
                IsManualCategoriesCreationDisabled = settings?.TryGet<bool>(DefaultCardTypeExtensionSettings.IsManualCategoriesCreationDisabled) ?? false,
                IsNullCategoryCreationDisabled = settings?.TryGet<bool>(DefaultCardTypeExtensionSettings.IsNullCategoryCreationDisabled) ?? false,
                CategoriesViewMapping = settings
                    ?.TryGet<IList>(DefaultCardTypeExtensionSettings.CategoriesViewMapping)
                    ?.Cast<Dictionary<string, object?>>()
                    .ToList(),
                PagingMode = settings?.ConvertEnum(DefaultCardTypeExtensionSettings.PagingMode, Paging.No) ?? Paging.No,
                PageLimit = settings?.TryConvertInt32(DefaultCardTypeExtensionSettings.PageLimit) ?? DefaultTypeExtensionTypeHelper.DefaultPageLimit
            };

            if (context.CardTask is not { } cardTask)
            {
                AddCardModelInitializers(session, extensionContainer, viewService, processNameResolver, model, filesViewAlias, options);
            }
            else
            {
                model.TaskInitializers.Add(async (taskCardModel, ct) =>
                {
                    if (taskCardModel.CardTask == cardTask)
                    {
                        AddCardModelInitializers(session, extensionContainer, viewService, processNameResolver, taskCardModel, filesViewAlias, options);
                    }
                });
            }
        }

        /// <summary>
        /// Выполняет необходимые действия после инициализации представления с файлами.
        /// </summary>
        /// <param name="initializationStrategy"><inheritdoc cref="IViewCardControlInitializationStrategy" path="/summary"/></param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="extensionContainer"><inheritdoc cref="IExtensionContainer" path="/summary"/></param>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="processNameResolver"><inheritdoc cref="IProcessNameResolver" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="ITypeExtensionContext" path="/summary"/></param>
        /// <param name="model">Модель карточки.</param>
        /// <returns></returns>
        public static async Task ProcessInitializedFilesView(
            IViewCardControlInitializationStrategy? initializationStrategy,
            ISession session,
            IExtensionContainer extensionContainer,
            IViewService viewService,
            IProcessNameResolver processNameResolver,
            ITypeExtensionContext context,
            ICardModel model)
        {
            ThrowIfNull(session);
            ThrowIfNull(extensionContainer);
            ThrowIfNull(viewService);
            ThrowIfNull(processNameResolver);
            ThrowIfNull(context);

            if (context.CardTask is not { } cardTask)
            {
                var isAttached = await AttachViewToFileControlCoreAsync(initializationStrategy, context, model, context.CancellationToken);
                if (!isAttached)
                {
                    var tables = model.ControlBag.OfType<GridViewModel>();
                    foreach (var table in tables)
                    {
                        table.RowInvoked += (s, e) =>
                        {
                            if (e.Action is GridRowAction.Inserted or GridRowAction.Opening)
                            {
                                InitializeExtensionForTableForm(initializationStrategy, context, e);
                            }
                        };
                    }
                }
            }
            else
            {
                await model.ModifyTasksAsync(async (task, _) =>
                {
                    if (task.TaskModel.CardTask == cardTask)
                    {
                        await task.ModifyWorkspaceAsync(async (t, subscribeToTaskModel) =>
                        {
                            var isAttached = await AttachViewToFileControlCoreAsync(initializationStrategy, context, task.TaskModel, CancellationToken.None);
                            if (!isAttached)
                            {
                                foreach (var table in task.TaskModel.ControlBag.OfType<GridViewModel>())
                                {
                                    table.RowInvoked += (s, e) =>
                                    {
                                        if (e.Action is GridRowAction.Inserted or GridRowAction.Opening)
                                        {
                                            InitializeExtensionForTableForm(initializationStrategy, context, e);
                                        }
                                    };
                                }
                            }
                        });
                    }
                });
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Создает контрол с файлами, через который представление
        /// будет взаимодействовать с файловым API. Для каждого алиаса представления
        /// должен быть создан свой контрол. Создание происходит в <see cref="CardUIExtension.Initializing"/>.
        /// </summary>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="extensionContainer"><inheritdoc cref="IExtensionContainer" path="/summary"/></param>
        /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
        /// <param name="processNameResolver"><inheritdoc cref="IProcessNameResolver" path="/summary"/></param>
        /// <param name="cardModel">Модель карточки.</param>
        /// <param name="viewControlName">Алиас контрола представления, которое будет адаптировано под отображение файлов.</param>
        /// <param name="creationParams">Параметры файлового контрола.</param>
        private static void AddCardModelInitializers(
            ISession session,
            IExtensionContainer extensionContainer,
            IViewService viewService,
            IProcessNameResolver processNameResolver,
            ICardModel cardModel,
            string viewControlName,
            FileControlCreationParams creationParams)
        {
            cardModel.ControlInitializers.Add(async (control, cm, r, ct) =>
            {
                if (control is CardViewControlViewModel viewControl)
                {
                    if (viewControl.Name != viewControlName)
                    {
                        return;
                    }

                    if (FormCreationContext.Current.FileControls.Any(x => x.Name == viewControl.Name))
                    {
                        TessaDialog.ShowError($"Multiple FileViewControlViewModel with Name='{viewControl.Name}' was found on the form.");
                        return;
                    }

                    var categoriesView = await viewService.GetByNameAsync(creationParams.CategoriesViewAlias, ct);
                    if (categoriesView is null)
                    {
                        TessaDialog.ShowError($"Categories View:'{creationParams.CategoriesViewAlias}' isn't found'");
                        return;
                    }

                    viewControl.PagingMode = creationParams.PagingMode;
                    viewControl.MaxResultsCount = creationParams.PageLimit;
                    viewControl.PageCountStatus = true;

                    var cardMetadata = cm.GeneralMetadata;
                    var fileContainer = cm.FileContainer;
                    var fileTypes = CardHelper.GetCardFileTypes(await CardHelper.GetFileCardTypesAsync(cardMetadata, session.User.IsAdministrator(), ct));

                    var fileControl = new ViewFileControl(
                        viewControl,
                        fileContainer,
                        extensionContainer,
                        cm.MenuContext,
                        fileTypes,
                        creationParams.IsCategoriesEnabled,
                        creationParams.IsManualCategoriesCreationDisabled,
                        creationParams.IsNullCategoryCreationDisabled,
                        false,
                        creationParams.IsIgnoreExistingCategories,
                        session,
                        processNameResolver,
                        previewControlName: creationParams.PreviewControlName,
                        name: viewControl.Name)
                    {
                        CategoryFilterAsync = async (context) =>
                        {
                            ITessaViewResult? result = null;
                            var categoriesViewMetadata = await categoriesView.GetMetadataAsync(context.CancellationToken);

                            var request = new TessaViewRequest(categoriesViewMetadata.Alias);

                            var parameters =
                                await ViewMappingHelper.AddRequestParametersAsync(
                                    creationParams.CategoriesViewMapping,
                                    cardModel,
                                    session,
                                    categoriesView,
                                    cancellationToken: context.CancellationToken);
                            if (parameters is not null)
                            {
                                request.Parameters.AddRange(parameters);
                            }

                            await cm.ExecuteInContextAsync(
                                async (c, ct3) => { result = await categoriesView.GetDataAsync(request, ct3).ConfigureAwait(false); },
                                context.CancellationToken).ConfigureAwait(false);

                            // категории из представления в порядке, в котором их вернуло представление, кроме строчек null.
                            var viewCategories = result?.Rows
                                .Where(x => x.Count > 0)
                                .Select(IFileCategory (x) => new FileCategory((Guid?) x[0], (string) NotNullOrThrow(x[1]), (int) NotNullOrThrow(x[2])))
                                .Where(x => x.ID.HasValue)
                                .ToArray() ?? [];

                            // категории из представления плюс вручную добавленные или другие присутствующие в карточке категории, кроме null.
                            var mainCategories = viewCategories
                                .Union(context.Categories)
                                .ToArray();

                            // добавляем наверх "Без категории" и возвращаем результирующий список
                            return new List<IFileCategory?> { null }
                                .Union(mainCategories);
                        }
                    };

                    await fileControl.InitializeAsync(fileContainer.Files, cancellationToken: ct);

                    cm.Info[viewControl.Name] = fileControl;
                    FormCreationContext.Current.Register(fileControl);

                    control.Unloaded += async (s, e) =>
                    {
                        var deferral = e.Defer();
                        try
                        {
                            fileControl.StopTimer();
                            await fileControl.UnloadAsync(e.ValidationResult);
                        }
                        catch (Exception ex)
                        {
                            deferral.SetException(ex);
                        }
                        finally
                        {
                            deferral.Dispose();
                        }
                    };
                }
            });
        }

        /// <summary>
        /// Связывает представление с файловым API через контрол с файлами.
        /// </summary>
        /// <param name="cardModel">Модель карточки.</param>
        /// <param name="settings">Настройки расширения.</param>
        /// <param name="createRowFunc">Функция, создающая строку представления.</param>
        /// <param name="initializationStrategy"><inheritdoc cref="IViewCardControlInitializationStrategy" path="/summary"/></param>
        /// <param name="viewModifierAction">Функция модификации контрола представления, например, задания дефолного столбца для сортировки.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Файловый контрол <see cref="ViewFileControl"/>.</returns>
        private static async ValueTask<bool> AttachViewToFileControlAsync(
            ICardModel cardModel,
            ISerializableObject settings,
            Func<TableRowCreationOptions, ViewControlRowViewModel> createRowFunc,
            IViewCardControlInitializationStrategy? initializationStrategy = null,
            Action<CardViewControlViewModel>? viewModifierAction = null,
            CancellationToken cancellationToken = default)
        {
            var viewControlName = settings.TryGet<string>(DefaultCardTypeExtensionSettings.FilesViewAlias);
            if (cardModel.Controls.TryGet<CardViewControlViewModel>(viewControlName) is not { } viewControlViewModel
                || CardFilesHelper.TryGetFileControl(cardModel.Info, viewControlViewModel.Name) is not { } fileControl)
            {
                return false;
            }

            viewControlViewModel.CreateRowFunc = createRowFunc;
            InitializeContextMenu(viewControlViewModel, fileControl);
            if (initializationStrategy is not null)
            {
                await viewControlViewModel.InitializeStrategyAsync(initializationStrategy, true, cancellationToken);
                viewModifierAction?.Invoke(viewControlViewModel);
                await viewControlViewModel.InitializeOnTabAsync();
            }

            AttachToFileControl(viewControlViewModel, fileControl);
            InitializeGrouping(viewControlViewModel, fileControl);
            InitializeFiltering(viewControlViewModel, fileControl);
            InitializeDragDrop(viewControlViewModel, cardModel);
            InitializeClickCommands(viewControlViewModel, fileControl);
            InitializeMenuButton(viewControlViewModel, fileControl);
            InitializeKeyDownHandlers(viewControlViewModel, fileControl);

            var defaultGroup = settings.TryGet<string>(DefaultCardTypeExtensionSettings.DefaultGroup);
            if (!string.IsNullOrEmpty(defaultGroup))
            {
                await fileControl.SelectGroupingAsync(fileControl.Groupings.TryGet(defaultGroup), cancellationToken);
            }

            return true;
        }

        /// <summary>
        /// Привязывает изменение коллекции файлов к представлению.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Вью-модель скрытого контрола с файлами.</param>
        private static void AttachToFileControl(CardViewControlViewModel viewModel, IFileControl fileControl)
        {
            fileControl.Items.CollectionChanged += (sender, e) => { viewModel.DelayedViewRefresh.RunAfterDelay(150); };
        }

        /// <summary>
        /// Добавляет Drag&amp;Drop к представлению с файлами.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="cardModel">Модель карточки.</param>
        private static void InitializeDragDrop(CardViewControlViewModel viewModel, ICardModel cardModel)
        {
            viewModel.AllowDrop = true;
            viewModel.DragDrop = new FilesDragDrop(cardModel, viewModel.Name);
        }

        /// <summary>
        /// Синхронизирует группировку в файловом контроле и предсталвении.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Контрол файлов.</param>
        private static void InitializeGrouping(CardViewControlViewModel viewModel, IFileControl fileControl)
        {
            if (viewModel.Table.Columns is not { } columns || viewModel.ViewMetadata is not { } viewMetadata)
            {
                return;
            }

            foreach (var column in columns.Cast<TableColumnViewModel>())
            {
                column.ContextMenuGenerators.Clear();
            }

            var groupCaptionColumn = columns.Cast<TableColumnViewModel>().FirstOrDefault(x => x.ColumnName == ColumnsConst.GroupCaption);
            if (groupCaptionColumn is not null)
            {
                groupCaptionColumn.Visibility = false;
            }

            fileControl.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(FileControl.SelectedGrouping))
                {
                    if (fileControl.SelectedGrouping is null)
                    {
                        viewModel.Table.SetGrouping(null);
                        var categoryColumn = columns.Cast<TableColumnViewModel>().First(x => x.ColumnName == ColumnsConst.CategoryCaption);
                        categoryColumn.Visibility = true;
                    }
                    else
                    {
                        var categoryColumn = columns.Cast<TableColumnViewModel>().First(x => x.ColumnName == ColumnsConst.CategoryCaption);
                        categoryColumn.Visibility = fileControl.SelectedGrouping.Name != FileGroupingNames.Category;

                        var groupColumnMetadata = viewMetadata.Columns.First(x => x.Alias == ColumnsConst.GroupName);
                        var groupCaptionColumnMetadata = viewMetadata.Columns.First(x => x.Alias == ColumnsConst.GroupCaption);

                        viewModel.Table.SetGrouping(groupColumnMetadata, groupCaptionColumnMetadata);
                    }

                    var groupCaptionColumnValue = columns.Cast<TableColumnViewModel>()
                        .FirstOrDefault(x => x.ColumnName == ColumnsConst.GroupCaption);

                    if (groupCaptionColumnValue is not null)
                    {
                        groupCaptionColumnValue.Visibility = false;
                    }
                }
            };
        }

        /// <summary>
        /// Синхронизирует фильтрацию в файловом контроле и представлении.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Контрол файлов.</param>
        private static void InitializeFiltering(CardViewControlViewModel viewModel, IFileControl fileControl)
        {
            fileControl.PropertyChanged += async (o, e) =>
            {
                // во время обновления представления фильтрация может быть сброшена в дата провайдере. Это событие обрабатывать не нужно.
                if (viewModel.IsDataLoading)
                {
                    return;
                }

                if (e.PropertyName == nameof(IFileControl.SelectedFiltering) && viewModel.Parameters is { } parameters)
                {
                    var previousFilteringParameter = parameters.FindByName(ColumnsConst.FilterParameter);
                    if (previousFilteringParameter is not null)
                    {
                        parameters.Remove(previousFilteringParameter);
                    }

                    if (fileControl.SelectedFiltering is FileGroupingFiltering filter)
                    {
                        parameters.Add(new RequestParameter(ColumnsConst.FilterParameter).Add(EqualsToCriteriaOperator.Instance, filter.Caption));
                    }

                    await viewModel.RefreshAsync();
                }
            };
        }

        /// <summary>
        /// Инициализирует обработчики событий клавиатуры.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Вью-модель скрытого контрола с файлами.</param>
        private static void InitializeKeyDownHandlers(CardViewControlViewModel viewModel, IFileControl fileControl)
        {
            viewModel.KeyDownHandlers.Add(async (item, data, e) =>
            {
                if (data is TableFileRowViewModel row)
                {
                    if (row.GridViewModel.SelectedItems.Count == 1 &&
                        row.GridViewModel.SelectedItem == row)
                    {
                        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        {
                            var file = row.FileViewModel.Model;
                            if (file is null)
                            {
                                TessaDialog.ShowMessage(string.Format((await LocalizeAsync("$UI_Common_FileNotFound"))!, ""));

                                return;
                            }

                            await FileControlHelper.OpenAsync(fileControl, [file], FileOpeningMode.ForEdit);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Инициализирует обработчики событий мыши и выбора строки.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Вью-модель скрытого контрола с файлами.</param>
        private static void InitializeClickCommands(CardViewControlViewModel viewModel, ViewFileControl fileControl)
        {
            viewModel.Table.RowUnselected += async (o, e) =>
            {
                if (e.Row is null)
                {
                    return;
                }

                var gridViewModel = e.Row.GridViewModel;
                var row = (TableFileRowViewModel) e.Row;
                var fileViewModel = row.FileViewModel;
                var file = fileViewModel.Model.Versions.Last.File;

                //<- Обработка случая, когда строка выбрана, а файл еще прогружается.
                if (e.Row.IsSelected == fileViewModel.IsSelected)
                {
                    return;
                }

                if (fileControl.Manager.IsPreviewInProgress())
                {
                    fileViewModel.IsSelected = true;
                    e.Row.IsSelected = true;
                    return;
                }
                //->

                fileViewModel.IsSelected = false;

                // следующий код асинхронный, использовать аргументы "e" нельзя.

                if (file.IsLocal && fileControl.Manager.IsInPreview(file.Content.GetLocalFilePath()))
                {
                    await fileControl.Manager.ResetPreviewAsync();
                }
                else if (gridViewModel.SelectedItems.Count == 1)
                {
                    await fileControl.Manager.ResetPreviewAsync();
                }

                if (Keyboard.Modifiers.Has(ModifierKeys.Control) || Keyboard.Modifiers.Has(ModifierKeys.Shift))
                {
                    var presenter = new FileControlPresenter(fileControl);
                    await presenter.ShowSelectedFilesMessageAsync(fileControl.SelectedItems.ToArray());
                }
            };

            viewModel.Table.RowSelected += async (o, e) =>
            {
                if (e.Row is null)
                {
                    return;
                }

                var gridViewModel = e.Row.GridViewModel;
                var row = (TableFileRowViewModel) e.Row;
                var fileViewModel = row.FileViewModel;

                //<- Обработка случая, когда строка выбрана, а файл еще прогружается.
                if (e.Row.IsSelected == fileViewModel.IsSelected)
                {
                    return;
                }

                if (fileControl.Manager.IsPreviewInProgress())
                {
                    fileViewModel.IsSelected = false;
                    e.Row.IsSelected = false;
                    return;
                }

                //->
                fileViewModel.IsSelected = true;

                // следующий код асинхронный, использовать аргументы "e" нельзя.

                // если мы перевели фокус на это представление
                if (gridViewModel.SelectedItems.Count == 1)
                {
                    await fileControl.Manager.ClearSelectionAsync(fileControl);
                }

                if (Keyboard.Modifiers.HasNot(ModifierKeys.Control) && Keyboard.Modifiers.HasNot(ModifierKeys.Shift))
                {
                    fileControl.BeginShowPreview(fileViewModel);
                }

                if (Keyboard.Modifiers.Has(ModifierKeys.Control) || Keyboard.Modifiers.Has(ModifierKeys.Shift))
                {
                    var presenter = new FileControlPresenter(fileControl);
                    await presenter.ShowSelectedFilesMessageAsync(fileControl.SelectedItems.ToArray());
                }
            };

            viewModel.LeftButtonClickCommand = new DelegateCommand(async (o) =>
            {
                var clickInfo = (IViewClickInfo) o;
                var row = (TableFileRowViewModel) clickInfo.Row;
                var fileViewModel = row.FileViewModel;
                if (fileControl.Manager.IsPreviewInProgress())
                {
                    clickInfo.EventArgs.Handled = true;
                    return;
                }

                if (Keyboard.Modifiers.HasNot(ModifierKeys.Control) && Keyboard.Modifiers.HasNot(ModifierKeys.Shift) && row.IsSelected)
                {
                    fileControl.BeginShowPreview(fileViewModel);
                }
            });

            viewModel.DoubleClickCommand =
                new DelegateCommand(async o =>
                {
                    var clickInfo = (IViewClickInfo) o;
                    var row = (TableFileRowViewModel) clickInfo.Row;
                    var selectedFileID = row.FileID;
                    var file = fileControl.Files.FirstOrDefault(f => f.ID == selectedFileID);
                    if (file is null)
                    {
                        await TessaDialog.ShowMessageAsync(string.Format((await LocalizeAsync("$UI_Common_FileNotFound"))!, ""));
                        return;
                    }

                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    {
                        await FileControlHelper.OpenAsync(fileControl, [file], FileOpeningMode.ForEdit);
                    }
                    else
                    {
                        await FileControlHelper.OpenAsync(fileControl, [file], FileOpeningMode.ForRead);
                    }
                });

            viewModel.RightButtonClickCommand = new DelegateCommand(async o =>
            {
                var clickInfo = (IViewClickInfo) o;
                clickInfo.EventArgs.Handled = true;
                var presenter = new FileControlPresenter(fileControl);
                var row = (TableFileRowViewModel) clickInfo.Row;
                var fileID = row.FileID;
                var file = fileControl.Items.FirstOrDefault(f => f.Model.ID == fileID);
                await presenter.ShowFileMenuAsync(file, clickInfo.FrameworkElement).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Создает контекстное меню контрола.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Вью-модель скрытого контрола с файлами.</param>
        private static void InitializeContextMenu(CardViewControlViewModel viewModel, IFileControl fileControl)
        {
            viewModel.ContextMenuGenerators.Add(async context =>
            {
                var (actions, _, _) = await fileControl.GenerateControlMenuAsync();
                var toRemoveActions = new[] { FileMenuActionNames.Sortings };
                context.MenuActions.AddRange(actions.Where(x => !toRemoveActions.Contains(x.Name)));
            });
        }

        /// <summary>
        /// Добавляет кнопку меню в контрол представления.
        /// </summary>
        /// <param name="viewModel">Вью-модель контрола представления.</param>
        /// <param name="fileControl">Вью-модель скрытого контрола с файлами.</param>
        private static void InitializeMenuButton(CardViewControlViewModel viewModel, IFileControl fileControl)
        {
            var refreshButtonIndex = viewModel.TopItems.Items.IndexOf(i => i is RefreshButton);
            if (refreshButtonIndex != -1)
            {
                viewModel.TopItems.Items.RemoveAt(refreshButtonIndex);
            }

            var addMenuButton = new ShowContextMenuButtonViewModel { FileControl = fileControl, ViewModel = viewModel };
            viewModel.TopItems.Items.Insert(0, addMenuButton);
        }

        private static void InitializeExtensionForTableForm(
            IViewCardControlInitializationStrategy? initializationStrategy,
            ITypeExtensionContext context,
            RowEventArgs e)
        {
            var cardViewControlViewModels = e.RowModel.ControlBag.OfType<CardViewControlViewModel>();
            if (cardViewControlViewModels.Any())
            {
                DispatcherHelper.InvokeInUI(async () =>
                {
                    var isAttached = await AttachViewToFileControlCoreAsync(
                        initializationStrategy,
                        context,
                        e.RowModel,
                        CancellationToken.None);
                    if (!isAttached)
                    {
                        var tables = e.RowModel.ControlBag.OfType<GridViewModel>();
                        foreach (var table in tables)
                        {
                            table.RowInvoked += (s2, e2) => { InitializeExtensionForTableForm(initializationStrategy, context, e2); };
                        }
                    }
                });
            }
        }

        private static async ValueTask<bool> AttachViewToFileControlCoreAsync(
            IViewCardControlInitializationStrategy? initializationStrategy,
            ITypeExtensionContext extensionContext,
            ICardModel cardModel,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(extensionContext.Settings);

            IViewCardControlInitializationStrategy? strategy = null;
            foreach (var handlerAsync in cardModel.TryGetFileViewExtensionInitializationStrategyHandlers()
                     ?? Enumerable.Empty<TryGetControlInitializationStrategyAsync>())
            {
                strategy = await handlerAsync(extensionContext, cardModel, cancellationToken);
                if (strategy is not null)
                {
                    break;
                }
            }

            return await AttachViewToFileControlAsync(
                cardModel,
                extensionContext.Settings,
                CreateRowFunc,
                strategy ?? initializationStrategy,
                cancellationToken: cancellationToken);
        }

        private static ViewControlRowViewModel CreateRowFunc(TableRowCreationOptions options)
        {
            var fileViewModel = (IFileViewModel) options.Data[ColumnsConst.FileViewModel];
            options.Data.Remove(ColumnsConst.FileViewModel);

            return new TableFileRowViewModel(fileViewModel, options)
            {
                AutomationId = $"{fileViewModel.Model.ID}",
                AutomationName = fileViewModel.Model.Name
            };
        }

        #endregion
    }
}
