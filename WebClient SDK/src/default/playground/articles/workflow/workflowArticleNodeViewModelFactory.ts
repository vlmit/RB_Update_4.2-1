import { NodeThemes, NodeShapes } from 'tessa/ui/workflow/enums';
import { WorkflowNodeViewModel } from 'tessa/ui/workflow/chunk/layout/nodes/workflowNodeViewModel';
import {
  IWorkflowLayoutViewModel,
  IWorkflowNodeViewModel,
  IWorkflowNodeViewModelFactory
} from 'tessa/ui/workflow/chunk/layout/workflowTypes';
import { WorkflowNodeStorage } from 'tessa/ui/workflow/chunk/models/workflowNodeStorage';

export type NodeShapeData = {
  id: string;
  shape: string;
  background: string;
};

export class WorkflowArticleNodeViewModelFactory implements IWorkflowNodeViewModelFactory {
  nodesShapeData: NodeShapeData[] = [];

  createNode(layout: IWorkflowLayoutViewModel, node: WorkflowNodeStorage): IWorkflowNodeViewModel {
    const viewModel = new WorkflowNodeViewModel(layout, node);
    const nodeShapeData = this.nodesShapeData.find(x => x.id === node.id);
    if (nodeShapeData) {
      viewModel.shape = nodeShapeData.shape;
      viewModel.theme = nodeShapeData.background;
    } else {
      viewModel.shape = NodeShapes.rectangle;
      viewModel.theme = NodeThemes.default;
    }

    return viewModel;
  }
}
