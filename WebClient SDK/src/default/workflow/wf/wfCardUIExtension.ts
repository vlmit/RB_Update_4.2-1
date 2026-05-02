import { WfResolutionTaskInfo } from './wfResolutionTaskInfo';
import {
  UIContext,
  IUIContext,
  tryGetFromInfo,
  showConfirmWithCancel,
  MenuAction,
  SeparatorMenuAction
} from 'tessa/ui';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  CardSavingRequest,
  CardSavingMode,
  ICardEditorModel,
  ICardModel,
  IBlockViewModel,
  CardEditorOperationType,
  CardToolbarAction,
  StateRestoreMode
} from 'tessa/ui/cards';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms';
import {
  TaskViewModel,
  TaskWorkspaceState,
  TaskViewModelEventArgs,
  TaskFormContentViewModelEventArgs
} from 'tessa/ui/cards/tasks';
import { LabelViewModel, FileListViewModel, FileTagViewModel } from 'tessa/ui/cards/controls';
import { getTypedFieldValue, createTypedField, DotNetType, Visibility } from 'tessa/platform';
import { ArrayStorage, MapStorage } from 'tessa/platform/storage';
import { LocalizationManager } from 'tessa/localization';
import { CardSection, CardRow, CardRowState, CardTaskHistoryItem, CardTask } from 'tessa/cards';
import { Guid, StorageAccessor, StorageHelper, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardHelper, WfHelper } from '@tessa/platform';

const MainCardCategoryId = 'ef065661-6613-4c87-bf93-0e1dd558a751';

/**
 * Расширение для модификации UI карточки и заданий в соответствии с бизнес-процессами Workflow.
 */
@extension({ name: 'WfCardUIExtension' })
export class WfCardUIExtension extends CardUIExtension {
  //#region fields

  private _disposes: Array<(() => void) | null> = [];

  //#endregion

  //#region CardUIExtension

  public initialized(context: ICardUIExtensionContext): void {
    // если нет ни одного задания резолюции в истории, то выходим
    const isTaskCard = context.model.cardType.id === CardHelper.WfTaskCardTypeID;
    const taskHistory = context.model.tryGetTaskHistory();
    if (
      !taskHistory ||
      (!isTaskCard &&
        taskHistory.rows.every(x => !!x.parentRowId || !WfHelper.taskTypeIsResolution(x.typeId)))
    ) {
      return;
    }

    if (isTaskCard) {
      // если предыдущая карточка отличалась от карточки-сателлита задачи (т.е. была основной карточкой), то не переносим состояние контролов
      // (например, группировку файлов) в открываемую карточку
      const editor = context.uiContext.cardEditor;
      if (
        editor &&
        editor.cardModel &&
        editor.cardModel.cardType.id !== CardHelper.WfTaskCardTypeID
      ) {
        context.model.stateIsInitialized = true;
      }

      let typeCaption = '';
      const task = context.card.tasks[0];
      if (task) {
        typeCaption = task.typeCaption;
      } else {
        const historyItem = context.card.taskHistory[0];
        typeCaption = historyItem ? historyItem.typeCaption : '';
      }

      if (typeCaption && context.model.forms.length > 0) {
        context.model.forms[0].tabCaption = typeCaption;
      }

      // ссылка "вернуться в карточку"
      const navigateMainCard = context.model.controls.get('NavigateMainCard') as LabelViewModel;
      if (navigateMainCard) {
        navigateMainCard.controlVisibility = Visibility.Collapsed;
        const toolbarAction = new CardToolbarAction({
          name: 'NavigateMainCard',
          caption: navigateMainCard.text,
          toolTip: navigateMainCard.tooltip,
          icon: '',
          command: () => WfCardUIExtension.taskCardNavigateAction()
        });
        context.toolbar.clearItems();
        context.toolbar.addItem(toolbarAction);
      }

      const taskFiles = context.model.controls.get('TaskFiles') as FileListViewModel;
      if (taskFiles) {
        if (taskFiles.isCategoriesEnabled) {
          const prevFilter = taskFiles.categoryFilter;
          if (!prevFilter) {
            taskFiles.categoryFilter = async context => {
              return context.categories.filter(x => !x || x.id !== MainCardCategoryId);
            };
          } else {
            taskFiles.categoryFilter = async categories => {
              const cats = (await prevFilter(categories)) || [];
              return cats.filter(x => !x || x.id !== MainCardCategoryId);
            };
          }
        }

        // делаем группу "Файлы карточки" всегда первой
        taskFiles.groupSorting = groups => {
          groups.forEach(group => {
            if (group.caption === '$WfResolution_MainCardFileCategory') {
              group.order = -10;
            }
          });
        };

        // теги-иконки для файлов от этой задачи
        for (const file of taskFiles.files) {
          const cardFile = context.card.files.find(x => x.rowId === file.id);
          if (!cardFile || !cardFile.externalSource) {
            file.tag = new FileTagViewModel('ta icon-thin-020', 'rgba(0, 176, 0, 0.19)');
          }
        }

        this._disposes.push(
          taskFiles.containerFileAdding.addWithDispose(e => {
            // если создаётся копия файла из виртуальных файлов карточки, то надо сбросить категорию файла на "Без категории";
            // если выполняется копирование файла в основную карточку, то меняется категория, но Origin остаётся как null
            const file = e.file;
            const copiedToMainCard = !!getTypedFieldValue(file.info['CopiedToMainCard']);
            const cardFile = context.card.files.find(x => x.rowId === file.id);
            let categoryIsMainCard = !!file.category && file.category.id === MainCardCategoryId;
            const mainCategoryOfCardFile =
              cardFile?.categoryId && cardFile?.categoryId === MainCardCategoryId;
            if (mainCategoryOfCardFile && !copiedToMainCard) {
              cardFile!.categoryCaption = null;
              cardFile!.categoryId = null;
            }

            if (categoryIsMainCard && file.origin) {
              file.category = null;
              file.origin = null;
              file.info['CopiedToMainCard'] = createTypedField(true, DotNetType.Boolean);
              categoryIsMainCard = false;
            }
          })
        );

        this._disposes.push(
          taskFiles.containerFileAdded.addWithDispose(e => {
            const file = e.file;
            const categoryIsMainCard = !!file.category && file.category.id === MainCardCategoryId;
            if (!categoryIsMainCard) {
              const fileViewModel = e.control.files.find(x => x.id === file.id);
              if (fileViewModel) {
                fileViewModel.tag = new FileTagViewModel(
                  'ta icon-thin-020',
                  'rgba(0, 176, 0, 0.19)'
                );
              }
            }
          })
        );
      }
    } else {
      // открывается карточка, отличная от сателлита, причём в Info присутствует сохранённый State от основной карточки
      // (до того, как мы ушли в сателлит), поэтому восстанавливаем состояние
      const editor = context.uiContext.cardEditor!;
      const mainFormState = tryGetFromInfo(editor.info, 'MainCardState', null);
      if (mainFormState) {
        context.model.mainForm?.setState(mainFormState, StateRestoreMode.All) ||
          context.model.wysiwygForm?.applyState(mainFormState);
        context.model.stateIsInitialized = true;

        // восстанавливаем его только один раз
        delete editor.info['MainCardState'];
      }
      context.toolbar.removeItemIfExists('NavigateMainCard');
    }

    // добавляем обработчики, связанные с отображением задания резолюции и его кнопок
    if (!context.model.inSpecialMode) {
      (context.model.tryGetTasks() ?? []).forEach(task => {
        if (WfHelper.taskTypeIsResolution(task.taskModel.cardType.id!)) {
          const taskInfo = WfCardUIExtension.createTaskInfo(task);

          WfCardUIExtension.modifyResolutionTask(taskInfo, context.model, isTaskCard, true);
          this._disposes.push(
            task.workspaceChanged.addWithDispose(() => {
              WfCardUIExtension.modifyResolutionTask(taskInfo, context.model, isTaskCard, false);
            })
          );

          this._disposes.push(() => taskInfo.unsubscribe());

          this._disposes.push(
            task.postponeMetadataInitializing.addWithDispose(
              WfCardUIExtension.postponeMetadataInitializing
            )
          );
          this._disposes.push(
            task.postponeContentInitializing.addWithDispose(
              WfCardUIExtension.postponeContentInitializing
            )
          );
        }
      });
    }

    if (!isTaskCard) {
      taskHistory.itemContextMenuGenerators.push(ctx => {
        if (
          !context.model.inSpecialMode &&
          WfHelper.taskTypeIsResolution(ctx.historyItem.model.typeId)
        ) {
          const fileCount = tryGetFromInfo<number>(ctx.historyItem.model.info, 'FileCount', 0);

          ctx.menuActions.splice(
            0,
            0,
            new MenuAction(
              'WfResolution_NavigateTaskCard',
              fileCount > 0
                ? LocalizationManager.instance.format(
                    '$WfTaskFiles_ShowFilesTagCount_ContextMenu',
                    fileCount
                  )
                : '$WfTaskFiles_ShowFilesTag_ContextMenu',
              null,
              () => WfCardUIExtension.taskCardNavigateAction(ctx.historyItem.model.rowId)
            ),
            new SeparatorMenuAction(false, 'WfResolution_Visualize_Separator')
          );
        }
      });

      taskHistory.rows.forEach(item => {
        let fileCount;
        if (
          WfHelper.taskTypeIsResolution(item.model.typeId) &&
          (fileCount = item.model.info['FileCount']) &&
          TypedField.getNumber(fileCount) > 0
        ) {
          item.setTag(
            'icon-thin-043',
            LocalizationManager.instance.format(
              context.model.inSpecialMode
                ? '$WfTaskFiles_FilesTagCount_ToolTip'
                : '$WfTaskFiles_ShowFilesTagCount_ToolTip',
              TypedField.get(fileCount)
            ),
            context.model.inSpecialMode
              ? () => {}
              : () => WfCardUIExtension.taskCardNavigateAction(item.rowId)
          );
        }
      });
    }
  }

  public reopening(context: ICardUIExtensionContext): void {
    const editor = context.uiContext.cardEditor;
    if (!editor || !context.getRequest) {
      return;
    }

    if (context.model.cardType.id !== CardHelper.WfTaskCardTypeID) {
      // если была открыта не карточка-сателлит, а другая карточка, и сейчас открывается карточка-сателлит,
      // то сохраним информацию по выбранной вкладке и прочим параметрам для основной карточки, чтобы потом мы смогли всё восстановить
      if (context.getRequest.cardTypeId === CardHelper.WfTaskCardTypeID) {
        editor.info['MainCardState'] =
          context.model.mainForm?.getState() ?? context.model.wysiwygForm?.getState();
      }
    } else if (editor.currentOperationType === CardEditorOperationType.SaveAndRefresh) {
      // если была открыта карточка-сателлит, то при её рефреше сразу после сохранения с завершением задания
      // мы будем загружать основную карточку, а не сателлит
      const storeResponse = editor.lastData.storeResponse;
      let responseCardId;

      if (
        storeResponse &&
        !!(responseCardId = tryGetFromInfo(storeResponse.info, 'NextCardID', null))
      ) {
        // открываем основную карточку вместо карточки задания, если было завершено задание (про это знает серверное расширение)
        context.getRequest.cardId = responseCardId;
        context.getRequest.cardTypeId = tryGetFromInfo(storeResponse.info, 'NextCardTypeID', null);
        context.getRequest.cardTypeName = null;
      }
    }
  }

  public finalized(): void {
    for (const dispose of this._disposes) {
      if (dispose) {
        dispose();
      }
    }
    this._disposes.length = 0;
  }

  //#endregion

  //#region methods

  private static createTaskInfo(taskViewModel: TaskViewModel): WfResolutionTaskInfo {
    const taskCard = taskViewModel.taskModel.cardTask!.tryGetCard();

    let childrenSection: CardSection | undefined;
    let childrenRows!: ArrayStorage<CardRow>;
    const hasChildren =
      !!taskCard &&
      !!taskCard.tryGetSections() &&
      !!(childrenSection = taskCard.sections.tryGet('WfResolutionChildrenVirtual')) &&
      !!(childrenRows = childrenSection.tryGetRows()!) &&
      childrenSection.rows.length > 0;

    const hasIncompleteChildren =
      hasChildren &&
      childrenRows.some(x => x.state !== CardRowState.Deleted && !x.tryGetField('Completed'));

    return new WfResolutionTaskInfo(taskViewModel, hasChildren, hasIncompleteChildren);
  }

  private static tryGetFilesCount(
    tasks: ArrayStorage<CardTask>,
    tasksHistory: ArrayStorage<CardTaskHistoryItem>,
    rowID: string
  ): number {
    let currentTask = tasks.find(t => Guid.equals(t.rowId, rowID));
    if (currentTask?.parentRowId) {
      let parentTask: CardTask | undefined;
      do {
        parentTask = tasks.find(t => Guid.equals(t.rowId, currentTask?.parentRowId));
        currentTask = parentTask;
      } while (parentTask?.parentRowId);

      const count = new StorageAccessor(parentTask?.card.info ?? {}).tryGetNumber('FileCount');
      if (count !== null) {
        return count;
      }
    }

    let currentHistoryItem = tasksHistory.find(t => Guid.equals(t.rowId, rowID));
    if (currentHistoryItem?.parentRowId) {
      let parentHistoryItem: CardTaskHistoryItem | undefined;
      do {
        parentHistoryItem = tasksHistory.find(t =>
          Guid.equals(t.rowId, currentHistoryItem?.parentRowId)
        );
        currentHistoryItem = parentHistoryItem;
      } while (parentHistoryItem?.parentRowId);

      return new StorageAccessor(parentHistoryItem?.info ?? {}).tryGetNumberOrDefault('FileCount');
    }

    return 0;
  }

  private static modifyResolutionTask(
    taskInfo: WfResolutionTaskInfo,
    model: ICardModel,
    isTaskCard: boolean,
    subscribeToTaskModel: boolean
  ) {
    const taskViewModel = taskInfo.control;

    if (!model.inSpecialMode) {
      const workspaceState = taskViewModel.taskWorkspace.state;

      let filesCount = 0;

      if (isTaskCard) {
        const filesControl = model.controls.get('TaskFiles');
        if (filesControl) {
          const taskFiles = filesControl as FileListViewModel;
          filesCount = taskFiles.files.filter(
            f => f.model.category?.id !== MainCardCategoryId
          ).length;
        }
      } else {
        filesCount =
          new StorageAccessor(taskViewModel.taskModel.cardTask!.info).tryGetNumberOrDefault(
            'FileCount'
          ) ||
          WfCardUIExtension.tryGetFilesCount(
            model.card.tasks,
            model.card.taskHistory,
            taskViewModel.taskModel.cardTask!.rowId
          );
      }

      taskViewModel.taskWorkspace.setTag(
        'm-attach',
        () => {
          WfCardUIExtension.taskCardNavigateAction(taskViewModel.taskModel.cardTask!.rowId);

          // В компактном режиме закрываем диалог с задачами
          if (model.mainForm instanceof DefaultFormTabWithTasksViewModel) {
            model.mainForm.closeMobileTasks();
          }
        },
        filesCount > 0 ? `+${filesCount}` : null
      );

      if (
        isTaskCard ||
        workspaceState === TaskWorkspaceState.Locked ||
        workspaceState === TaskWorkspaceState.LockedForPerformer ||
        workspaceState === TaskWorkspaceState.UnlockedForPerformer ||
        workspaceState === TaskWorkspaceState.Initial ||
        workspaceState === TaskWorkspaceState.DefaultForm
      ) {
        if (isTaskCard) {
          taskViewModel.taskWorkspace.setLink('$WfTaskFiles_ReturnFromFilesLink', () => {
            WfCardUIExtension.taskCardNavigateAction(taskViewModel.taskModel.cardTask!.rowId);
            if (model.mainForm instanceof DefaultFormTabWithTasksViewModel) {
              model.mainForm.closeMobileTasks();
            }
          });
        } else {
          if (filesCount > 0) {
            const message = LocalizationManager.instance.format(
              '$WfTaskFiles_ShowFilesLinkTemplate',
              filesCount
            );
            taskViewModel.taskWorkspace.setLink(message, () => {
              WfCardUIExtension.taskCardNavigateAction(taskViewModel.taskModel.cardTask!.rowId);
              if (model.mainForm instanceof DefaultFormTabWithTasksViewModel) {
                model.mainForm.closeMobileTasks();
              }
            });
          }
        }
      }
    }

    const form = taskViewModel.taskWorkspace.form;
    let childResolutions!: IBlockViewModel;
    // скрываем таблицу с дочерними резолюциями, если таких резолюций нет
    if (
      form &&
      !taskInfo.hasChildren &&
      !!(childResolutions = form.blocks.find(x => x.name === 'ChildResolutions')!)
    ) {
      childResolutions.blockVisibility = Visibility.Collapsed;
    }

    if (subscribeToTaskModel) {
      const taskCard = taskViewModel.taskModel.cardTask!.tryGetCard();
      let taskSections!: MapStorage<CardSection>;
      if (taskCard && !!(taskSections = taskCard.tryGetSections()!)) {
        const resolutionSection = taskSections.tryGet('WfResolutions');
        if (resolutionSection) {
          taskInfo.subscribeToResolutionSectionAndUpdate(resolutionSection);
        }

        const performersSection = taskSections.tryGet('WfResolutionPerformers');
        if (performersSection) {
          taskInfo.subscribeToPerformersAndUpdate(performersSection.rows);
        }
      }
    } else {
      taskInfo.update();
    }
  }

  public static taskCardNavigateAction = async (taskRowId?: string): Promise<void> => {
    const context = UIContext.current;
    const editor = context.cardEditor;

    if (!editor || !editor.cardModel) {
      return;
    }

    const model = editor.cardModel;

    if (await model.hasChanges()) {
      const result = await showConfirmWithCancel('$WfTaskFiles_SaveChangesConfirmation');
      if (result === null) {
        return;
      }

      if (result) {
        const success = await editor.saveCard(
          context,
          {},
          new CardSavingRequest(CardSavingMode.KeepPreviousCard)
        );
        if (success) {
          WfCardUIExtension.beginTaskCardNavigateAction(taskRowId, model, editor, context, null);
        }

        return;
      }
    }

    WfCardUIExtension.beginTaskCardNavigateAction(taskRowId, model, editor, context, model.digest);
  };

  private static beginTaskCardNavigateAction(
    taskRowId: string | null | undefined,
    model: ICardModel,
    editor: ICardEditorModel,
    context: IUIContext,
    digest: string | null
  ) {
    if (model.cardType.id === CardHelper.WfTaskCardTypeID) {
      const section = model.card.sections.get('Satellites');
      const mainCardId = section.fields.get<string>('MainCardID');

      if (mainCardId) {
        const info = {};

        const permissionsCalculated = model.card.info['kr_permissions_calculated'];
        if (permissionsCalculated != undefined) {
          info['kr_permissions_calculated'] = permissionsCalculated;
          info['kr_calculate_permissions'] = permissionsCalculated;
        }

        editor.openCard({
          cardId: mainCardId,
          context,
          info
        });
      }
    } else if (taskRowId) {
      const info = {};
      if (digest) {
        info['.digest'] = TypedField.createString(model.digest);
      }

      const permissionsCalculated = model.card.info['kr_permissions_calculated'];
      if (permissionsCalculated != undefined) {
        info['kr_permissions_calculated'] = permissionsCalculated;
      }

      editor.openCard({
        cardId: taskRowId,
        cardTypeId: CardHelper.WfTaskCardTypeID,
        cardTypeName: CardHelper.WfTaskCardTypeName,
        context,
        info
      });
    }
  }

  private static async postponeMetadataInitializing(e: TaskViewModelEventArgs): Promise<void> {
    const targetMetadata = e.task.postponeMetadata;
    const targetType = targetMetadata.cardType;
    await targetType.ensureDataLoaded();
    const targetForm = targetType.tryGetMainFormForTask();
    const targetBlocks = targetForm?.blocks;

    // удаляем блок с информацией по заданию, т.к. он будет скопирован ниже из основной формы
    if (targetBlocks && targetBlocks.length > 0) {
      targetBlocks.splice(0, 1);
    }

    // копируем все блоки из основной формы задания в начало формы откладывания
    const sourceType = e.task.taskModel.cardType.clone();
    await sourceType.ensureDataLoaded();
    const sourceForm = sourceType.tryGetMainFormForTask();
    if (targetForm && sourceForm) {
      if (targetBlocks) {
        targetBlocks.splice(0, 0, ...sourceForm.blocks);
      }

      // копируем настройки формы из основной формы задания в форму откладывания
      targetForm.formSettings = StorageHelper.clone(sourceForm.formSettings);
    }

    // копируем метаинформацию по виртуальной таблице для формы откладывания
    const metadataSection = e.task.taskModel.cardMetadata.sections.getSectionByName(
      'WfResolutionChildrenVirtual'
    );
    if (metadataSection) {
      targetMetadata.sections.push(metadataSection.clone());

      const sourceItem = sourceType.schemeItems.find(x => x.sectionId === metadataSection.id);
      if (sourceItem) {
        targetType.schemeItems.push(sourceItem);
      }
    }
  }

  private static async postponeContentInitializing(
    e: TaskFormContentViewModelEventArgs
  ): Promise<void> {
    const sourceSection = e.task.taskModel.card.sections.tryGet('WfResolutionChildrenVirtual');
    if (!sourceSection) {
      return;
    }

    const targetSection = e.card.sections.tryGet('WfResolutionChildrenVirtual');
    if (!targetSection) {
      return;
    }

    targetSection.setFrom(sourceSection);
  }

  //#endregion
}
