import { inject, injectable } from '@tessa/application';
import { AsyncLazy, TypedJsonConverter } from '@tessa/core';
import {
  ArrowOrientation,
  NodeShapes,
  WorkflowLinkMode,
  WorkflowSignalProcessingMode
} from 'tessa/ui/workflow/enums';
import { UIButton } from 'tessa/ui/uiButton';
import { Visibility } from 'tessa/platform';
import { WorkflowProcessStorage } from 'tessa/ui/workflow/chunk/models/workflowProcessStorage';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { TextFieldView } from 'ui/textField/textFieldView';
import { WorkflowEditor } from 'tessa/ui/workflow/chunk/layout/workflowEditor';
import { WorkflowEditorViewModel } from 'tessa/ui/workflow/chunk/layout/workflowEditorViewModel';
import { WorkflowLayoutArticleBase } from './workflowLayoutArticleBase';
import {
  IWorkflowActionDescriptorRegistry$,
  IWorkflowActionHandlerResolver$,
  IWorkflowActionSettingsRegistry$,
  IWorkflowApiManager$,
  IWorkflowEditorFactory$,
  IWorkflowNodeViewModelFactory$,
  IWorkflowSettingsEditorViewModelFactory$,
  WorkflowHashParameterComplexTypesCache$
} from 'tessa/ui/workflow/workflowInjects';
import { IWorkflowSettingsEditorViewModelFactory } from 'tessa/ui/workflow/chunk/editors/workflowEditorTypes';
import {
  IHistoryManagerFactory,
  IHistoryManagerFactory$,
  IViewRepository,
  IViewRepository$
} from '@tessa/platform';
import { WorkflowActionDescriptors } from 'tessa/ui/workflow/workflowActionDescriptors';
import { WorkflowScenarioActionSettings } from 'tessa/ui/workflow/chunk/models/actions/workflowScenarioActionSettings';
import {
  IWorkflowEditorFactory,
  IWorkflowNodeViewModelFactory,
  WorkflowEditorSettings
} from 'tessa/ui/workflow/chunk/layout/workflowTypes';
import {
  IWorkflowActionDescriptorRegistry,
  IWorkflowActionHandlerResolver,
  IWorkflowActionSettingsRegistry,
  IWorkflowApiManager
} from 'tessa/ui/workflow';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { WorkflowHashParameterComplexTypesCache } from 'tessa/ui/workflow/workflowHashParameterComplexTypesCache';
import { showError } from 'tessa/ui';

@injectable()
export class WorkflowLayoutEditorArticle extends WorkflowLayoutArticleBase {
  //#region ctors

  constructor(
    @inject(IWorkflowSettingsEditorViewModelFactory$)
    protected readonly _settingsFactory: IWorkflowSettingsEditorViewModelFactory,
    @inject(IHistoryManagerFactory$)
    protected readonly _historyManagerFactory: IHistoryManagerFactory,
    @inject(IWorkflowNodeViewModelFactory$)
    protected readonly _nodeFactory: IWorkflowNodeViewModelFactory,
    @inject(IWorkflowActionSettingsRegistry$)
    _actionSettingsRegistry: IWorkflowActionSettingsRegistry,
    @inject(IWorkflowActionDescriptorRegistry$)
    private readonly _actionDescriptorRegistry: IWorkflowActionDescriptorRegistry,
    @inject(IWorkflowApiManager$, { lazy: true })
    private readonly _workflowApiManager: AsyncLazy<IWorkflowApiManager>,
    @inject(IWorkflowEditorFactory$, { lazy: true })
    protected readonly _workflowEditorFactory: AsyncLazy<IWorkflowEditorFactory>,
    @inject(IWorkflowActionHandlerResolver$)
    protected readonly _workflowActionHandlerResolver: IWorkflowActionHandlerResolver,
    @inject(IViewRepository$)
    protected readonly _viewRepository: IViewRepository,
    @inject(WorkflowHashParameterComplexTypesCache$, { lazy: true })
    private readonly _complexTypesCache: AsyncLazy<WorkflowHashParameterComplexTypesCache>
  ) {
    super(_actionSettingsRegistry);
  }

  //#endregion

  //#region overrides

  override getSettings(): PlaygroundArticleSettings {
    return { name: 'Workflow/Editor', description: 'Workflow editor' };
  }

  override async initialize(): Promise<void> {
    await super.initialize();
    await this.addEditorTestBlockNode();
  }

  //#endregion

  //#region add blocks methods

  async addLayoutBlockNode(
    caption: string,
    init: (process: WorkflowProcessStorage, nodeFactory: IWorkflowNodeViewModelFactory) => void,
    description?: string
  ): Promise<void> {
    this.addBlock({
      caption: caption,
      description: description,
      props: async () => {
        const textField = new TextFieldViewModel();
        await textField.initialize();
        textField.minRows = 3;
        textField.maxRows = 10;

        const process = new WorkflowProcessStorage();
        init(process, this._nodeFactory);

        const editorSettings: WorkflowEditorSettings = {
          id: process.id,
          process: process,
          isLocked: true
        };
        const editor = new WorkflowEditorViewModel(
          editorSettings,
          this._settingsFactory,
          this._actionSettingsRegistry,
          this._actionDescriptorRegistry,
          this._nodeFactory,
          this.clipboardManager,
          this._historyManagerFactory,
          await this._workflowApiManager.getValue(),
          await this._workflowEditorFactory.getValue(),
          this._workflowActionHandlerResolver,
          this._viewRepository,
          await this._complexTypesCache.getValue()
        );

        await editor.initialize();

        return { textField, viewModel: editor, process };
      },
      view: ({ viewModel, textField }) => (
        <>
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
              height: 900px;
            `}
          >
            <TextFieldView viewModel={textField} />
            <WorkflowEditor viewModel={viewModel} />
          </DemoForm>
        </>
      ),
      buttons: ({ textField, process, viewModel: editor }) => [
        UIButton.create({
          name: 'showStructure',
          caption: 'Show structure',
          type: 'small',
          theme: 'control',
          icon: 'm-refresh',
          visibility: Visibility.Visible,
          onMouseDown: () => {
            if (!process) {
              return;
            }

            const json = TypedJsonConverter.serialize(process.getStorage(), {
              stringifySpace: 2
            });
            textField.text = json;
          }
        }),
        UIButton.create({
          name: 'loadStructure',
          caption: 'Load structure',
          type: 'small',
          theme: 'control',
          icon: 'm-refresh',
          visibility: Visibility.Visible,
          onMouseDown: () => {
            if (!textField.text) {
              return;
            }

            try {
              const storage = TypedJsonConverter.deserialize(textField.text);

              if (editor) {
                process = new WorkflowProcessStorage(storage);
                editor.setWorkflowProcess({
                  process,
                  id: process.id,
                  isLocked: !editor.layout.readonly
                });
              }
            } catch (e) {
              showError(e.message);
            }
          }
        }),
        UIButton.create({
          name: 'ChangeReadonly',
          caption: 'Change readonly',
          type: 'small',
          theme: 'control',
          icon: 'm-refresh',
          visibility: Visibility.Visible,
          onMouseDown: () => {
            if (editor) {
              editor.layout.readonly = !editor.layout.readonly;
            }
          }
        })
      ]
    });
  }

  addEditorTestBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('ConnectionTest', (process, nodeFactory) => {
      const node1 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.ellipse,
        icon: 'Thin15'
      });
      this.addTestAction({
        node: node1,
        actionType: WorkflowActionDescriptors.startProcessDescriptor
      });
      const node2 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.rectangle,
        left: 350,
        icon: 'Thin155'
      });
      this.addTestAction({
        node: node2,
        actionType: WorkflowActionDescriptors.scenarioDescriptor,
        preScript: 'pre script',
        postScript: 'post script',
        initSettings: action => {
          const settings = action.settings as WorkflowScenarioActionSettings;
          settings.script = 'test script';
        }
      });
      this.addTestAction({
        node: node2,
        actionType: WorkflowActionDescriptors.taskDescriptor
      });
      const node3 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.rhombus,
        left: 600,
        icon: 'Thin215'
      });
      this.addTestAction({
        node: node3,
        actionType: WorkflowActionDescriptors.taskDescriptor
      });
      const node4 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.ellipse,
        left: 850,
        icon: 'Thin254'
      });
      this.addTestAction({
        node: node4,
        actionType: WorkflowActionDescriptors.endProcessDescriptor
      });
      process.nodes.push(node1, node2, node3, node4);

      const edge1 = this.createTestLink({
        fromNode: node1.id,
        toNode: node2.id,
        caption: `multiline
test caption`
      });
      const edge2 = this.createTestLink({
        fromNode: node2.id,
        toNode: node3.id,
        fromOrientation: ArrowOrientation.Bottom,
        toOrientation: ArrowOrientation.Bottom,
        anchors: [
          { x: 400, y: 350 },
          { x: 600, y: 350 }
        ],
        useAnchorPosition: true,
        captionAnchorPosition: 0.35,
        caption: 'another very long caption',
        linkMode: WorkflowLinkMode.NeverCreateNew,
        signalProcessingMode: WorkflowSignalProcessingMode.Async
      });
      const edge3 = this.createTestLink({
        fromNode: node2.id,
        toNode: node3.id,
        caption: 'very long caption',
        inCondition: 'test',
        linkMode: WorkflowLinkMode.AlwaysCreateNew,
        signalProcessingMode: WorkflowSignalProcessingMode.AfterUploadingFiles
      });
      const edge4 = this.createTestLink({
        fromNode: node3.id,
        toNode: node4.id,
        caption: 'final caption'
      });
      process.links.push(edge1, edge2, edge3, edge4);
    });
  }

  //#endregion
}
