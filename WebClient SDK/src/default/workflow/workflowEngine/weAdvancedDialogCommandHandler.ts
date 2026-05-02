import { IStorage, StorageHelper, TypedField } from '@tessa/core';
import { extension, inject, ISession, ISession$ } from '@tessa/application';
import {
  CardTaskCompletionOptionSettings,
  CardTaskDialogActionResult,
  ICardMetadataRepository,
  ICardMetadataRepository$,
  IClientCommandHandlerContext,
  IWorkflowEngineProcessor,
  IWorkflowEngineProcessor$,
  WorkflowEngineProcessRequest
} from '@tessa/platform';
import { ICardEditorModel } from 'tessa/ui/cards';
import { showNotEmpty } from 'tessa/ui';
import { AdvancedDialogCommandHandler } from '../krProcess/commandInterpreter/advancedDialogCommandHandler';
import { IFormBuilder$, IFormService$ } from 'tessa/ui/formEditor/injects';
import { IFormBuilder, IFormService } from 'tessa/ui/formEditor/types';

/**
 * Обработчик клиентской команды DefaultCommandTypes.WeShowAdvancedDialog.
 */
@extension({ name: 'WeAdvancedDialogCommandHandler' })
export class WeAdvancedDialogCommandHandler extends AdvancedDialogCommandHandler {
  constructor(
    @inject(ICardMetadataRepository$) cardMetadataRepository: ICardMetadataRepository,
    @inject(ISession$) session: ISession,
    @inject(IFormService$) formService: IFormService,
    @inject(IFormBuilder$) formBuilder: IFormBuilder,
    @inject(IWorkflowEngineProcessor$) private readonly _workflowProcessor: IWorkflowEngineProcessor
  ) {
    super(cardMetadataRepository, session, formService, formBuilder);
  }

  protected prepareDialogCommand(
    context: IClientCommandHandlerContext
  ): CardTaskCompletionOptionSettings | null {
    const coSettings = StorageHelper.tryGet<IStorage>(
      context.command.parameters,
      'CompletionOptionSettings'
    );
    const dialogSettings = StorageHelper.tryGet<IStorage>(
      coSettings,
      StorageHelper.systemKeyPrefix + 'DialogSettings'
    );
    if (dialogSettings) {
      return new CardTaskCompletionOptionSettings(dialogSettings);
    }
    return null;
  }

  protected async completeDialogCore(
    actionResult: CardTaskDialogActionResult,
    context: IClientCommandHandlerContext,
    _dialogCardEditor: ICardEditorModel | null,
    parentCardEditor: ICardEditorModel | null
  ): Promise<boolean> {
    const coSettings = StorageHelper.tryGet<IStorage>(
      context.command.parameters,
      'CompletionOptionSettings'
    )!;
    const { request, requestSignature } = this.getProcessRequest(coSettings);

    const contextResponse = StorageHelper.tryGet<IStorage>(
      context.outerContext as IStorage,
      'response'
    );
    if (!contextResponse) {
      return true;
    }

    const responseInfo = contextResponse.info ?? {};
    const additionalInfo: IStorage = {};
    additionalInfo[StorageHelper.systemKeyPrefix + 'CardTaskDialogActionResult'] =
      actionResult.getStorage();

    const processObj = StorageHelper.tryGet<IStorage>(
      responseInfo as IStorage,
      StorageHelper.systemKeyPrefix + 'WorkflowEngineProcessSerializedKey'
    );
    if (processObj) {
      additionalInfo[StorageHelper.systemKeyPrefix + 'WorkflowEngineProcessSerializedKey'] =
        processObj;
    }

    const result = await this._workflowProcessor.processSignalAsync(
      request!,
      requestSignature,
      additionalInfo,
      request => {
        const requestInfo = request.info;
        requestInfo[StorageHelper.systemKeyPrefix + 'WebAdvancedDialogCommandSkipUIContextFlag'] =
          TypedField.trueBoolean;
      }
    );

    await showNotEmpty(result.validationResult);
    if (result.validationResult.isSuccessful && parentCardEditor) {
      await parentCardEditor.refreshCard(parentCardEditor.context);
    }

    return !!(
      result.validationResult.isSuccessful &&
      result.responseInfo &&
      !StorageHelper.tryGet(result.responseInfo, `${StorageHelper.systemKeyPrefix}KeepTaskDialog`)
    );
  }

  private getProcessRequest(info: IStorage) {
    let request: WorkflowEngineProcessRequest | null = null;
    const requestFromInfo = StorageHelper.tryGet<IStorage>(
      info,
      StorageHelper.systemKeyPrefix + 'ProcessRequest'
    );
    if (requestFromInfo) {
      request = new WorkflowEngineProcessRequest(requestFromInfo);
    }
    const requestSignature = StorageHelper.tryGet<string>(
      info,
      StorageHelper.systemKeyPrefix + 'ProcessRequestSignature'
    );
    return { request, requestSignature };
  }
}
