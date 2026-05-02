import { IStorage, StorageHelper, TypedField } from '@tessa/core';
import { extension, inject, ISession, ISession$ } from '@tessa/application';
import {
  CardTaskCompletionOptionSettings,
  CardTaskDialogActionResult,
  ICardMetadataRepository,
  ICardMetadataRepository$,
  IClientCommandHandlerContext,
  KrProcessInstance
} from '@tessa/platform';
import { AdvancedDialogCommandHandler } from './advancedDialogCommandHandler';
import { launchProcess } from 'tessa/ui/workflow/krProcess';
import { ICardEditorModel } from 'tessa/ui/cards';
import { IFormBuilder, IFormService } from 'tessa/ui/formEditor/types';
import { IFormBuilder$, IFormService$ } from 'tessa/ui/formEditor/injects';

/**
 * Обработчик клиентской команды `DefaultCommandTypes.ShowAdvancedDialog`.
 */
@extension({ name: 'KrAdvancedDialogCommandHandler' })
export class KrAdvancedDialogCommandHandler extends AdvancedDialogCommandHandler {
  //#region ctor

  constructor(
    @inject(ICardMetadataRepository$) cardMetadataRepository: ICardMetadataRepository,
    @inject(ISession$) session: ISession,
    @inject(IFormService$) formService: IFormService,
    @inject(IFormBuilder$) formBuilder: IFormBuilder
  ) {
    super(cardMetadataRepository, session, formService, formBuilder);
  }

  //#endregion

  //#region base overrides

  protected prepareDialogCommand(
    context: IClientCommandHandlerContext
  ): CardTaskCompletionOptionSettings | null {
    const parameters = context.command.parameters;
    if (!parameters.ProcessInstance) {
      return null;
    }
    const coSettingsObj = parameters.CompletionOptionSettings as IStorage;
    if (!coSettingsObj) {
      return null;
    }
    return new CardTaskCompletionOptionSettings(coSettingsObj);
  }

  protected async completeDialogCore(
    actionResult: CardTaskDialogActionResult,
    context: IClientCommandHandlerContext,
    _cardEditor: ICardEditorModel,
    parentCardEditor: ICardEditorModel | null
  ): Promise<boolean> {
    const parameters = context.command.parameters;
    const instanceStorage = StorageHelper.tryGet<IStorage>(parameters, 'ProcessInstance');
    if (!instanceStorage) {
      return true;
    }

    const processInstance = new KrProcessInstance(instanceStorage);
    const requestInfo: IStorage = {};

    if (parentCardEditor && parentCardEditor.cardModel) {
      const card = parentCardEditor.cardModel.card;
      card.info[StorageHelper.systemKeyPrefix + 'CardTaskDialogActionResult'] =
        actionResult.getStorage();
    } else {
      requestInfo[StorageHelper.systemKeyPrefix + 'CardTaskDialogActionResult'] =
        actionResult.getStorage();
    }

    requestInfo[StorageHelper.systemKeyPrefix + 'WebAdvancedDialogCommandSkipUIContextFlag'] =
      TypedField.createBoolean(true);

    const result = await launchProcess(processInstance, {
      cardEditor: parentCardEditor!,
      requestInfo: requestInfo
    });
    if (!result) {
      return false;
    }

    // Сообщения валидации будут выведены при запуске процесса.

    return (
      result.validationResult.isSuccessful &&
      !StorageHelper.tryGet(
        result.cardResponse.info,
        `${StorageHelper.systemKeyPrefix}KeepTaskDialog`
      )
    );
  }

  //#endregion
}
