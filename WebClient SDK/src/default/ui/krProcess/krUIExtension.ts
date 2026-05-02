import {
  CardUIExtension,
  IBlockViewModel,
  ICardModel,
  ICardUIExtensionContext,
  IControlViewModel
} from 'tessa/ui/cards';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms';
import { Visibility } from 'tessa/platform';
import { CardTypeFlags } from 'tessa/cards/types';
import {
  createNavigateBackAction,
  createUnlockForPerformerAction,
  saveCardWithTaskModifier,
  TaskAction,
  TaskActionType,
  TaskActionViewModel,
  TaskFormContentViewModelEventArgs,
  TaskGroupingType,
  TaskNavigator,
  TaskViewModel,
  TaskViewModelEventArgs,
  TaskWorkspaceState
} from 'tessa/ui/cards/tasks';
import {
  CardRowState,
  CardSectionType,
  CardTableType,
  CardTaskAction,
  CardTaskFlags,
  CardTaskState
} from 'tessa/cards';
import { ICardMetadataFunctionRole } from 'tessa/cards/metadata';
import { deserializeFromTypedToPlain } from 'tessa/platform/serialization';
import { Flags, IStorage, StorageAccessor, StorageHelper } from '@tessa/core';
import { clone } from 'tessa/platform/storage';
import { GridViewModel, LabelViewModel } from 'tessa/ui/cards/controls';
import { extension, inject } from '@tessa/application';
import { defaultLayout } from 'components/cardElements/grid';
import { runInAction } from 'mobx';
import { ICardSingletonCache, ICardSingletonCache$ } from '@tessa/platform';
import { ISignFilesProvider, ISignFilesProvider$ } from './signFiles';

/**
 * Скрывает результаты запроса комментария из задания согласования если комментарий не запрашивался.
 * Скрывает поле "Комментарий" для варианта завершения "Согласовать", если установлена соответствующая настройка.
 */
@extension({ name: 'KrUIExtension' })
export class KrUIExtension extends CardUIExtension {
  constructor(
    @inject(ICardSingletonCache$) private readonly _cardSingletonCache: ICardSingletonCache,
    @inject(ISignFilesProvider$) private readonly _signFilesProvider: ISignFilesProvider
  ) {
    super();
  }

  private _commentIsHiddenForApproval: boolean | null = null;
  private _disposes: Array<(() => void) | null> = [];

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
      targetForm.formSettings = clone(sourceForm.formSettings);
    }
    // копируем метаинформацию по виртуальной таблице для формы откладывания
    const krCommentsInfoVirtual =
      e.task.taskModel.cardMetadata.sections.getSectionByName('KrCommentsInfoVirtual');
    if (krCommentsInfoVirtual) {
      targetMetadata.sections.push(krCommentsInfoVirtual.clone());

      const sourceItem = sourceType.schemeItems.find(x => x.sectionId === krCommentsInfoVirtual.id);
      if (sourceItem) {
        targetType.schemeItems.push(sourceItem);
      }
    }

    const krAdditionalApprovalInfoVirtual = e.task.taskModel.cardMetadata.sections.getSectionByName(
      'KrAdditionalApprovalInfoVirtual'
    );
    if (krAdditionalApprovalInfoVirtual) {
      targetMetadata.sections.push(krAdditionalApprovalInfoVirtual.clone());

      const sourceItem = sourceType.schemeItems.find(
        x => x.sectionId === krAdditionalApprovalInfoVirtual.id
      );
      if (sourceItem) {
        targetType.schemeItems.push(sourceItem);
      }
    }

    const metadataSection = e.task.taskModel.cardMetadata.sections.getSectionByName(
      'KrAdditionalApprovalsRequestedInfoVirtual'
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
    let sourceSection = e.task.taskModel.card.sections.tryGet('KrCommentsInfoVirtual');
    if (!sourceSection) {
      return;
    }

    let targetSection = e.card.sections.tryGet('KrCommentsInfoVirtual');
    if (!targetSection) {
      return;
    }

    targetSection.setFrom(sourceSection);

    sourceSection = e.task.taskModel.card.sections.tryGet('KrAdditionalApprovalInfoVirtual');
    if (!sourceSection) {
      return;
    }

    targetSection = e.card.sections.tryGet('KrAdditionalApprovalInfoVirtual');
    if (!targetSection) {
      return;
    }

    targetSection.setFrom(sourceSection);

    sourceSection = e.task.taskModel.card.sections.tryGet('SequenceProvider');
    if (!sourceSection) {
      return;
    }
    targetSection = e.card.sections.tryGet('KrAdditionalApprovalsRequestedInfoVirtual');
    if (!targetSection) {
      return;
    }
    targetSection.setFrom(sourceSection);
  }

  public initialized(context: ICardUIExtensionContext): void {
    const model = context.model;
    if (
      !(model.mainForm instanceof DefaultFormTabWithTasksViewModel) ||
      Flags.hasNotFlag(model.cardType.flags, CardTypeFlags.AllowTasks)
    ) {
      return;
    }

    const functionRoles = context.model.generalMetadata.enumerations.functionRoles;
    const formWithTasks = model.mainForm as DefaultFormTabWithTasksViewModel;

    for (const taskViewModel of formWithTasks.tasks) {
      this.modifyTaskAndAttachHandlers(taskViewModel);
      this.modifyUniversalTask(taskViewModel, functionRoles);
      this.modifySigningTask(context.model, taskViewModel);
      this.modifyTaskGrid(taskViewModel);
    }
  }

  public finalized(_context: ICardUIExtensionContext): void {
    for (const dispose of this._disposes) {
      if (dispose) {
        dispose();
      }
    }
  }

  private commentIsHiddenForApproval = async (): Promise<boolean> => {
    if (this._commentIsHiddenForApproval != null) {
      return this._commentIsHiddenForApproval;
    }

    const krSettings = await this._cardSingletonCache.getCard('KrSettings');
    if (!krSettings) {
      this._commentIsHiddenForApproval = false;
      return false;
    }

    const value = !!krSettings.sections.get('KrSettings').fields.get('HideCommentForApprove');
    this._commentIsHiddenForApproval = value;
    return value;
  };

  private modifyTaskAndAttachHandlers(taskViewModel: TaskViewModel) {
    const taskModel = taskViewModel.taskModel;
    if (
      (taskModel.cardType.id !== 'e4d7f6bf-fea9-4a3b-8a5a-e1a0a40de74c' && // KrApproveTypeID
        taskModel.cardType.id !== 'b3d8eae3-c6bf-4b59-bcc7-461d526c326c' && // KrAdditionalApprovalTypeID
        taskModel.cardType.id !== '968d68b3-a7c5-4b5d-bfa4-bb0f346880b6') || // KrSigningTypeID
      taskModel.cardTask!.isLockedEffective
    ) {
      return;
    }

    let commentBlock: IBlockViewModel;
    // скрываем блок с комментариями в текущем представлении
    if (
      taskModel.card.sections.get('KrCommentsInfoVirtual').rows.length === 0 &&
      !!(commentBlock = taskModel.blocks.get('CommentsBlockShort')!)
    ) {
      // Если секция есть, но ее поля незаполнены - значит запроса комментария не было
      commentBlock.blockVisibility = Visibility.Collapsed;
    }

    if (
      taskModel.cardType.id === 'e4d7f6bf-fea9-4a3b-8a5a-e1a0a40de74c' || // KrApproveTypeID
      taskModel.cardType.id === 'b3d8eae3-c6bf-4b59-bcc7-461d526c326c' || // KrAdditionalApprovalTypeID
      taskModel.cardType.id === '968d68b3-a7c5-4b5d-bfa4-bb0f346880b6' // KrSigningTypeID
    ) {
      let additionalApprovalBlock: IBlockViewModel;
      // скрываем блок с заданиями доп согласования в текущем представлении
      if (
        taskModel.card.sections.get('KrAdditionalApprovalInfoVirtual').rows.length === 0 &&
        !!(additionalApprovalBlock = taskModel.blocks.get('AdditionalApprovalBlockShort')!)
      ) {
        // Если секция есть, но ее поля незаполнены - значит запроса комментария не было
        additionalApprovalBlock.blockVisibility = Visibility.Collapsed;
      }

      // Скрываем блок с запрошенными заданиями доп согласования в текущем задании
      let additionalApprovalsRequestedInfoTable: IControlViewModel;
      if (
        taskModel.card.sections.tryGet('KrAdditionalApprovalsRequestedInfoVirtual') &&
        taskModel.card.sections.tryGet('KrAdditionalApprovalsRequestedInfoVirtual')!.rows.length ===
          0 &&
        !!(additionalApprovalsRequestedInfoTable = taskModel.controls.get(
          'AdditionalApprovalsRequestedInfoTable'
        )!)
      ) {
        // Если секция есть, но ее поля незаполнены - значит запроса комментария не было
        additionalApprovalsRequestedInfoTable.controlVisibility = Visibility.Collapsed;
      }
    }

    // в начальной форме задания гарантированно нет поля "Комментарий",
    // которое может понадобиться скрыть для варианта "Согласовать"

    // скрываем блок с комментариями в других представлениях

    this._disposes.push(
      taskViewModel.workspaceChanged.addWithDispose(async e => {
        // получить блок по taskModel.Blocks.TryGet нельзя, т.к. для формы откладывания заданий будет свой экземпляр блока,
        // при этом TryGet вернёт блок для предыдущей формы карточки

        const form = e.task.taskWorkspace.form;
        if (!form) {
          return;
        }

        const blocks = form.blocks;
        let innerCommentBlock: IBlockViewModel;
        if (
          taskModel.card.sections.get('KrCommentsInfoVirtual').rows.length === 0 &&
          !!(innerCommentBlock = blocks.find(x => x.name === 'CommentsBlockShort')!)
        ) {
          // Если секция есть, но ее поля незаполнены - значит запроса комментария не было
          innerCommentBlock.blockVisibility = Visibility.Collapsed;
        }

        if (
          taskModel.cardType.id === 'e4d7f6bf-fea9-4a3b-8a5a-e1a0a40de74c' || // KrApproveTypeID
          taskModel.cardType.id === 'b3d8eae3-c6bf-4b59-bcc7-461d526c326c' || // KrAdditionalApprovalTypeID
          taskModel.cardType.id === '968d68b3-a7c5-4b5d-bfa4-bb0f346880b6' // KrSigningTypeID
        ) {
          let innerAdditionalApprovalBlock: IBlockViewModel;
          if (
            taskModel.card.sections.get('KrAdditionalApprovalInfoVirtual').rows.length === 0 &&
            !!(innerAdditionalApprovalBlock = blocks.find(
              x => x.name === 'AdditionalApprovalBlockShort'
            )!)
          ) {
            // Если секция есть, но ее поля незаполнены - значит запроса комментария не было
            innerAdditionalApprovalBlock.blockVisibility = Visibility.Collapsed;
          }

          // Скрываем блок с запрошенными заданиями доп согласования в текущем задании.
          let additionalApprovalsRequestedInfoTable: IControlViewModel;
          if (
            taskModel.card.sections.tryGet('KrAdditionalApprovalsRequestedInfoVirtual') &&
            taskModel.card.sections.tryGet('KrAdditionalApprovalsRequestedInfoVirtual')!.rows
              .length === 0 &&
            !!(additionalApprovalsRequestedInfoTable = taskModel.controls.get(
              'AdditionalApprovalsRequestedInfoTable'
            )!)
          ) {
            // Если секция есть, но ее поля незаполнены - значит запроса комментария не было
            additionalApprovalsRequestedInfoTable.controlVisibility = Visibility.Collapsed;
          }

          // скрываем поле "Комментарий" для варианта завершения "Согласовать"
          let approvalCommentBlock: IBlockViewModel;
          if (
            form.name === 'Approve' &&
            (await this.commentIsHiddenForApproval()) &&
            !!(approvalCommentBlock = blocks.find(x => x.name === 'CommentBlock')!)
          ) {
            approvalCommentBlock.blockVisibility = Visibility.Collapsed;
          }
        }
      })
    );

    // подписываемся на построение метаинформации и виртуальной карточки для формы откладывания задания

    this._disposes.push(
      taskViewModel.postponeMetadataInitializing.addWithDispose(
        KrUIExtension.postponeMetadataInitializing
      )
    );

    this._disposes.push(
      taskViewModel.postponeContentInitializing.addWithDispose(
        KrUIExtension.postponeContentInitializing
      )
    );
  }

  private modifySigningTask(cardModel: ICardModel, taskViewModel: TaskViewModel) {
    const taskModel = taskViewModel.taskModel;
    if (
      taskModel.cardType.id === '968d68b3-a7c5-4b5d-bfa4-bb0f346880b6' // KrSigningTypeID
    ) {
      taskViewModel.modifyWorkspace(e => {
        const signingOptions = e.task.taskModel.card.sections.tryGet('KrSigningTaskOptions');
        if (signingOptions) {
          if (!signingOptions.fields.tryGet('AllowAdditionalApproval')) {
            const actionIndex = e.task.taskWorkspace.additionalActions.findIndex(
              x => x.completionOption?.id === 'c726d8ba-73b9-4867-87fe-387d4c61a75a'
            ); // AdditionalApproval
            if (actionIndex > -1) {
              e.task.taskWorkspace.additionalActions.splice(actionIndex, 1);
            }
          }

          const shouldSignFiles = signingOptions.fields.tryGetBoolean('SignCardFiles');
          if (!shouldSignFiles) {
            return;
          }

          const signActionIndex = e.task.taskWorkspace.actions.findIndex(
            x => x.completionOption?.id === '45d6f756-d30b-4c98-9d72-6adf1a15d075' // Sign
          );

          if (signActionIndex === -1) {
            return;
          }

          const signAction = e.task.taskWorkspace.actions[signActionIndex];
          if (signAction.type === TaskActionType.NavigateToForm) {
            return;
          }

          const btnAction = signAction.button.buttonAction;

          signAction.button.buttonAction = async (btn, e) => {
            const signed = await this._signFilesProvider.signFilesAction(cardModel, taskViewModel);
            if (signed && btnAction) {
              btnAction(btn, e);
            }
          };
        }
      });
    }
  }

  private modifyUniversalTask(
    taskViewModel: TaskViewModel,
    functionRoles: readonly ICardMetadataFunctionRole[]
  ) {
    const taskModel = taskViewModel.taskModel;
    if (
      taskModel.cardType.id === '9c6d9824-41d7-41e6-99f1-e19ea9e576c5' && // KrUniversalTaskTypeID
      taskModel.cardTask &&
      taskModel.cardTask.isCanPerform &&
      (taskModel.cardTask.storedState !== CardTaskState.InProgress ||
        Flags.hasFlag(taskModel.cardTask.flags, CardTaskFlags.CurrentPerformer))
    ) {
      taskViewModel.modifyWorkspace(e => this.modifyUniversalTaskAction(e.task, functionRoles));
    }
  }

  private modifyUniversalTaskAction = (
    taskViewModelInternal: TaskViewModel,
    functionRoles: readonly ICardMetadataFunctionRole[]
  ) => {
    if (taskViewModelInternal.taskWorkspace.form) {
      return;
    }

    const actionsInitialCount = taskViewModelInternal.taskWorkspace.actions.length;
    const additionalActionsInitialCount =
      taskViewModelInternal.taskWorkspace.additionalActions.length;

    const taskModel = taskViewModelInternal.taskModel;
    const taskSessionRoles = taskModel.cardTask!.taskSessionRoles;
    const taskSessionFunctionRoles = functionRoles.filter(x =>
      taskSessionRoles.some(y => y.functionRoleId === x.id)
    );
    const task = taskViewModelInternal.taskModel.cardTask;
    const taskFlags = task!.flags;
    const autoStartTask =
      task?.storedState === CardTaskState.Created &&
      (Flags.hasFlag(
        taskViewModelInternal.taskModel.cardType.flags,
        CardTypeFlags.AutoStartTasks
      ) ||
        Flags.hasFlag(taskFlags, CardTaskFlags.AutoStart));

    if (StorageHelper.tryGet(task!.info, '.hiddenByDefault')) {
      taskViewModelInternal.taskWorkspace.actions.splice(
        taskViewModelInternal.taskWorkspace.actions.length - actionsInitialCount,
        0,
        createUnlockForPerformerAction(taskViewModelInternal.taskNavigator)
      );
      return;
    }

    const section = taskModel.card.sections.getOrAdd('KrUniversalTaskOptions');
    section.type = CardSectionType.Table;
    section.tableType = CardTableType.Collection;
    const rowsStorage = section.rows;
    const rows = rowsStorage.map(x => x);
    rows.sort((a, b) => a.get<number>('Order')! - b.get<number>('Order')!);

    for (const row of rows) {
      const optionId = row.get<string>('OptionID')!;
      const caption = row.get<string>('Caption')!;
      const showComment = !!row.get('ShowComment');
      const message = row.get<string>('Message')!;
      const additional = row.get('Additional');

      const settingsJson: string | null = row.get('Settings');
      const settings = settingsJson
        ? (deserializeFromTypedToPlain(settingsJson) as IStorage)
        : null;

      let groupingType: TaskGroupingType | undefined;
      if (settings) {
        const optionFunctionRolesIDs = new StorageAccessor(settings).tryGetGuidArray(
          '.OptionFunctionRoles'
        );
        if (optionFunctionRolesIDs) {
          const optionSessionFunctionRoles = taskSessionFunctionRoles.filter(
            x => optionFunctionRolesIDs?.some(y => y === x.id) ?? false
          );
          if (
            (optionSessionFunctionRoles.length > 0 &&
              optionSessionFunctionRoles.every(x => !x.hideTaskByDefault) &&
              (Flags.hasFlag(taskFlags, CardTaskFlags.CurrentPerformer) ||
                (autoStartTask && Flags.hasFlag(taskFlags, CardTaskFlags.CanPerform)))) ||
            (optionSessionFunctionRoles.length > 0 &&
              optionSessionFunctionRoles.some(x => !x.canTakeInProgress))
          ) {
            groupingType = functionRoles.some(x => x.hideTaskByDefault)
              ? TaskGroupingType.HideByDefault
              : TaskGroupingType.Default;
          } else {
            continue;
          }
        }
      } else if (
        (taskModel.cardTask!.storedState != CardTaskState.InProgress &&
          Flags.hasNotFlag(taskModel.cardTask!.flags, CardTaskFlags.AutoStart) &&
          Flags.hasNotFlag(taskModel.cardType.flags, CardTypeFlags.AutoStartTasks)) ||
        (taskModel.cardTask!.storedState === CardTaskState.InProgress &&
          Flags.hasNotFlag(taskModel.cardTask!.flags, CardTaskFlags.CurrentPerformer))
      ) {
        continue;
      }

      if (additional) {
        const index =
          taskViewModelInternal.taskWorkspace.additionalActions.length -
          additionalActionsInitialCount;

        taskViewModelInternal.taskWorkspace.additionalActions.splice(
          index,
          0,
          this.generateTaskAction(
            taskViewModelInternal.taskNavigator,
            optionId,
            caption,
            message,
            showComment ? 'WithComment' : '',
            groupingType ?? TaskGroupingType.Default
          )
        );
      } else {
        taskViewModelInternal.taskWorkspace.actions.splice(
          taskViewModelInternal.taskWorkspace.actions.length - actionsInitialCount,
          0,
          this.generateTaskAction(
            taskViewModelInternal.taskNavigator,
            optionId,
            caption,
            message,
            showComment ? 'WithComment' : '',
            groupingType ?? TaskGroupingType.Default
          )
        );
      }

      runInAction(() => {
        const orderedActions = taskViewModelInternal.taskWorkspace.actions
          .slice()
          .sort((x, y) => x.groupingType - y.groupingType);
        taskViewModelInternal.taskWorkspace.actions.replaceWith(orderedActions);
      });
    }
  };

  private modifyTaskGrid(taskViewModel: TaskViewModel) {
    const taskModel = taskViewModel.taskModel;
    if (
      taskModel.cardType.id === 'c6f3828f-b001-46f6-b121-3f3ed9e65cde' // KrInfoForInitiatorTaskTypeID
    ) {
      this.disableGridCompactMode(taskModel, 'KrInfoForInitiator');
    } else if (
      taskModel.cardType.id === 'e4d7f6bf-fea9-4a3b-8a5a-e1a0a40de74c' || // KrApproveTypeID
      taskModel.cardType.id === 'b3d8eae3-c6bf-4b59-bcc7-461d526c326c' || // KrAdditionalApprovalTypeID
      taskModel.cardType.id === '968d68b3-a7c5-4b5d-bfa4-bb0f346880b6' // KrSigningTypeID
    ) {
      this._disposes.push(
        taskViewModel.modifyWorkspace(e => {
          this.disableGridCompactMode(e.task.taskModel, 'KrAdditionalApprovalTable');
          this.disableGridCompactMode(e.task.taskModel, 'KrCommentsTable');
        })
      );
    }
  }

  private disableGridCompactMode(taskModel: ICardModel, tableName: string) {
    const table = taskModel.controls.get(tableName) as GridViewModel;
    const layout = table?.layouts?.find(x => x.name === defaultLayout.name);
    if (!layout) {
      return;
    }

    layout.start = 0;
  }

  private generateTaskAction(
    navigator: TaskNavigator,
    optionId: string,
    caption: string,
    message: string,
    showComment: string,
    groupingType: TaskGroupingType = TaskGroupingType.Default
  ): TaskAction {
    return !message && !showComment
      ? new TaskActionViewModel(
          caption,
          () =>
            saveCardWithTaskModifier(navigator.taskModel, task => {
              task.action = CardTaskAction.Complete;
              task.state = CardRowState.Deleted;
              task.optionId = optionId;
            }),
          TaskActionType.Complete,
          groupingType,
          null,
          navigator.taskModel
        )
      : new TaskActionViewModel(
          caption,
          () => {
            navigator.navigateToForm(TaskWorkspaceState.OptionForm, ExtendedTaskForm, [
              this.generateTaskAction(navigator, optionId, caption, '', ''),
              createNavigateBackAction(navigator)
            ]);

            const newTaskModel = navigator.taskModel;
            let blockViewModel: IBlockViewModel | undefined;
            if ((blockViewModel = newTaskModel.blocks.get(ExtendedTaskForm))) {
              let controlViewModel: IControlViewModel | undefined;
              if (
                (controlViewModel = blockViewModel.controls.find(p => p.name === 'MessageLabel'))
              ) {
                const label = controlViewModel as LabelViewModel;
                if (message) {
                  label.controlVisibility = Visibility.Visible;
                  label.text = message;
                } else {
                  label.controlVisibility = Visibility.Collapsed;
                }
              }

              if ((controlViewModel = blockViewModel.controls.find(p => p.name === 'Comment'))) {
                controlViewModel.controlVisibility = showComment
                  ? Visibility.Visible
                  : Visibility.Collapsed;
              }
            }
          },
          TaskActionType.NavigateToForm,
          groupingType,
          null,
          navigator.taskModel
        );
  }
}

const ExtendedTaskForm = 'Extended';
