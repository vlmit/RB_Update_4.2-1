import { injectable, localize } from '@tessa/application';
import {
  NotImplementedError,
  StorageHelper,
  ValidationError,
  ValidationResult,
  ValueOrFactory
} from '@tessa/core';
import { AiTableWithText, AiToolContext, IAiTool, IAiToolDataViewModel } from 'tessa/ui/ai';
import { AiTableWithTextViewModel } from '../models/aiTableWithTextViewModel';

/**
 * Плагин ИИ агента для получения информации о договорах.
 */
@injectable()
export class ContractInfoAiTool implements IAiTool {
  //#region static

  static readonly key = 'contract_info';

  //#endregion

  //#region IAiTool members

  readonly id = ContractInfoAiTool.key;

  async getViewModel(
    context: AiToolContext,
    isActive: ValueOrFactory<boolean>
  ): Promise<IAiToolDataViewModel> {
    if (!context.setInteractionModeMessageTable) {
      const error = localize('$Ai_AiAgent_Validation_MissingParameter');
      throw new ValidationError(ValidationResult.fromText(error));
    }

    const data = context.aiMessage.data;

    if (!StorageHelper.isStorage(data)) {
      const error = localize('$Ai_AiAgent_Validation_FailedToDeserializeAiResponse');
      throw new ValidationError(ValidationResult.fromText(error));
    }

    const aiTableWithText = new AiTableWithText().deserializeFromStorage(data);

    if (
      !aiTableWithText.table.columns.length ||
      !aiTableWithText.table.rows.length ||
      !aiTableWithText.text.trim()
    ) {
      const error = localize('$Ai_AiAgent_Validation_FailedToDeserializeAiResponse');
      throw new ValidationError(ValidationResult.fromText(error));
    }

    context.setInteractionModeMessageTable(aiTableWithText.table);
    const viewModel = new AiTableWithTextViewModel(
      aiTableWithText.text,
      aiTableWithText.table,
      isActive,
      context.assistant
    );
    await viewModel.initialize();
    return viewModel;
  }

  processAction(_context: AiToolContext, _actionId: ValueOrFactory<string>): Promise<void> {
    throw new NotImplementedError();
  }

  //#endregion
}
