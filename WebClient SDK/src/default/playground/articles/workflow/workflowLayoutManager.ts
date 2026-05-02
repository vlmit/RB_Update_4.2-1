import { WorkflowProcessStorage } from 'tessa/ui/workflow/chunk/models/workflowProcessStorage';
import {
  IWorkflowClipboardManager,
  IWorkflowLayoutViewModel,
  IWorkflowNodeViewModelFactory
} from 'tessa/ui/workflow/chunk/layout/workflowTypes';
import { WorkflowLayoutViewModel } from 'tessa/ui/workflow/chunk/layout/workflowLayoutViewModel';
import { Guid, IStorage } from '@tessa/core';
import { IHistoryManagerFactory } from '@tessa/platform';
import { WorkflowTemplateContextMenuGenerator } from 'tessa/ui/workflow/chunk/layout/workflowTemplateContextMenuGenerator';
import { IWorkflowActionSettingsFactory } from 'tessa/ui/workflow';

export class WorkflowLayoutManager {
  //#region fields

  private readonly _getNodeFactory: () => IWorkflowNodeViewModelFactory;
  private readonly _initProcessFunc: (
    process: WorkflowProcessStorage,
    nodeFactory: IWorkflowNodeViewModelFactory
  ) => void;

  private _process: WorkflowProcessStorage;
  private _nodeFactory: IWorkflowNodeViewModelFactory;
  private readonly _clipboardManager: IWorkflowClipboardManager;
  private readonly _historyManagerFactory: IHistoryManagerFactory;
  private readonly _actionSettingsFactory: IWorkflowActionSettingsFactory;
  private _layoutViewModel: IWorkflowLayoutViewModel;
  private _resetMode: undefined | 'view' | 'viewmodel' | IStorage;

  //#endregion

  //#region ctors

  constructor(
    clipboardManager: IWorkflowClipboardManager,
    historyManagerFactory: IHistoryManagerFactory,
    actionSettingsFactory: IWorkflowActionSettingsFactory,
    getNodeFactory: () => IWorkflowNodeViewModelFactory,
    initProcessFunc: (
      process: WorkflowProcessStorage,
      nodeFactory: IWorkflowNodeViewModelFactory
    ) => void
  ) {
    this._clipboardManager = clipboardManager;
    this._historyManagerFactory = historyManagerFactory;
    this._actionSettingsFactory = actionSettingsFactory;
    this._getNodeFactory = getNodeFactory;
    this._initProcessFunc = initProcessFunc;
  }

  //#endregion

  //#region properties

  public get process(): WorkflowProcessStorage {
    return this._process;
  }

  public get layoutViewModel(): IWorkflowLayoutViewModel {
    return this._layoutViewModel;
  }

  //#endregion

  //#region methods

  resetIfRequired(): void {
    if (this._resetMode) {
      this._resetMode = undefined;
      return;
    }

    this.resetModel();
    this.resetViewModel();
  }

  reset(resetMode?: 'view' | 'viewmodel' | IStorage): void {
    this._resetMode = resetMode;
    if (!this._resetMode) {
      this.resetModel();
      this.resetViewModel();
    } else if (this._resetMode === 'viewmodel') {
      this.resetViewModel();
    } else if (this._resetMode === 'view') {
      // do nothing special
    } else {
      this.resetModel(this._resetMode);
      this.resetViewModel();
    }
  }

  private resetModel(storage?: IStorage) {
    this._process = new WorkflowProcessStorage(storage);
    if (!storage) {
      this._process.id = Guid.newGuid();
      this._nodeFactory = this._getNodeFactory();
      this._initProcessFunc(this._process, this._nodeFactory);
    }
  }

  private resetViewModel() {
    if (this._layoutViewModel) {
      this._layoutViewModel.dispose();
    }

    this._layoutViewModel = new WorkflowLayoutViewModel(
      this._process,
      this._nodeFactory,
      this._clipboardManager,
      new WorkflowTemplateContextMenuGenerator(),
      this._historyManagerFactory(),
      this._actionSettingsFactory
    );
  }

  //#endregion
}
