import { ValidationResultBuilder } from '@tessa/core';
import { ICardMetadata, ICardMetadata$ } from '@tessa/platform';
import { inject, injectable } from '@tessa/application';
import { showNotEmpty } from 'tessa/ui';
import { IWorkflowActionHandlerContext, IWorkflowActionSettingsRegistry } from 'tessa/ui/workflow';
import { WorkflowActionHandlerBase } from 'tessa/ui/workflow/workflowActionHandlerBase';
import { IWorkflowActionSettingsRegistry$ } from 'tessa/ui/workflow/workflowInjects';
import { KrApprovalActionSettings } from '../models/krApprovalActionSettings';
import { KrWorkflowActionsHelper } from '../krWorkflowActionsHelper';
import { IKrWorkflowActionCompletionOptionsProvider$ } from '../../../injects';
import { IKrWorkflowActionCompletionOptionsProvider } from '../types';
import { ActionCompletionOptions } from '../actionCompletionOptions';
import { KrTaskOptionRowSettings } from '../models/krTaskOptionRowSettings';
import { KrActionOptionRowSettings } from '../models/krActionOptionRowSettings';

/**
 * Обработчик действия "Согласование".
 */
@injectable()
export class KrApprovalActionHandler extends WorkflowActionHandlerBase {
  //#region ctor

  constructor(
    @inject(IWorkflowActionSettingsRegistry$)
    protected readonly _workflowActionSettingsRegistry: IWorkflowActionSettingsRegistry,
    @inject(ICardMetadata$) protected readonly _cardMetadata: ICardMetadata,
    @inject(IKrWorkflowActionCompletionOptionsProvider$)
    protected readonly _actionCompletionOptionsProvider: IKrWorkflowActionCompletionOptionsProvider
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initializeAction(context: IWorkflowActionHandlerContext): Promise<void> {
    const actionSettingsFactory = await this._workflowActionSettingsRegistry.resolve();
    const actionWithSettings = actionSettingsFactory.create(context.action);
    const settings = actionWithSettings.settings as KrApprovalActionSettings;

    const validationResult = new ValidationResultBuilder();
    KrWorkflowActionsHelper.initializeTaskCompletionOptions(
      this._cardMetadata,
      settings.completeOptions,
      [
        'e4d7f6bf-fea9-4a3b-8a5a-e1a0a40de74c', // KrApprove
        'b3d8eae3-c6bf-4b59-bcc7-461d526c326c', // KrAdditionalApproval
        'f0360d95-4f88-4809-b926-57b34a2f69f5', // KrRequestComment
        'c9b93ae3-9b7b-4431-a306-aace4aea8732' // KrEditInterject
      ],
      validationResult,
      () => new KrTaskOptionRowSettings(actionWithSettings.action, actionWithSettings.actionState),
      KrApprovalActionHandler.name,
      true
    );
    showNotEmpty(validationResult.build());

    KrWorkflowActionsHelper.initializeActionCompletionOptions(
      this._actionCompletionOptionsProvider.getActionCompletionOptions(),
      settings.actionCompleteOptions,
      [ActionCompletionOptions.approved, ActionCompletionOptions.disapproved],
      () => new KrActionOptionRowSettings(actionWithSettings.action, actionWithSettings.actionState)
    );
  }

  //#endregion
}
