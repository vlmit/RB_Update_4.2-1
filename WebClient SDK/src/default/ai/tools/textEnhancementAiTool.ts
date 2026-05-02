import { NotImplementedError, ValueOrFactory } from '@tessa/core';
import { injectable, localize } from '@tessa/application';
import {
  AiAction,
  AiAssistantDialogScopeContext,
  AiTextMessagePart,
  AiToolContext,
  IAiTool,
  IAiToolDataViewModel
} from 'tessa/ui/ai';

//TODO: Будет изменено в рамках задачи https://gitlab.syntellect.ru/tessa_team/tessa/-/issues/7043

/**
 * An AI agent tool for text enhancement text field in the card.
 */
@injectable()
export class TextEnhancementAiTool implements IAiTool {
  //#region static

  /** AI tool unique identifier for using at {@link AiToolProvider}. */
  static readonly key = 'text_enhancement';

  //#endregion

  //#region IAiTool members

  readonly id = TextEnhancementAiTool.key;

  async getViewModel(
    _context: AiToolContext,
    _isActive: ValueOrFactory<boolean>
  ): Promise<IAiToolDataViewModel> {
    throw new NotImplementedError();
  }

  async processAction(context: AiToolContext, actionId: string): Promise<void> {
    if (actionId !== AiAction.apply.id) {
      return;
    }

    const text = context.aiMessage.content
      ?.filter(c => c instanceof AiTextMessagePart)
      .map((c: AiTextMessagePart) => c.text)
      .join('\n');

    if (!text) {
      throw new Error(localize('$Ai_AiAgent_Validation_TextEnhancementTool_NoText'));
    }

    const dialogContext = AiAssistantDialogScopeContext.last;

    if (!dialogContext) {
      throw new Error(localize('$Ai_AiAgent_Validation_InvalidContext'));
    }

    await dialogContext.dialog.close(text);
  }

  //#endregion
}
