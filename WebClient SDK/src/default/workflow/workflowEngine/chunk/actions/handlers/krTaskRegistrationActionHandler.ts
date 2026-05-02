import { ValidationResultBuilder } from '@tessa/core';
import { ICardMetadata, ICardMetadata$ } from '@tessa/platform';
import { inject, injectable } from '@tessa/application';
import { showNotEmpty } from 'tessa/ui';
import { IWorkflowActionHandlerContext, IWorkflowActionSettingsRegistry } from 'tessa/ui/workflow';
import { WorkflowActionHandlerBase } from 'tessa/ui/workflow/workflowActionHandlerBase';
import { IWorkflowActionSettingsRegistry$ } from 'tessa/ui/workflow/workflowInjects';
import { KrTaskRegistrationActionSettings } from '../models/krTaskRegistrationActionSettings';
import { KrWorkflowActionsHelper } from '../krWorkflowActionsHelper';
import { IKrWorkflowActionCompletionOptionsProvider$ } from '../../../injects';
import { IKrWorkflowActionCompletionOptionsProvider } from '../types';
import { KrTaskRegistrationActionOptionRowSettings } from '../models/krTaskRegistrationActionOptionRowSettings';

/**
 * Обработчик действия "Задание регистрации".
 */
@injectable()
export class KrTaskRegistrationActionHandler extends WorkflowActionHandlerBase {
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
    const settings = actionWithSettings.settings as KrTaskRegistrationActionSettings;

    const validationResult = new ValidationResultBuilder();
    KrWorkflowActionsHelper.initializeTaskCompletionOptions(
      this._cardMetadata,
      settings.completeOptions,
      [
        '09fdd6a3-3946-4f30-9ef9-f533fad3a4a2' // KrRegistration
      ],
      validationResult,
      () =>
        new KrTaskRegistrationActionOptionRowSettings(
          actionWithSettings.action,
          actionWithSettings.actionState
        ),
      KrTaskRegistrationActionHandler.name
    );
    showNotEmpty(validationResult.build());
  }

  //#endregion
}
