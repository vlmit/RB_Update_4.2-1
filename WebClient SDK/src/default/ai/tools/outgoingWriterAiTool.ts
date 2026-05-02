import { NotImplementedError, ValueOrFactory } from '@tessa/core';
import { inject, injectable } from '@tessa/application';
import {
  AiAssistantDialogScopeContext,
  AiAutoAction,
  AiHelper,
  AiToolContext,
  IAiAssistantLinkManager,
  IAiAssistantLinkManager$,
  IAiTool,
  IAiToolDataViewModel
} from 'tessa/ui/ai';

/**
 * An AI agent tool for creating an outgoing email from an incoming document card.
 */
@injectable()
export class OutgoingWriterAiTool implements IAiTool {
  //#region static

  static readonly key = 'outgoing_write';

  //#endregion

  //#region ctor

  constructor(
    @inject(IAiAssistantLinkManager$)
    private readonly _aiAssistantLinkManager: IAiAssistantLinkManager
  ) {}

  //#endregion

  //#region IAiTool members

  readonly id = OutgoingWriterAiTool.key;

  async getViewModel(
    _context: AiToolContext,
    _isActive: ValueOrFactory<boolean>
  ): Promise<IAiToolDataViewModel> {
    throw new NotImplementedError();
  }

  async processAction(context: AiToolContext, actionId: string): Promise<void> {
    if (
      context.aiMessage.action instanceof AiAutoAction &&
      context.aiMessage.action.id === actionId &&
      (await AiHelper.tryProcessAutoAction(this._aiAssistantLinkManager, context.aiMessage.action))
    ) {
      await AiAssistantDialogScopeContext.last?.dialog.close();
    }
  }

  //#endregion
}
