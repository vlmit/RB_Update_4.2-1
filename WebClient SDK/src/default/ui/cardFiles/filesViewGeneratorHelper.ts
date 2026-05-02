import { ICardModel } from 'tessa/ui/cards/interfaces';
import { FileControlCreationParams } from './fileControlCreationParams';
import {
  CardControlTypes,
  CardInstanceType,
  CardTypeEntryControl,
  CardTypeFlags,
  ICardTypeExtensionContext,
  Paging,
  SchemeType,
  ViewCriteriaOperators,
  ViewParameterMetadata
} from '@tessa/platform';
import { Visibility, hasNotFlag } from 'tessa/platform';
import { CardFileType } from 'tessa/cards';
import { localize } from '@tessa/application';
import {
  FileGroupingFiltering,
  FileListViewModel,
  FileSortingDirection,
  FileViewModel,
  GridRowAction,
  GridRowEventArgs,
  GridViewModel,
  IViewControlInitializationStrategy,
  ViewControlButtonPanelViewModel,
  ViewControlPagingViewModel,
  ViewControlRefreshButtonViewModel,
  ViewControlToolbarItem,
  ViewControlViewModel,
  addRequestParameters,
  getSelectedFilesPreviewMessage
} from 'tessa/ui/cards/controls';
import { FormCreationContext, IControlViewModel } from 'tessa/ui/cards';
import {
  ITessaViewResult,
  RequestParameterBuilder,
  TessaViewRequest,
  ViewService
} from 'tessa/views';
import { UIButton, showError, tryGetFromInfo, tryGetFromSettings } from 'tessa/ui';
import { FileCategory, allowPreviewExtensions, checkCanDownloadFile } from 'tessa/files';
import { IStorage, ValidationResultBuilder } from '@tessa/core';
import { TableFileRowViewModel } from './tableFileRowViewModel';
import { Keyboard } from 'tessa/keyboard';
import { Lambda, reaction, runInAction } from 'mobx';
import { ShowContextMenuButtonViewModel } from './showContextMenuButtonViewModel';
import { tryGetFileViewExtensionInitializationStrategyHandlers } from './cardFilesExtensions';
import { FilesViewControlContentItemsFactory } from './filesViewControlContentItemsFactory';
import { FilesViewCardControlInitializationStrategy } from './filesViewCardControlInitializationStrategy';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms';
import { userSession } from 'common';
import { PreviewFilesList } from 'tessa/ui/preview';

export async function processInitializingFilesView(
  model: ICardModel,
  context: ICardTypeExtensionContext
): Promise<void> {
  const settings = context.settings;
  const filesViewAlias = tryGetFromSettings<string>(settings, 'FilesViewAlias', '');
  if (!filesViewAlias) {
    return;
  }

  const options = new FileControlCreationParams();
  const categoriesViewAlias = tryGetFromSettings<string>(settings, 'CategoriesViewAlias', '');
  if (categoriesViewAlias) {
    options.categoriesViewAlias = categoriesViewAlias;
    // otherwise keep default view alias (it's not empty)
  }
  options.previewControlName = tryGetFromSettings<string>(settings, 'PreviewControlName', '');
  options.isCategoriesEnabled = tryGetFromSettings<boolean>(settings, 'IsCategoriesEnabled', false);
  options.isIgnoreExistingCategories = tryGetFromSettings<boolean>(
    settings,
    'IsIgnoreExistingCategories',
    false
  );
  options.isManualCategoriesCreationDisabled = tryGetFromSettings<boolean>(
    settings,
    'IsManualCategoriesCreationDisabled',
    false
  );
  options.isNullCategoryCreationDisabled = tryGetFromSettings<boolean>(
    settings,
    'IsNullCategoryCreationDisabled',
    false
  );
  options.categoriesViewMapping = tryGetFromSettings<IStorage[]>(
    settings,
    'CategoriesViewMapping',
    []
  );
  options.pagingMode = tryGetFromSettings<Paging>(settings, 'PagingMode', Paging.No);
  options.pageLimit = tryGetFromSettings<number>(settings, 'PageLimit', 20);

  const cardTask = context.cardTask;
  if (!cardTask) {
    await initializeFileControl(model, filesViewAlias, options);
  } else {
    model.taskInitializers.push(async taskCardModel => {
      if (taskCardModel.cardTask === cardTask) {
        await initializeFileControl(taskCardModel, filesViewAlias, options);
      }
    });
  }
}

export async function processInitializedFilesView(
  model: ICardModel,
  context: ICardTypeExtensionContext,
  disposes: Array<(() => void) | null>
): Promise<void> {
  const settings = context.settings;
  if (!context.cardTask) {
    const fileControl = await attachViewToFileControlCore(context, model);
    initializeDefaultGroupInFileControl(fileControl, settings);
    if (!fileControl) {
      const tables = model.controlsBag.filter(x => x instanceof GridViewModel) as GridViewModel[];
      for (const table of tables) {
        disposes.push(
          table.rowInvoked.addWithDispose(e => {
            if (e.action == GridRowAction.Inserted || e.action == GridRowAction.Opening) {
              initializeExtensionForTableForm(context, e, disposes);
            }
          })
        );
      }
    }
  } else {
    const tasks = (model.mainForm as DefaultFormTabWithTasksViewModel).tasks;
    if (!tasks) {
      return;
    }
    const task = tasks.find(x => x.taskModel.cardTask === context.cardTask);
    if (task) {
      task.modifyWorkspace(async () => {
        const fileControl = await attachViewToFileControlCore(context, task.taskModel);
        initializeDefaultGroupInFileControl(fileControl, settings);
        if (!fileControl) {
          const tables = task.taskModel.controlsBag.filter(
            x => x instanceof GridViewModel
          ) as GridViewModel[];
          for (const table of tables) {
            disposes.push(
              table.rowInvoked.addWithDispose(e => {
                if (e.action == GridRowAction.Inserted || e.action == GridRowAction.Opening) {
                  initializeExtensionForTableForm(context, e, disposes);
                }
              })
            );
          }
        }
      });
    }
  }
}

function initializeFileControl(
  model: ICardModel,
  viewControlName: string,
  creationParams: FileControlCreationParams,
  dispose: Array<Function | Lambda | null> = []
) {
  const cardMetadata = model.generalMetadata;
  const cardTypes = cardMetadata.cardTypes.filter(
    p =>
      p.instanceType === CardInstanceType.File &&
      hasNotFlag(p.flags, CardTypeFlags.Hidden) &&
      (userSession.isAdmin || hasNotFlag(p.flags, CardTypeFlags.Administrative))
  );

  const fileTypes = cardTypes
    .map(fileType => {
      return {
        item: new CardFileType(fileType),
        localizedCaption: localize(fileType.caption)
      };
    })
    .sort((a, b) => {
      return a.localizedCaption.localeCompare(b.localizedCaption);
    })
    .map(x => x.item);

  model.controlInitializers.push(async control => {
    if (control instanceof ViewControlViewModel) {
      if (control.name !== viewControlName) {
        return;
      }

      if (FormCreationContext.current?.fileControls.some(x => x.name === viewControlName)) {
        showError(
          `Multiple FileViewControlViewModel with Name='${viewControlName}' was found on the form.`
        );
        return;
      }

      const categoriesView = ViewService.instance.getByName(creationParams.categoriesViewAlias);
      if (!categoriesView) {
        showError(`Categories View:'${creationParams.categoriesViewAlias}' isn't found.`);
        return;
      }

      control.pagingMode = creationParams.pagingMode;
      control.pageLimit = creationParams.pageLimit;
      control.pageCountStatus = true;

      const controlType = new CardTypeEntryControl();
      controlType.name = viewControlName;
      controlType.type = CardControlTypes.ViewControlControlType;
      const fileControl = new FileListViewModel(
        controlType,
        model,
        null,
        FileSortingDirection.Ascending,
        null,
        false,
        creationParams.isCategoriesEnabled,
        creationParams.isManualCategoriesCreationDisabled,
        creationParams.isNullCategoryCreationDisabled,
        false,
        creationParams.isIgnoreExistingCategories,
        categoriesView,
        null,
        creationParams.previewControlName,
        fileTypes
      );

      fileControl.categoryFilter = async context => {
        const categories = context.categories;
        if (!categoriesView) {
          return categories;
        }

        const request = new TessaViewRequest(categoriesView.metadata);

        // Добавляем параметры из маппинга
        const parameters = addRequestParameters(
          creationParams.categoriesViewMapping,
          model,
          categoriesView
        );
        if (parameters) {
          request.parameters = parameters;
        }

        let result!: ITessaViewResult;
        await model.executeInContext(async () => (result = await categoriesView.getData(request)));

        const rows = result.rows || [];

        // категории из представления в порядке, в котором их
        // вернуло представление (кроме строчек null)
        const viewCategories = rows
          .filter(x => x.length > 0 && !!x[0])
          .map(x => new FileCategory(x[0] as string, x[1] as string, x[2] as number));

        const notNullCategories = categories.filter(x => x != null) as FileCategory[];
        // категории из представления плюс вручную добавленные или другие
        // присутствующие в карточке категории, кроме null
        const mainCategories = viewCategories.concat(...notNullCategories);

        const finalCategories: (FileCategory | null)[] = [];
        // делаем distinct
        mainCategories.forEach(category => {
          if (finalCategories.every(x => !FileCategory.equals(x, category))) {
            finalCategories.push(category);
          }
        });

        // добавляем наверх "Без категории" и возвращаем результирующий список
        finalCategories.splice(0, 0, null);

        return finalCategories;
      };

      await fileControl.initialize();
      model.info[viewControlName] = fileControl;
      FormCreationContext.current?.registerFileControl(fileControl);
      (model.controlsBag as Array<IControlViewModel>).push(fileControl);
      dispose.push(() => fileControl.unload(new ValidationResultBuilder()));
    }
  });
}

async function attachViewToFileControl(
  cardModel: ICardModel,
  viewControlName: string,
  initializationStrategy?: IViewControlInitializationStrategy,
  viewModifierAction?: (viewControl: ViewControlViewModel) => void,
  dispose: Array<Function | Lambda | null> = []
): Promise<FileListViewModel | null> {
  const viewControlViewModel = cardModel.controls.get(viewControlName) as ViewControlViewModel;
  if (!viewControlViewModel) {
    return null;
  }

  // viewControlViewModel.createRowFunc = createRowFunc;
  const fileControl = tryGetFromInfo<FileListViewModel | null>(
    cardModel.info,
    viewControlName,
    null
  );
  if (!fileControl) {
    throw new Error(`File control not found.`);
  }

  if (initializationStrategy) {
    viewControlViewModel.initializeStrategy(initializationStrategy, true);
    if (viewModifierAction) {
      viewModifierAction(viewControlViewModel);
    }

    if (viewControlViewModel.table) {
      viewControlViewModel.table.createRowAction = opt =>
        new TableFileRowViewModel(opt, fileControl);
    }

    await viewControlViewModel.initialize();

    initializeSelection(viewControlViewModel, fileControl);
    initializeGrouping(viewControlViewModel, fileControl);
    initializeFiltering(viewControlViewModel, fileControl);
    initializeClickCommands(viewControlViewModel, fileControl);
    initializeMenuButton(viewControlViewModel, fileControl);
    initializeContextMenu(viewControlViewModel, fileControl);

    await viewControlViewModel.initialRefresh();
  }

  dispose.push(
    reaction(
      () => fileControl.files,
      () => viewControlViewModel.refreshWithDelay(150),
      {
        equals: () => false
      }
    )
  );

  return fileControl;
}

function initializeSelection(
  viewControl: ViewControlViewModel,
  fileControl: FileListViewModel,
  dispose: Array<Function | Lambda | null> = []
) {
  let lastSelectedRows: ReadonlyArray<ReadonlyMap<string, any>> | null = null;
  dispose.push(
    reaction(
      () => viewControl.selectedRows,
      () => {
        processSelection(viewControl, fileControl, lastSelectedRows);
        lastSelectedRows = viewControl.selectedRows ? [...viewControl.selectedRows] : null;
      },
      {
        equals: () => false
      }
    ),
    reaction(
      () => fileControl.fileVersion,
      file => {
        const { data: rows, selectionState } = viewControl;
        if (!rows || !selectionState) {
          return;
        }
        const { selectedRow } = selectionState;

        const newSelectedFile = viewControl.data?.find(row => {
          const fileViewModel = row.get('FileViewModel') as FileViewModel;
          return fileViewModel.model.lastVersion === file;
        });
        if (!newSelectedFile || !selectedRow) {
          return;
        }
        if (selectedRow !== newSelectedFile) {
          selectionState.unSelectRow(selectedRow);
          selectionState.setSelection(newSelectedFile);
        }
      }
    )
  );
}

function processSelection(
  viewControl: ViewControlViewModel,
  fileControl: FileListViewModel,
  lastSelectedRows: ReadonlyArray<ReadonlyMap<string, any>> | null
) {
  const selectedFiles: Array<FileViewModel> =
    viewControl.selectedRows?.map(x => x.get('FileViewModel')) ?? [];
  const notSelectedFiles: Array<FileViewModel> = fileControl.files.filter(
    x => !selectedFiles.includes(x)
  );

  for (const file of selectedFiles) {
    file.selected = true;
  }
  for (const file of notSelectedFiles) {
    file.selected = false;
  }

  const selectedRow = viewControl.selectedRow;
  const selectedFile: FileViewModel = selectedRow?.get('FileViewModel');
  const multiSelect =
    Keyboard.instance.ctrl ||
    Keyboard.instance.shift ||
    viewControl.multiSelectEnabled ||
    (viewControl.selectedRows && viewControl.selectedRows.length > 1);
  const preview = fileControl.manager;

  if (preview) {
    if (selectedFiles.length === 0) {
      preview.reset();
      return;
    }
    /*
      отключать предпросмотр,
      если файл был загружен в файловом контролле,
      но не сохранен в карточке.
    */
    if (selectedFile.isFileStateCreated() && !allowPreviewExtensions(selectedFile.model)) {
      preview.reset();
      return;
    }
    const selectedRowViewModel = viewControl.table?.rows.find(x => x.data === selectedRow);
    const selectionContext = selectedRowViewModel?.selectionContext;
    const shouldShowPreview =
      !multiSelect &&
      selectedFile &&
      // файл выбран нажатием на строку
      selectionContext?.selectedBy === 'row' &&
      selectedRow &&
      // и единственная выбранная строка - не результат деселекта других строк
      // (текущая строка не содержится в массиве выбранных строк)
      !lastSelectedRows?.includes(selectedRow);

    if (shouldShowPreview) {
      const {
        model: { lastVersion }
      } = selectedFile;

      const getFiles = () => {
        const files: FileViewModel[] =
          viewControl.table?.rows.map(row => row.data.get('FileViewModel')) ?? [];

        return files
          .map(vm => vm.model.lastVersion)
          .filter(x => checkCanDownloadFile(x).items.length === 0);
      };

      const previewList = new PreviewFilesList(getFiles, lastVersion);

      preview.showPreview(previewList);
      return;
    }

    const shouldShowMessage =
      multiSelect ||
      // файл выбран нажатием на чекбокс
      selectionContext?.selectedBy === 'checkbox' ||
      // или осталась одна выделенная строка в результате деселекта всех других
      // (текущая строка содержится в массиве выбранных строк)
      (selectedRow && lastSelectedRows?.includes(selectedRow));

    if (shouldShowMessage) {
      preview.reset();
      const { main, additional } = getSelectedFilesPreviewMessage(selectedFiles);
      preview.setMessage(main, additional);
      return;
    }
  }
}

function initializeGrouping(
  viewControl: ViewControlViewModel,
  fileControl: FileListViewModel,
  dispose: Array<Function | Lambda | null> = []
) {
  const table = viewControl.table;
  if (!table) {
    return;
  }

  const groupCaptionColumn = table.columns.find(x => x.columnName === 'GroupCaption');
  if (groupCaptionColumn) {
    groupCaptionColumn.visibility = false;
  }

  dispose.push(
    reaction(
      () => table.groupingColumn,
      groupingColumn => {
        if (!groupingColumn) {
          fileControl.selectedGrouping = null;
        }
      }
    )
  );

  dispose.push(
    reaction(
      () => fileControl.selectedGrouping,
      grouping => {
        if (!grouping) {
          table.setGrouping(null);
          const categoryColumn = table.columns.find(x => x.columnName === 'CategoryCaption');
          if (categoryColumn) {
            categoryColumn.visibility = true;
          }
        } else {
          if (grouping.name === 'Category') {
            const categoryColumn = table.columns.find(x => x.columnName === 'CategoryCaption');
            if (categoryColumn) {
              categoryColumn.visibility = false;
            }
          } else {
            const categoryColumn = table.columns.find(x => x.columnName === 'CategoryCaption');
            if (categoryColumn) {
              categoryColumn.visibility = true;
            }
          }

          const groupColumnMetadata = viewControl.viewMetadata!.columns.get('GroupName');
          const groupCaptionColumnMetadata = viewControl.viewMetadata!.columns.get('GroupCaption');
          if (groupColumnMetadata && groupCaptionColumnMetadata) {
            table.setGrouping(groupColumnMetadata, groupCaptionColumnMetadata);
          }
        }

        const groupCaptionColumn = table.columns.find(x => x.columnName === 'GroupCaption');
        if (groupCaptionColumn) {
          groupCaptionColumn.visibility = false;
        }

        // При изменении группировки перерассчитываются значения полей GroupColumn и GroupName, поэтому нужно сделать рефреш представления
        viewControl.refresh();
      }
    )
  );
}

function initializeFiltering(
  viewControl: ViewControlViewModel,
  fileControl: FileListViewModel,
  dispose: Array<Function | Lambda | null> = []
) {
  dispose.push(
    reaction(
      () => fileControl.selectedFiltering,
      filtering => {
        if (viewControl.isDataLoading) {
          return;
        }

        const previousFilteringParameter = viewControl.parameters.parameters.find(
          x => x.metadata?.alias === 'FilterParameter'
        );
        if (previousFilteringParameter) {
          viewControl.parameters.removeParameters(previousFilteringParameter);
        }
        if (filtering && filtering instanceof FileGroupingFiltering) {
          const parameterMetadata = new ViewParameterMetadata();
          parameterMetadata.caption = filtering.grouping.caption;
          parameterMetadata.alias = 'FilterParameter';
          parameterMetadata.schemeType = SchemeType.String;
          const newFilteringParameter = new RequestParameterBuilder()
            .withMetadata(parameterMetadata)
            .addCriteria(ViewCriteriaOperators.EqualsTo, filtering.caption, filtering.caption)
            .asRequestParameter();
          viewControl.parameters.addParameters(newFilteringParameter);
        }
        viewControl.refresh();
      }
    )
  );
}

function initializeClickCommands(
  viewControl: ViewControlViewModel,
  _fileControl: FileListViewModel
) {
  viewControl.doubleClickAction = async info => {
    const table = viewControl.table;
    if (!table) {
      return;
    }

    const row = table.rows.find(x => x.data === info.selectedObject);
    if (!row) {
      return;
    }

    const actions = row.getContextMenu();
    const openForRead = actions.find(x => x.name === 'OpenForRead');
    if (openForRead) {
      openForRead.executeAction?.();
    }

    if (Keyboard.instance.alt) {
      const openForEdit = actions.find(x => x.name === 'OpenForEdit');
      if (openForEdit) {
        openForEdit.executeAction?.();
      }
    }
  };
}

function initializeMenuButton(viewControl: ViewControlViewModel, fileControl: FileListViewModel) {
  const refreshButtonIndex = viewControl.bottomItems.findIndex(
    x => x.content instanceof ViewControlRefreshButtonViewModel
  );

  if (refreshButtonIndex !== -1) {
    viewControl.bottomItems.splice(refreshButtonIndex, 1);
  }

  // Вырезать пейджинг, только если он не задан.
  if (
    viewControl.pagingMode === Paging.No ||
    (viewControl.pagingMode === Paging.Optional && !viewControl.optionalPagingStatus)
  ) {
    const pagingIndex = viewControl.bottomItems.findIndex(
      x => x.content instanceof ViewControlPagingViewModel
    );
    if (refreshButtonIndex !== -1) {
      viewControl.bottomItems.splice(pagingIndex, 1);
    }
  }

  const bottomPanelButtons = new ViewControlButtonPanelViewModel(viewControl);
  viewControl.bottomItems.splice(0, 0, new ViewControlToolbarItem(bottomPanelButtons, 'left'));

  const uploadButton = fileControl.getControlActions().find(button => button.name === 'Upload');
  const permissions = fileControl.fileContainer.permissions;

  if (uploadButton && !uploadButton.isCollapsed) {
    const uploadFileButton = UIButton.create({
      icon: 'm-plus',
      name: uploadButton.name,
      className: 'files-control-button',
      buttonAction: (_b, e) => e && uploadButton.action && uploadButton.action(e),
      tooltip: '$UI_Controls_FilesControl_UploadFiles',
      visibility: permissions.canAdd ? Visibility.Visible : Visibility.Collapsed,
      theme: 'transparent',
      type: 'small'
    });

    runInAction(() => {
      bottomPanelButtons.buttons.push(uploadFileButton);
    });
  }

  const addMenuButton = new ShowContextMenuButtonViewModel(viewControl, fileControl);
  viewControl.bottomItems.push(new ViewControlToolbarItem(addMenuButton, 'left'));
}

function initializeContextMenu(viewControl: ViewControlViewModel, fileControl: FileListViewModel) {
  if (viewControl.table) {
    viewControl.table.rowContextMenuGenerators.push(ctx => {
      const row = ctx.row as TableFileRowViewModel;

      // если строка, по которой кликнули выделена, то мы учитываем все выделенные строки
      const withAnotherSelectedFiles = row.isSelected;

      const actions = fileControl.getFileActions(row.fileViewModel, withAnotherSelectedFiles);
      ctx.menuActions.push(...actions);
    });
  }

  viewControl.onOpenGlobalContextMenu.add(ctx => {
    const addMenuButton = viewControl.bottomItems.find(
      x => 'content' in x && x.content instanceof ShowContextMenuButtonViewModel
    )?.content as ShowContextMenuButtonViewModel | undefined;

    if (!addMenuButton) {
      return;
    }

    ctx.menuActions.push(...addMenuButton.getMenuActions());
  });
}

function initializeDefaultGroupInFileControl(
  fileControl: FileListViewModel | null,
  settings: IStorage | null
) {
  if (fileControl) {
    const defaultGroup = tryGetFromSettings<string>(settings, 'DefaultGroup', '');
    if (defaultGroup) {
      fileControl.selectedGrouping =
        fileControl.groupings.find(x => x.name === defaultGroup) ?? null;
    }
  }
}

function initializeExtensionForTableForm(
  context: ICardTypeExtensionContext,
  e: GridRowEventArgs,
  disposes: Array<(() => void) | null>
) {
  const cardViewControlViewModels = e.rowModel!.controlsBag.filter(
    x => x instanceof ViewControlViewModel
  ) as ViewControlViewModel[];
  if (cardViewControlViewModels.length > 0) {
    const fileControl = attachViewToFileControlCore(context, e.rowModel!);
    if (!fileControl) {
      const tables = e.rowModel!.controlsBag.filter(
        x => x instanceof GridViewModel
      ) as GridViewModel[];
      for (const table of tables) {
        disposes.push(
          table.rowInvoked.addWithDispose(e => {
            if (e.action == GridRowAction.Inserted || e.action == GridRowAction.Opening) {
              initializeExtensionForTableForm(context, e, disposes);
            }
          })
        );
      }
    }
  }
}

async function attachViewToFileControlCore(
  extensionContext: ICardTypeExtensionContext,
  cardModel: ICardModel
): Promise<FileListViewModel | null> {
  let strategy: IViewControlInitializationStrategy | null = null;
  for (const handler of tryGetFileViewExtensionInitializationStrategyHandlers(cardModel) ?? []) {
    strategy = handler(extensionContext, cardModel);
    if (!!strategy) {
      break;
    }
  }
  const settings = extensionContext.settings;
  const filesViewAlias = tryGetFromSettings<string>(settings, 'FilesViewAlias', '');
  if (!filesViewAlias) {
    return null;
  }

  return await attachViewToFileControl(
    cardModel,
    filesViewAlias,
    strategy ??
      new FilesViewCardControlInitializationStrategy(new FilesViewControlContentItemsFactory())
  );
}
