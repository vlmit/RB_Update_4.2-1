import {
  NotImplementedError,
  StorageHelper,
  ValidationError,
  ValidationResult,
  ValidationResultType,
  ValueOrFactory
} from '@tessa/core';
import { injectable, localize } from '@tessa/application';
import { AiToolContext, IAiTool, IAiToolDataViewModel } from 'tessa/ui/ai';
import {
  AiCreateIncomingViewModel,
  IncomingDocumentInfo
} from '../models/aiCreateIncomingViewModel';

/**
 * An AI agent tool for creating an incoming document card (file tool).
 */
@injectable()
export class CreateIncomingAiTool implements IAiTool {
  //#region static

  /** AI tool unique identifier for using at {@link AiToolProvider}. */
  static readonly key = 'incoming_create_card';

  //#endregion

  //#region IAiTool members

  readonly id = CreateIncomingAiTool.key;

  async getViewModel(
    context: AiToolContext,
    isActive: ValueOrFactory<boolean>
  ): Promise<IAiToolDataViewModel> {
    const { data, tool } = context.aiMessage;

    if (!StorageHelper.isStorage(data)) {
      throw new ValidationError(
        ValidationResult.fromText(
          localize('$Ai_AiAgent_Validation_FailedToDeserializeAiResponse', tool),
          ValidationResultType.Error
        )
      );
    }

    const dataTyped = new IncomingDocumentInfo().deserializeFromStorage(data);
    const viewModel = new AiCreateIncomingViewModel(dataTyped, isActive);
    await viewModel.initialize();
    return viewModel;
  }

  async processAction(): Promise<void> {
    throw new NotImplementedError();
  }

  //#endregion
}
