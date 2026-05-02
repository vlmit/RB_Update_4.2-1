import { inject, injectable } from '@tessa/application';
import { WorkflowProcessEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowProcessEditorViewModel';
import { WorkflowProcessStorage } from 'tessa/ui/workflow/chunk/models/workflowProcessStorage';
import {
  IDbEnumerationProvider,
  IDbEnumerationProvider$,
  WorkflowSignalProcessingMode
} from '@tessa/platform';
import { showMessage, UIButton } from 'tessa/ui';
import { AsyncLazy, Guid, TypedJsonConverter } from '@tessa/core';
import { Visibility } from 'tessa/platform';
import { WorkflowLinkStorage } from 'tessa/ui/workflow/chunk/models/workflowLinkStorage';
import { WorkflowSettingsEditor } from 'tessa/ui/workflow/chunk/editors/workflowSettingsEditorView';
import { WorkflowLinkEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowLinkEditorViewModel';
import { WorkflowNodeEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowNodeEditorViewModel';
import { WorkflowNodeStorage } from 'tessa/ui/workflow/chunk/models/workflowNodeStorage';
import { IWorkflowActionDescriptorRegistry, WorkflowLogLevel } from 'tessa/ui/workflow/types';
import { WorkflowLinkMode } from 'tessa/ui/workflow/enums';
import { WorkflowActionStorage } from 'tessa/ui/workflow/chunk/models/workflowActionStorage';
import { IWorkflowActionDescriptorRegistry$ } from 'tessa/ui/workflow/workflowInjects';
import { WorkflowProcessStateEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowProcessStateEditorViewModel';
import { WorkflowProcessStateStorage } from 'tessa/ui/workflow/chunk/models/workflowProcessStateStorage';
import { WorkflowErrorStorage } from 'tessa/ui/workflow/chunk/models/workflowErrorStorage';
import { WorkflowNodeStateEditorViewModel } from 'tessa/ui/workflow/chunk/editors/workflowNodeStateEditorViewModel';
import { WorkflowNodeStateStorage } from 'tessa/ui/workflow/chunk/models/workflowNodeStateStorage';
import { WorkflowActionStateStorage } from 'tessa/ui/workflow/chunk/models/workflowActionStateStorage';
import { WorkflowInstanceTaskStorage } from 'tessa/ui/workflow/chunk/models/workflowInstanceTaskStorage';
import { WorkflowInstanceSubprocessStorage } from 'tessa/ui/workflow/chunk/models/workflowInstanceSubprocessStorage';
import { IconSelector, IconSelector$ } from 'tessa/ui/iconSelector';
import { DemoForm, PlaygroundArticle } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class WorkflowEditorsArticle extends PlaygroundArticle {
  //#region fields

  protected iconSelector: IconSelector;

  //#endregion

  //#region ctor

  constructor(
    @inject(IDbEnumerationProvider$)
    protected readonly _enumerationProvider: IDbEnumerationProvider,
    @inject(IWorkflowActionDescriptorRegistry$)
    protected readonly _actionDescriptorRegistry: IWorkflowActionDescriptorRegistry,
    @IconSelector$({ lazy: true }) protected readonly _iconSelectorFactory: AsyncLazy<IconSelector>
  ) {
    super();
  }

  //#endregion

  //#region overrides

  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Workflow/Editors/Core',
      description: 'Workflow editor editors'
    };
  }

  override async initialize(): Promise<void> {
    this.iconSelector = await this._iconSelectorFactory.getValue();

    this.addWorkflowProcessTemplateEditorBlockNode();
    this.addWorkflowProcessInstanceEditorBlockNode();
    this.addLinkEditorBlockNode();
    this.addWorkflowNodeEditorBlockNodes();
    this.addWorkflowNodeInstanceEditorBlockNode();
  }

  //#endregion

  //#region add blocks methods

  addWorkflowProcessEditorNode(
    caption: string,
    init: (viewModel: WorkflowProcessEditorViewModel) => Promise<void>,
    description?: string
  ): void {
    let storage: WorkflowProcessStorage;

    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        storage = new WorkflowProcessStorage();
        storage.name = 'test-ProcessName';
        storage.description =
          'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. \n' +
          'Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. \n' +
          'Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. \n' +
          'Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.';
        storage.logLevel = WorkflowLogLevel.None;
        storage.parentTypeID = 'BB870E87-BCC4-4E89-9C79-EDAF3CC8AE4B';
        storage.parentTypeName = 'test-base-class';
        storage.globalScript =
          'var str = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod...";\n' +
          'var result = str.Substring(10);\n';

        const viewModel = new WorkflowProcessEditorViewModel(
          storage,
          () => false,
          () => {},
          () => {},
          () => {}
        );
        await init(viewModel);
        return { viewModel };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ width: '100%' })}>
          <WorkflowSettingsEditor
            viewModel={viewModel}
            editorClassName={'workflow-process-editor'}
          />
        </DemoForm>
      ),
      buttons: [
        UIButton.create({
          name: 'showStorage',
          caption: 'Show Storage',
          type: 'small',
          theme: 'control',
          icon: 'm-etc',
          visibility: Visibility.Visible,
          onMouseDown: async () => {
            await showMessage(
              TypedJsonConverter.serialize(storage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  addWorkflowInstanceditorNode(
    caption: string,
    init: (viewModel: WorkflowProcessStateEditorViewModel) => Promise<void>,
    description?: string
  ): void {
    let storage: WorkflowProcessStateStorage;
    let errors: WorkflowErrorStorage[];

    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        storage = new WorkflowProcessStateStorage();
        storage.name = 'test-ProcessName-instance';
        storage.logLevel = WorkflowLogLevel.None;

        errors = [];

        const error1 = new WorkflowErrorStorage();
        error1.rowId = Guid.newGuid();
        error1.added = '6/5/2025 6:19:10 PM';
        error1.text = 'Error_1';
        error1.nodeInstanceID = 'dc541752-496d-45eb-ad71-92ab186d7601';
        error1.nodeID = 'dc541742-496d-45eb-ad71-92ab186d7601';
        error1.isAsync = true;
        error1.resumable = true;
        errors.push(error1);

        const error2 = new WorkflowErrorStorage();
        error2.rowId = Guid.newGuid();
        error2.added = '6/5/2025 6:21:15 PM';
        error2.text = 'Error_2';
        error2.nodeInstanceID = 'dc541752-496d-45eb-ad71-92ab186d7601';
        error2.nodeID = 'dc541742-496d-45eb-ad71-92ab186d7601';
        error2.isAsync = true;
        error2.resumable = true;
        errors.push(error2);

        const viewModel = new WorkflowProcessStateEditorViewModel(
          storage,
          errors,
          () => false,
          () => {}
        );

        viewModel.openErrorCardAction = async error => {
          await showMessage(`Clicked action: ${error.text}`);
        };
        await init(viewModel);
        return { viewModel };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ width: '100%' })}>
          <WorkflowSettingsEditor
            viewModel={viewModel}
            editorClassName={'workflow-process-editor'}
          />
        </DemoForm>
      ),
      buttons: [
        UIButton.create({
          name: 'showStorage',
          caption: 'Show Storage',
          type: 'small',
          theme: 'control',
          icon: 'm-etc',
          visibility: Visibility.Visible,
          onMouseDown: async () => {
            await showMessage(
              TypedJsonConverter.serialize(storage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  addWorkflowProcessTemplateEditorBlockNode(): void {
    this.addWorkflowProcessEditorNode(
      'Process Editor',
      async viewModel => await viewModel.initialize()
    );
  }

  addWorkflowProcessInstanceEditorBlockNode(): void {
    this.addWorkflowInstanceditorNode(
      'Instance Editor',
      async viewModel => await viewModel.initialize()
    );
  }

  addLinkEditorNode(
    caption: string,
    init: (viewModel: WorkflowLinkEditorViewModel) => Promise<void>,
    description?: string
  ): void {
    const processStorage = new WorkflowProcessStorage({
      Name: 'test-ProcessName',
      TemplateCardID: '8B6D5CA1-A195-4A1C-90A1-167645ED2CB1'
    });

    let storage: WorkflowLinkStorage;

    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        storage = new WorkflowLinkStorage();
        storage.name = 'test-LinkName';
        storage.caption = 'tes-LinkCaption';
        storage.linkMode = WorkflowLinkMode.Default;
        storage.signalProcessingMode = WorkflowSignalProcessingMode.Async;
        storage.lockProcess = true;
        storage.retryAllowed = false;
        storage.syncProcessing = false;
        storage.outCondition =
          'var outStr = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod...";\n' +
          'var outResult = str.Substring(10);\n';
        storage.outDescription =
          'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. \n' +
          'Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. \n' +
          'Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. \n' +
          'Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.';
        storage.inCondition =
          'var inStr = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod...";\n' +
          'var inResult = str.Substring(10);\n';
        storage.inDescription =
          'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. \n' +
          'Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. \n' +
          'Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. \n' +
          'Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.';

        const viewModel = new WorkflowLinkEditorViewModel(
          storage,
          processStorage,
          this._enumerationProvider,
          () => {},
          {
            readonly: () => false
          }
        );
        await init(viewModel);
        return { viewModel };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ width: '100%' })}>
          <WorkflowSettingsEditor viewModel={viewModel} editorClassName={'workflow-link-editor'} />
        </DemoForm>
      ),
      buttons: [
        UIButton.create({
          name: 'showStorage',
          caption: 'Show Storage',
          type: 'small',
          theme: 'control',
          icon: 'm-etc',
          visibility: Visibility.Visible,
          onMouseDown: async () => {
            await showMessage(
              TypedJsonConverter.serialize(storage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  addLinkEditorBlockNode(): void {
    this.addLinkEditorNode('Link Editor', async viewModel => await viewModel.initialize());
  }

  addWorkflowNodeEditorNode(
    caption: string,
    init: (viewModel: WorkflowNodeEditorViewModel) => Promise<void>,
    description?: string,
    designMode: boolean = true
  ): void {
    let storage: WorkflowNodeStorage;

    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        storage = new WorkflowNodeStorage();
        storage.name = 'test-NodeName';
        storage.caption = 'test-NodeCaption';
        storage.description =
          'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. \n' +
          'Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. \n' +
          'Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. \n' +
          'Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.';
        storage.icon = 'Thin91';
        storage.defaultSignals = ['Default', 'StopApprovalProcess'];

        const action1 = new WorkflowActionStorage();
        action1.id = Guid.newGuid();
        action1.name = 'WorkflowTaskAction_1';
        action1.caption = '$CardTypes_TypesNames_Task';
        action1.actionTypeId = 'dc541752-496d-45eb-ad71-92ab186d7601';
        action1.order = 0;
        storage.actions.push(action1);

        const action2 = new WorkflowActionStorage();
        action2.id = Guid.newGuid();
        action2.name = 'WorkflowTaskAction_2';
        action2.caption = '$CardTypes_TypesNames_Task';
        action2.actionTypeId = 'dc541752-496d-45eb-ad71-92ab186d7601';
        action2.order = 1;
        storage.actions.push(action2);

        const viewModel = new WorkflowNodeEditorViewModel(
          storage,
          null,
          null,
          null,
          this._actionDescriptorRegistry,
          this._enumerationProvider,
          () => designMode,
          () => {},
          () => {},
          this.iconSelector
        );
        viewModel.openActionAction = async action => {
          await showMessage(`Clicked action: ${action.name}`);
        };
        await init(viewModel);
        return { viewModel };
      },
      view: ({ viewModel }) => (
        <DemoForm
          customStyles={css => css({ width: '100%', display: 'flex', flexDirection: 'column' })}
        >
          <WorkflowSettingsEditor viewModel={viewModel} editorClassName={'workflow-node-editor'} />
        </DemoForm>
      ),
      buttons: [
        UIButton.create({
          name: 'showStorage',
          caption: 'Show Storage',
          type: 'small',
          theme: 'control',
          icon: 'm-etc',
          visibility: Visibility.Visible,
          onMouseDown: async () => {
            await showMessage(
              TypedJsonConverter.serialize(storage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  addWorkflowNodeInstanceditorNode(
    caption: string,
    init: (viewModel: WorkflowNodeStateEditorViewModel) => Promise<void>,
    description?: string
  ): void {
    let nodeStateStorage: WorkflowNodeStateStorage;
    let nodeStorage: WorkflowNodeStorage;
    let tasks: WorkflowInstanceTaskStorage[];
    let subprocesses: WorkflowInstanceSubprocessStorage[];

    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        nodeStateStorage = new WorkflowNodeStateStorage();

        nodeStorage = new WorkflowNodeStorage();
        nodeStorage.name = 'test-NodeName';
        nodeStorage.caption = 'test-NodeCaption';

        const action1 = new WorkflowActionStorage();
        action1.id = Guid.newGuid();
        action1.name = 'WorkflowTaskAction_1';
        action1.caption = '$CardTypes_TypesNames_Task';
        action1.actionTypeId = 'dc541752-496d-45eb-ad71-92ab186d7601';
        action1.order = 0;
        nodeStorage.actions.push(action1);

        const action2 = new WorkflowActionStorage();
        action2.id = Guid.newGuid();
        action2.name = 'WorkflowTaskAction_2';
        action2.caption = '$CardTypes_TypesNames_Task';
        action2.actionTypeId = 'dc541752-496d-45eb-ad71-92ab186d7601';
        action2.order = 1;
        nodeStorage.actions.push(action2);

        const actionState1 = new WorkflowActionStateStorage(action1);
        actionState1.setChanged(false);
        nodeStateStorage.actions.push(actionState1);

        const actionState2 = new WorkflowActionStateStorage(action2);
        // Специально, чтобы показать отработку маркера *
        actionState2.setChanged(true);
        nodeStateStorage.actions.push(actionState2);

        tasks = [];

        const task1 = new WorkflowInstanceTaskStorage();
        task1.rowId = Guid.newGuid();
        task1.roleId = '3db19fa0-228a-497f-873a-0250bf0a4ccb';
        task1.roleName = 'Admin';
        task1.userId = '3db19fa0-228a-497f-873a-0250bf0a4ccb';
        task1.userName = 'Admin';
        task1.planned = '6/7/2025 6:19:10 PM';
        task1.inProgress = '6/6/2025 6:19:10 PM';
        task1.typeId = '929e345c-acdf-41ea-acb6-6bb308de73ae';
        task1.typeCaption = '$AbTest_TypesNames_TestTask1';
        task1.digest = 'test_digest';
        task1.authorId = '3db19fa0-228a-497f-873a-0250bf0a4ccb';
        task1.authorName = 'Admin';
        task1.created = '6/6/2025 6:10:10 PM';
        task1.stateId = 1;
        task1.stateName = '$Cards_TaskStates_InWork';
        tasks.push(task1);

        subprocesses = [];

        const subprocess1 = new WorkflowInstanceSubprocessStorage();
        subprocess1.name = 'testSubprocesses_1';
        subprocess1.created = '6/7/2025 6:19:10 PM';
        subprocesses.push(subprocess1);

        const viewModel = new WorkflowNodeStateEditorViewModel(
          nodeStateStorage,
          nodeStorage,
          tasks,
          subprocesses,
          this._actionDescriptorRegistry,
          () => {},
          {
            readonly: () => false
          }
        );
        viewModel.openActionAction = async action => {
          await showMessage(`Clicked action: ${action.id}`);
        };
        await init(viewModel);
        return { viewModel };
      },
      view: ({ viewModel }) => (
        <DemoForm
          customStyles={css => css({ width: '100%', display: 'flex', flexDirection: 'column' })}
        >
          <WorkflowSettingsEditor viewModel={viewModel} editorClassName={'workflow-node-editor'} />
        </DemoForm>
      ),
      buttons: [
        UIButton.create({
          name: 'showStasteStorage',
          caption: 'Show State Storage',
          type: 'small',
          theme: 'control',
          icon: 'm-etc',
          visibility: Visibility.Visible,
          onMouseDown: async () => {
            await showMessage(
              TypedJsonConverter.serialize(nodeStateStorage.getStorage(), { stringifySpace: '  ' })
            );
          }
        }),
        UIButton.create({
          name: 'showNodeStorage',
          caption: 'Show Node Storage',
          type: 'small',
          theme: 'control',
          icon: 'm-etc',
          visibility: Visibility.Visible,
          onMouseDown: async () => {
            await showMessage(
              TypedJsonConverter.serialize(nodeStorage.getStorage(), { stringifySpace: '  ' })
            );
          }
        })
      ]
    });
  }

  addWorkflowNodeEditorBlockNodes(): void {
    this.addWorkflowNodeEditorNode(
      'Node Editor',
      async viewModel => await viewModel.initialize(),
      '',
      false
    );
    this.addWorkflowNodeEditorNode(
      'Node Editor [readonly]',
      async viewModel => await viewModel.initialize(),
      '',
      true
    );
  }

  addWorkflowNodeInstanceEditorBlockNode(): void {
    this.addWorkflowNodeInstanceditorNode(
      'Node Instance Editor',
      async viewModel => await viewModel.initialize()
    );
  }

  //#endregion
}
