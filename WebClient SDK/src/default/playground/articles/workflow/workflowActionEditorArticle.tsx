import { inject, injectable } from '@tessa/application';
import { showMessage, UIButton } from 'tessa/ui';
import { AsyncLazy, TypedJsonConverter } from '@tessa/core';
import { WorkflowActionDescriptors } from 'tessa/ui/workflow/workflowActionDescriptors';
import {
  IWorkflowActionEditorRegistry$,
  IWorkflowActionHandlerResolver$,
  IWorkflowActionSettingsRegistry$,
  WorkflowHashParameterComplexTypesCache$
} from 'tessa/ui/workflow/workflowInjects';
import { IWorkflowActionSettingsEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowEditorTypes';
import { WorkflowActionStorage } from 'tessa/ui/workflow/chunk/models/workflowActionStorage';
import { WorkflowSettingsEditor } from 'tessa/ui/workflow/chunk/editors/workflowSettingsEditorView';
import { WorkflowBindingContext } from 'tessa/ui/workflow/bindings/workflowBindingContext';
import { WorkflowActionEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowActionEditorViewModel';
import {
  IWorkflowActionEditorContainer,
  IWorkflowActionEditorRegistry,
  IWorkflowActionHandlerResolver,
  IWorkflowActionSettingsFactory,
  IWorkflowActionSettingsRegistry
} from 'tessa/ui/workflow';
import { KrDescriptors } from '../../../workflow/workflowEngine/krDescriptors';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { WorkflowHashParameterComplexTypesCache } from 'tessa/ui/workflow/workflowHashParameterComplexTypesCache';
import { IWorkflowComplexTypesProvider } from 'tessa/ui/workflow/workflowHashEditor/types';
import { WorkflowSettingsComplexTypesProvider } from 'tessa/ui/workflow/workflowHashEditor/workflowSettingsComplexTypesProvider';

@injectable()
export class WorkflowActionEditorArticle extends PlaygroundArticle {
  //#endregion fields

  private _actionSettingsFactory: IWorkflowActionSettingsFactory;
  private _actionEditorContainer: IWorkflowActionEditorContainer;
  private _complexTypesProvider: IWorkflowComplexTypesProvider;

  //#endregion

  //#region ctor

  constructor(
    @inject(IWorkflowActionSettingsRegistry$)
    protected readonly _actionSettingsRegistry: IWorkflowActionSettingsRegistry,
    @inject(IWorkflowActionEditorRegistry$)
    protected readonly _actionEditorRegistry: IWorkflowActionEditorRegistry,
    @inject(IWorkflowActionHandlerResolver$)
    protected readonly _actionHandlerResolver: IWorkflowActionHandlerResolver,
    @inject(WorkflowHashParameterComplexTypesCache$, { lazy: true })
    protected readonly _workflowHashParameterComplexTypesCache: AsyncLazy<WorkflowHashParameterComplexTypesCache>
  ) {
    super();
  }

  //#endregion

  //#region overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Workflow/Editors/Actions',
      description: 'Workflow editor actions editors'
    };
  }

  override async initialize(): Promise<void> {
    this._actionSettingsFactory = await this._actionSettingsRegistry.resolve();
    this._actionEditorContainer = await this._actionEditorRegistry.resolve();
    this._complexTypesProvider = new WorkflowSettingsComplexTypesProvider(
      await this._workflowHashParameterComplexTypesCache.getValue()
    );

    this.addOutdatedActionEditorNode();
    this.addTaskActionEditorNode();
    this.addTaskGroupActionEditorNode();
    this.addDialogActionEditorNode();
    this.addTaskControlActionEditorNode();
    this.addTaskGroupControlActionEditorNode();
    this.addStartActionEditorNode(false);
    this.addStartActionEditorNode(true);
    this.addEndActionEditorNode();
    this.addScenarioActionEditorNode();
    this.addAddFileFromTemplateActionEditorNode();
    this.addCommandActionEditorNode();
    this.addSendSignalActionEditorNode();
    this.addNotitifcationActionEditorNode();
    this.addAcquaintanceActionEditorNode();
    this.addTimerActionEditorNode();
    this.addTimerControlActionEditorNode();
    this.addSendHistoryManagementEditorNode();
    this.addConditionActionEditorNode();
    this.addChangeStateActionEditorNode();
    this.addCreateCardActionEditorNode();
    this.addpprovalProcessActionEditorNode();
    this.addSubprocessControlActionEditorNode();
    this.addApprovalProcessControlActionEditorNode();
  }

  //#endregion

  //#region add blocks methods

  addStartActionEditorNode(designMode: boolean): void {
    this.addTestViewModelBlock(`Base. DesignMode=${designMode}`, async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.startProcessDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addEndActionEditorNode(): void {
    this.addTestViewModelBlock('End', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.endProcessDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addScenarioActionEditorNode(): void {
    this.addTestViewModelBlock('Scenario', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.scenarioDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addAddFileFromTemplateActionEditorNode(): void {
    this.addTestViewModelBlock('AddFileFromTemplate', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.addFileFromTemplateDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addSendSignalActionEditorNode(): void {
    this.addTestViewModelBlock('SendSignal', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.sendSignalDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addNotitifcationActionEditorNode(): void {
    this.addTestViewModelBlock('Notification', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.notificationDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addAcquaintanceActionEditorNode(): void {
    this.addTestViewModelBlock('Acquaintance', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = KrDescriptors.acquaintanceDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addTimerActionEditorNode(): void {
    this.addTestViewModelBlock('Timer', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.timerDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addTimerControlActionEditorNode(): void {
    this.addTestViewModelBlock('Timer Control', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.timerControlDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addCommandActionEditorNode(): void {
    this.addTestViewModelBlock('Command', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.commandDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addSendHistoryManagementEditorNode(): void {
    this.addTestViewModelBlock('HistoryManagement', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.historyManagementDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addTaskActionEditorNode(): void {
    this.addTestViewModelBlock('Task', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.taskDescriptor.id;
      action.version = WorkflowActionDescriptors.taskDescriptor.version;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addTaskGroupActionEditorNode(): void {
    this.addTestViewModelBlock('Task Group', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.taskGroupDescriptor.id;
      action.version = WorkflowActionDescriptors.taskGroupDescriptor.version;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addDialogActionEditorNode(): void {
    this.addTestViewModelBlock('Dialog', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.dialogDescriptor.id;
      action.version = WorkflowActionDescriptors.dialogDescriptor.version;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addTaskControlActionEditorNode(): void {
    this.addTestViewModelBlock('Task Control', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.taskControlDescriptor.id;
      action.version = WorkflowActionDescriptors.taskControlDescriptor.version;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addTaskGroupControlActionEditorNode(): void {
    this.addTestViewModelBlock('Task Group Control', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.taskGroupControlDescriptor.id;
      action.version = WorkflowActionDescriptors.taskGroupControlDescriptor.version;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addOutdatedActionEditorNode(): void {
    this.addTestViewModelBlock('Outdated', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.taskDescriptor.id;

      action.version = 1;
      const storage = this._actionSettingsFactory.create(action);
      // ставим устаревшую версию и подменяем на ID outdatedActionDescriptor, чтобы получить нужный редактор.
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addConditionActionEditorNode(): void {
    this.addTestViewModelBlock('Condition', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.conditionDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const handler = await this._actionHandlerResolver.resolve(action.actionTypeId);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext,
        handler: handler ?? undefined
      });

      return { storage: action, viewModel };
    });
  }

  addChangeStateActionEditorNode(): void {
    this.addTestViewModelBlock('ChangeState', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = KrDescriptors.changeStateDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const handler = await this._actionHandlerResolver.resolve(action.actionTypeId);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext,
        handler: handler ?? undefined
      });

      return { storage: action, viewModel };
    });
  }

  addCreateCardActionEditorNode(): void {
    this.addTestViewModelBlock('CreateCard', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.createCardDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const handler = await this._actionHandlerResolver.resolve(action.actionTypeId);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext,
        handler: handler ?? undefined
      });

      return { storage: action, viewModel };
    });
  }

  addpprovalProcessActionEditorNode(): void {
    this.addTestViewModelBlock('ApprovalProcess', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.approvalProcessDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const handler = await this._actionHandlerResolver.resolve(action.actionTypeId);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext,
        handler: handler ?? undefined
      });

      return { storage: action, viewModel };
    });
  }

  addSubprocessControlActionEditorNode(): void {
    this.addTestViewModelBlock('Subprocess Control', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.subprocessControlDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  addApprovalProcessControlActionEditorNode(): void {
    this.addTestViewModelBlock('Approval Process Control', async () => {
      const bindingContext = new WorkflowBindingContext({}, {}, {}, this._complexTypesProvider);
      const action = new WorkflowActionStorage();
      action.actionTypeId = WorkflowActionDescriptors.approvalProcessControlDescriptor.id;

      const storage = this._actionSettingsFactory.create(action);
      const viewModel = this._actionEditorContainer.create(storage, {
        designMode: true,
        bindingContext
      });

      return { storage: action, viewModel };
    });
  }

  //#endregion

  //#region private methods

  private async addTestViewModelBlock(
    caption: string,
    viewModelFactory: () => Promise<{
      storage: WorkflowActionStorage;
      viewModel: IWorkflowActionSettingsEditorViewModel;
    }>
  ): Promise<void> {
    this.addBlock({
      caption: caption,
      props: async () => {
        const { storage, viewModel } = await viewModelFactory();
        const actionEditorViewModel = new WorkflowActionEditorViewModel(storage, viewModel);
        await actionEditorViewModel.initialize();

        return { actionEditorViewModel, storage };
      },
      view: ({ actionEditorViewModel }) => (
        <DemoForm
          customStyles={css => css({ width: '100%', display: 'flex', flexDirection: 'column' })}
        >
          <WorkflowSettingsEditor
            viewModel={actionEditorViewModel}
            editorClassName="workflow-process-action"
          />
        </DemoForm>
      ),
      buttons: ({ storage }) => [
        UIButton.create({
          name: 'ShowStorage',
          type: 'normal',
          theme: 'control',
          caption: 'Show Storage',
          buttonAction: async () => {
            await showMessage(
              TypedJsonConverter.serialize(storage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  //#endregion
}
