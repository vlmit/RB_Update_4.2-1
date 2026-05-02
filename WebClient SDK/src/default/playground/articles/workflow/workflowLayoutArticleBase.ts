import { Guid } from '@tessa/core';
import { WorkflowNodeStorage } from 'tessa/ui/workflow/chunk/models/workflowNodeStorage';
import { WorkflowLinkStorage } from 'tessa/ui/workflow/chunk/models/workflowLinkStorage';
import {
  ArrowOrientation,
  NodeThemes,
  NodeShapes,
  WorkflowLinkMode,
  WorkflowSignalProcessingMode
} from 'tessa/ui/workflow/enums';
import { WorkflowAnchorStorage } from 'tessa/ui/workflow/chunk/models/workflowAnchorStorage';
import { WorkflowArticleNodeViewModelFactory } from './workflowArticleNodeViewModelFactory';
import { WorkflowClipboardManager } from 'tessa/ui/workflow/chunk/layout/workflowClipboardManager';
import { WorkflowActionWithSettingsBase } from 'tessa/ui/workflow/chunk/models/workflowActionWithSettingsBase';
import { IWorkflowNodeViewModelFactory } from 'tessa/ui/workflow/chunk/layout/workflowTypes';
import { WorkflowActionDescriptor } from 'tessa/ui/workflow/workflowActionDescriptor';
import { WorkflowHelper } from 'tessa/ui/workflow/chunk/models/workflowHelper';
import { IWorkflowActionSettingsFactory, IWorkflowActionSettingsRegistry } from 'tessa/ui/workflow';
import { PlaygroundArticle } from 'tessa/ui/playground/chunk';

export abstract class WorkflowLayoutArticleBase extends PlaygroundArticle {
  //#region fields

  protected readonly clipboardManager = new WorkflowClipboardManager();

  protected actionSettingsFactory: IWorkflowActionSettingsFactory;

  //#endregion

  //#region ctor

  constructor(protected readonly _actionSettingsRegistry: IWorkflowActionSettingsRegistry) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initialize(): Promise<void> {
    this.actionSettingsFactory = await this._actionSettingsRegistry.resolve();
  }

  //#endregion

  //#region other methods

  protected createTestNode(args: {
    nodeFactory: IWorkflowNodeViewModelFactory;
    name?: string;
    caption?: string;
    height?: number;
    width?: number;
    left?: number;
    top?: number;
    shape?: string;
    background?: string;
    icon?: string;
  }): WorkflowNodeStorage {
    const model = new WorkflowNodeStorage();
    model.id = Guid.newGuid();
    model.width = args.width ?? 100;
    model.height = args.height ?? 100;
    model.left = args.left ?? 100;
    model.top = args.top ?? 100;
    model.caption = args.caption ?? args.shape ?? NodeShapes.rectangle;
    model.name = args.name ?? args.shape ?? NodeShapes.rectangle;
    model.icon = args.icon ?? null;

    if (args.nodeFactory instanceof WorkflowArticleNodeViewModelFactory) {
      args.nodeFactory.nodesShapeData.push({
        id: model.id,
        background: args.background ?? NodeThemes.default,
        shape: args.shape ?? NodeShapes.rectangle
      });
    }

    return model;
  }

  protected addTestAction(args: {
    node: WorkflowNodeStorage;
    actionType: WorkflowActionDescriptor;
    caption?: string;
    name?: string;
    preConditions?: string[];
    preScript?: string;
    postScript?: string;
    initSettings?: (action: WorkflowActionWithSettingsBase) => void;
  }): void {
    args.node.isStandAlone = args.actionType.isStandAlone;
    const action = args.node.actions.add();
    action.id = Guid.newGuid();
    action.order = args.node.actions.length - 1;
    action.actionTypeId = args.actionType.id;
    action.caption = args.caption ?? args.node.caption;
    action.name = args.name ?? args.node.name;
    WorkflowHelper.correctName(action, args.node.actions);
    if (args.preConditions) {
      action.preConditions.push(...args.preConditions);
    }
    if (args.preScript) {
      action.preScript = args.preScript;
    }
    if (args.postScript) {
      action.postScript = args.postScript;
    }
    if (args.initSettings) {
      const actionWithSettings = action.resolveSettings(this.actionSettingsFactory);
      args.initSettings(actionWithSettings);
    }
  }

  protected createTestLink(args: {
    fromNode: string;
    toNode: string;
    fromOrientation?: ArrowOrientation;
    toOrientation?: ArrowOrientation;
    caption?: string;
    useAnchorPosition?: boolean;
    captionAnchorPosition?: number;
    captionAnchorOffsetX?: number;
    captionAnchorOffsetY?: number;
    linkMode?: WorkflowLinkMode;
    signalProcessingMode?: WorkflowSignalProcessingMode;
    anchors?: { x: number; y: number }[];
    inCondition?: string;
    outCondition?: string;
  }): WorkflowLinkStorage {
    const model = new WorkflowLinkStorage();
    model.id = Guid.newGuid();
    model.fromNode = args.fromNode;
    model.toNode = args.toNode;
    model.fromOrientation = args.fromOrientation ?? ArrowOrientation.Right;
    model.toOrientation = args.toOrientation ?? ArrowOrientation.Left;
    model.fromPosition = 0.5;
    model.toPosition = 0.5;
    model.caption = args.caption ?? null;
    model.useAnchorPosition = args.useAnchorPosition ?? false;
    model.captionAnchorPosition = args.captionAnchorPosition ?? 0;
    model.captionAnchorOffsetX = args.captionAnchorOffsetX ?? 0;
    model.captionAnchorOffsetY = args.captionAnchorOffsetY ?? 0;
    model.linkMode = args.linkMode ?? WorkflowLinkMode.Default;
    model.signalProcessingMode = args.signalProcessingMode ?? WorkflowSignalProcessingMode.Default;
    model.inCondition = args.inCondition;
    model.outCondition = args.outCondition;

    if (args.anchors) {
      model.anchors = args.anchors.map(x => {
        const anchor = new WorkflowAnchorStorage();
        anchor.x = x.x;
        anchor.y = x.y;
        return anchor;
      });
    }

    return model;
  }

  //#endregion
}
