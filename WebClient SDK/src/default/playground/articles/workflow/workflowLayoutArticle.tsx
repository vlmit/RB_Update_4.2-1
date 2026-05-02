import { inject, injectable } from '@tessa/application';
import { WorkflowLayout } from 'tessa/ui/workflow/chunk/layout/workflowLayout';
import { TypedJsonConverter } from '@tessa/core';
import {
  ArrowOrientation,
  NodeShapes,
  WorkflowLinkMode,
  WorkflowSignalProcessingMode
} from 'tessa/ui/workflow/enums';
import { UIButton } from 'tessa/ui/uiButton';
import { Visibility } from 'tessa/platform';
import { WorkflowProcessStorage } from 'tessa/ui/workflow/chunk/models/workflowProcessStorage';
import { WorkflowArticleNodeViewModelFactory } from './workflowArticleNodeViewModelFactory';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { TextFieldView } from 'ui/textField/textFieldView';
import { WorkflowLayoutManager } from './workflowLayoutManager';
import { WorkflowLayoutArticleBase } from './workflowLayoutArticleBase';
import { IHistoryManagerFactory, IHistoryManagerFactory$ } from '@tessa/platform';
import { IWorkflowActionSettingsRegistry$ } from 'tessa/ui/workflow/workflowInjects';
import { IWorkflowActionSettingsRegistry } from 'tessa/ui/workflow';
import { DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';

@injectable()
export class WorkflowLayoutArticle extends WorkflowLayoutArticleBase {
  //#region constructors

  constructor(
    @inject(IHistoryManagerFactory$)
    protected readonly _historyManagerFactory: IHistoryManagerFactory,
    @inject(IWorkflowActionSettingsRegistry$)
    _actionSettingsRegistry: IWorkflowActionSettingsRegistry
  ) {
    super(_actionSettingsRegistry);
  }

  //#endregion

  //#region overrides

  override getSettings(): PlaygroundArticleSettings {
    return { name: 'Workflow/Layout', description: 'Workflow editor layout' };
  }

  override async initialize(): Promise<void> {
    await super.initialize();

    await this.addRectangleBlockNode();
    await this.addEllipseBlockNode();
    await this.addRhombusBlockNode();
    await this.addConnectionTestBlockNode();
    await this.addAutoConnectionBlockNode();
    await this.addStressTestBlockNode();
  }

  //#endregion

  //#region add blocks methods

  async addLayoutBlockNode(
    caption: string,
    init: (
      process: WorkflowProcessStorage,
      nodeFactory: WorkflowArticleNodeViewModelFactory
    ) => void,
    description?: string
  ): Promise<void> {
    this.addBlock({
      caption: caption,
      description: description,
      props: async ctx => {
        const layoutManager = new WorkflowLayoutManager(
          this.clipboardManager,
          this._historyManagerFactory,
          await this._actionSettingsRegistry.resolve(),
          () => new WorkflowArticleNodeViewModelFactory(),
          init
        );

        const textField1 = new TextFieldViewModel();
        await textField1.initialize();
        textField1.minRows = 3;
        textField1.maxRows = 10;

        layoutManager.resetIfRequired();

        return {
          viewModel: layoutManager.layoutViewModel,
          layoutManager,
          textField: textField1,
          block: ctx.block
        };
      },
      view: ({ viewModel, textField }) => (
        <>
          <DemoForm
            customStyles={css => css`
              flex-direction: column;
              gap: 10px;
              height: 800px;
            `}
          >
            <TextFieldView viewModel={textField} />
            <WorkflowLayout viewModel={viewModel} />
          </DemoForm>
        </>
      ),
      buttons: ({ layoutManager, textField, block }) => [
        UIButton.create({
          name: 'resetLayout',
          caption: 'Reset layout',
          type: 'small',
          theme: 'control',
          icon: 'm-refresh',
          visibility: Visibility.Visible,
          onMouseDown: () => {
            layoutManager.reset('view');
            block.resetView();
          }
        }),
        UIButton.create({
          name: 'resetViewModel',
          caption: 'Reset view model',
          type: 'small',
          theme: 'control',
          icon: 'm-refresh',
          visibility: Visibility.Visible,
          onMouseDown: () => {
            layoutManager.reset('viewmodel');
            block.resetView();
          }
        }),
        UIButton.create({
          name: 'showStructure',
          caption: 'Show structure',
          type: 'small',
          theme: 'control',
          icon: 'm-refresh',
          visibility: Visibility.Visible,
          onMouseDown: () => {
            if (!layoutManager.process) {
              return;
            }

            const json = TypedJsonConverter.serialize(layoutManager.process.getStorage(), {
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

            const storage = TypedJsonConverter.deserialize(textField.text);
            layoutManager.reset(storage);
            block.resetView();
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
            layoutManager.layoutViewModel.readonly = !layoutManager.layoutViewModel.readonly;
          }
        })
      ]
    });
  }

  addRectangleBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('RectangleNode', (process, nodeFactory) =>
      process.nodes.push(this.createTestNode({ nodeFactory, icon: 'Thin1' }))
    );
  }

  addEllipseBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('EllipseNode', (process, nodeFactory) =>
      process.nodes.push(
        this.createTestNode({ nodeFactory, shape: NodeShapes.ellipse, icon: 'Thin10' })
      )
    );
  }

  addRhombusBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('RhombusNode', (process, nodeFactory) =>
      process.nodes.push(
        this.createTestNode({ nodeFactory, shape: NodeShapes.rhombus, icon: 'Thin100' })
      )
    );
  }

  addConnectionTestBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('ConnectionTest', (process, nodeFactory) => {
      const node1 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.rectangle,
        icon: 'Thin15'
      });
      const node2 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.ellipse,
        left: 350,
        icon: 'Thin155'
      });
      const node3 = this.createTestNode({
        nodeFactory,
        shape: NodeShapes.rhombus,
        left: 600,
        icon: 'Thin215'
      });
      process.nodes.push(node1, node2, node3);

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
      process.links.push(edge1, edge2, edge3);
    });
  }

  addAutoConnectionBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('AutoConnection', (process, nodeFactory) => {
      const nodeCenter = this.createTestNode({
        nodeFactory,
        left: 400,
        top: 400,
        caption: 'center'
      });
      const nodeN = this.createTestNode({ nodeFactory, left: 400, top: 100, caption: 'N' });
      const nodeNE = this.createTestNode({ nodeFactory, left: 600, top: 200, caption: 'NE' });
      const nodeE = this.createTestNode({ nodeFactory, left: 700, top: 400, caption: 'E' });
      const nodeSE = this.createTestNode({ nodeFactory, left: 600, top: 600, caption: 'SE' });
      const nodeS = this.createTestNode({ nodeFactory, left: 400, top: 700, caption: 'S' });
      const nodeSW = this.createTestNode({ nodeFactory, left: 200, top: 600, caption: 'SW' });
      const nodeW = this.createTestNode({ nodeFactory, left: 100, top: 400, caption: 'W' });
      const nodeNW = this.createTestNode({ nodeFactory, left: 200, top: 200, caption: 'NW' });
      process.nodes.push(nodeCenter, nodeN, nodeNE, nodeE, nodeSE, nodeS, nodeSW, nodeW, nodeNW);

      for (let i = 1; i < process.nodes.length; i++) {
        process.links.push(
          this.createTestLink({
            fromNode: nodeCenter.id,
            toNode: process.nodes[i].id,
            fromOrientation: ArrowOrientation.NoOrientation,
            toOrientation: ArrowOrientation.NoOrientation
          })
        );
      }
    });
  }

  addStressTestBlockNode(): Promise<void> {
    return this.addLayoutBlockNode('StressTest', (process, nodeFactory) => {
      const nodesCount = 100;
      const nodesPerRow = 10;

      let index = 0;
      while (index < nodesCount) {
        const rowNum = Math.trunc(index / nodesPerRow);
        const columnNum = index % nodesPerRow;
        process.nodes.push(
          this.createTestNode({
            nodeFactory,
            caption: `Node #${index}`,
            left: 50 + columnNum * 150,
            top: 50 + rowNum * 150
          })
        );

        index++;
      }

      index = 1;
      while (index < nodesCount) {
        process.links.push(
          this.createTestLink({
            fromNode: process.nodes[index - 1].id,
            toNode: process.nodes[index].id,
            fromOrientation: index % 10 == 0 ? ArrowOrientation.Bottom : ArrowOrientation.Right,
            toOrientation: index % 10 == 0 ? ArrowOrientation.Top : ArrowOrientation.Left
          })
        );

        index++;
      }
    });
  }

  //#endregion
}
