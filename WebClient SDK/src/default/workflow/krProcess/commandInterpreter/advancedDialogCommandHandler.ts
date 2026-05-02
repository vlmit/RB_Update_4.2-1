import {
  CardTaskCompletionOptionSettings,
  CardTaskDialogActionResult,
  systemKeyPrefix,
  Card,
  CardTaskDialogNewMethod
} from 'tessa/cards';
import {
  showNotEmpty,
  UIContext,
  createCardEditorModel,
  IUIContext,
  createDialogForm,
  createCardModelWithMetadata,
  createCardFileSource,
  createCardFileContainer
} from 'tessa/ui';
import {
  AdvancedCardDialogManager,
  CardEditorCreationContext,
  CardEditorModel,
  ICardEditorModel
} from 'tessa/ui/cards';
import { Guid, TypedField } from 'tessa/platform';
import { IStorage } from 'tessa/platform/storage';
import { FileContainerPermissions, IFile, setFileContentToInfo } from 'tessa/files';
import { CardRequestExtensionContext } from 'tessa/cards/extensions';
import { ValidationResult, ValidationResultBuilder } from 'tessa/platform/validation';
import { ArgumentOutOfRangeError } from 'tessa/platform/errors';
import { CreateCardArg, ShowCardArg } from 'tessa/ui/uiHost/common';
import {
  CardFileVersion,
  CardHelper,
  CardNewRequest,
  CardNewStrategy,
  CardNewStrategyContext,
  CardRow,
  ClientCommandHandlerBase,
  ICardMetadataRepository,
  IClientCommandHandlerContext
} from '@tessa/platform';
import { FieldType, StorageHelper } from '@tessa/core';
import { IFormBuilder, IFormService } from 'tessa/ui/formEditor/types';
import { ISession } from '@tessa/application';

/**
 * Базовый класс обработчика клиентской команды отображения диалога.
 */
export abstract class AdvancedDialogCommandHandler extends ClientCommandHandlerBase {
  //#region ctor

  constructor(
    private readonly _cardMetadataRepository: ICardMetadataRepository,
    private readonly _session: ISession,
    private readonly _formService: IFormService,
    private readonly _formBuilder: IFormBuilder
  ) {
    super();
  }

  //#endregion

  //#region methods

  async handle(context: IClientCommandHandlerContext): Promise<void> {
    const coSettings = this.prepareDialogCommand(context);
    if (!coSettings) {
      return;
    }

    // в ТК при вызове диалога через тайл или при нажатии на кнопку в тулбаре UIContext не сохраняется.
    // в ЛК UIContext, который создается в showGlobalDialogAsync, сохраняется и доступен в расширениях,
    // поэтому поведение в этой части расширения может отличаться.
    // Для того чтобы обойти эту проблему в WeAdvancedDialogCommandHandler и KrAdvancedDialogCommandHandler прокидывается флажок
    // Когда этот флажок есть в CardRequestExtensionContext (и только для этого типа реквестов), то мы пропускаем проверку UIContext.
    let skipEditor = false;
    if (context.outerContext && context.outerContext instanceof CardRequestExtensionContext) {
      skipEditor =
        StorageHelper.tryGet(
          context.outerContext.request.info,
          systemKeyPrefix + 'WebAdvancedDialogCommandSkipUIContextFlag'
        ) ?? false;
    }

    const editor = UIContext.current.cardEditor;
    if (editor && !skipEditor) {
      this.setDialogNonTaskCompletionOptionSettings(editor, editor, coSettings, context);
    } else {
      setTimeout(() => this.showGlobalDialog(coSettings, context));
    }
  }

  //#endregion

  //#region protected methods

  /**
   * Метод для подготовки диалога для выполнения.
   *
   * @param context Контекст обработки клиентской команды.
   * @returns Информация для формирования диалога, или значение `null`, если невозможно сформировать диалог.
   */
  protected abstract prepareDialogCommand(
    context: IClientCommandHandlerContext
  ): CardTaskCompletionOptionSettings | null;

  /**
   * Метод выполнения диалога.
   *
   * @param actionResult Результат выполнения диалога.
   * @param context Контекст команды диалога.
   * @param cardEditor Редактор карточки диалога.
   * @param parentCardEditor Редактор карточки, для которой открывается диалог, если диалог открывается в рамках карточки.
   * @returns Значение `true`, если необходимо закрыть диалог, иначе `false`.
   */
  protected abstract completeDialogCore(
    actionResult: CardTaskDialogActionResult,
    context: IClientCommandHandlerContext,
    cardEditor: ICardEditorModel,
    parentCardEditor: ICardEditorModel | null
  ): Promise<boolean>;

  //#endregion

  //#region private methods

  /**
   * Отображает карточку в окне диалога.
   *
   * @param coSettings Параметры диалога.
   * @param context Контекст обработки клиентской команды.
   */
  private async showGlobalDialog(
    coSettings: CardTaskCompletionOptionSettings,
    context: IClientCommandHandlerContext
  ): Promise<void> {
    const info = coSettings.info;
    if (coSettings.preparedNewCard && coSettings.preparedNewCardSignature) {
      info[systemKeyPrefix + 'NewBilletCard'] = TypedField.create(
        coSettings.preparedNewCard,
        FieldType.Binary
      );
      info[systemKeyPrefix + 'NewBilletCardSignature'] = TypedField.create(
        coSettings.preparedNewCardSignature,
        FieldType.Binary
      );
    }
    info[systemKeyPrefix + 'StoreMode'] = TypedField.createInt(coSettings.storeMode);

    const contextInstance = UIContext.create(
      new UIContext({
        cardEditor: createCardEditorModel(),
        actionOverridings: AdvancedCardDialogManager.instance.createUIContextActionOverridings()
      })
    );
    try {
      await this.createNewCard(coSettings, info, ctx => {
        this.setDialogNonTaskCompletionOptionSettings(ctx.cardEditor!, null, coSettings, context);
        ctx.cardEditor!.info[systemKeyPrefix + 'OnlyConfigureDialog'] = TypedField.trueBoolean;
      });
    } finally {
      contextInstance.dispose();
    }
  }

  private async completeDialog(
    dialogCardEditor: ICardEditorModel,
    parentCardEditor: ICardEditorModel | null,
    coSettings: CardTaskCompletionOptionSettings,
    buttonName: string | null,
    completeDialog: boolean,
    context: IClientCommandHandlerContext
  ) {
    let closeDialog = false;

    await dialogCardEditor.setOperationInProgress(async () => {
      const dialogCard = dialogCardEditor.cardModel!.card.clone();
      const formData: IStorage | null = dialogCardEditor.useFormEditor
        ? StorageHelper.tryClone(dialogCardEditor.cardModel!.wysiwygForm?.rootDataProvider?.data)
        : null;

      const validationResult = await AdvancedDialogCommandHandler.prepareFilesForStore(
        dialogCard,
        dialogCardEditor.cardModel!.fileContainer.files
      );

      await showNotEmpty(validationResult);
      if (validationResult.hasErrors) {
        return;
      }

      const actionResult = new CardTaskDialogActionResult();
      actionResult.mainCardId = Guid.empty;
      actionResult.pressedButtonName = buttonName;
      actionResult.storeMode = coSettings.storeMode;
      actionResult.keepFiles = coSettings.keepFiles;
      actionResult.completeDialog = completeDialog;
      actionResult.setDialogCard(dialogCard);
      if (formData) {
        actionResult.formData = formData;
      }

      closeDialog = await this.completeDialogCore(
        actionResult,
        context,
        dialogCardEditor,
        parentCardEditor
      );
    });

    if (closeDialog) {
      await dialogCardEditor.close();
    }
  }

  /**
   * Задаёт контент указанных файлов в соответствующие `CardFile.info` карточки файлов.
   *
   * @param dialogCard Карточка диалога.
   * @param files Коллекция файлов.
   * @returns Результат выполнения операции, агрегированный для всех файлов.
   */
  private static async prepareFilesForStore(
    dialogCard: Card,
    files: Readonly<IFile[]>
  ): Promise<ValidationResult> {
    const validationResult = new ValidationResultBuilder();
    for (const file of files) {
      validationResult.add(await file.ensureContentModified());
    }

    if (validationResult.isSuccessful) {
      await setFileContentToInfo(dialogCard, files, validationResult);
    }
    return validationResult.build();
  }

  /**
   * Создаёт и открывает карточку в диалоге. Карточка создаётся в режиме по умолчанию или по шаблону.
   */
  private async createNewCard(
    dialogSettings: CardTaskCompletionOptionSettings,
    info: IStorage,
    cardModifierAction: (context: IUIContext) => void
  ): Promise<void> {
    const cardNewMethod = dialogSettings.cardNewMethod;

    let dialogDisplayValue = dialogSettings.displayValue;
    if (
      cardNewMethod === CardTaskDialogNewMethod.CardType &&
      (!dialogDisplayValue || dialogDisplayValue === '')
    ) {
      const cardMetadata = await this._cardMetadataRepository.getCardMetadata();
      const dialogType = cardMetadata.cardTypes.getCardTypeById(dialogSettings.dialogTypeId);
      if (dialogType) {
        dialogDisplayValue = dialogType.caption;
      }
    }

    const args: CreateCardArg = {
      cardTypeId: dialogSettings.dialogTypeId,
      info: info,
      displayValue: dialogDisplayValue!,
      context: UIContext.current,
      cardModifierAction: (ctx: CardEditorCreationContext) => {
        ctx.editor.dialogName = dialogSettings.dialogName;

        cardModifierAction?.(ctx.uiContext);
      },
      dialogOptions: {
        dialogName: dialogSettings.dialogName!,
        showOnlyFirstTab: dialogSettings.notDisplayTabs,
        dialogAutoSizeHeight: dialogSettings.autoSizeHeight,
        dialogAutoSizeWidth: dialogSettings.autoSizeWidth
      },
      saveCreationRequest: false
    };

    switch (cardNewMethod) {
      case CardTaskDialogNewMethod.CardType:
        await AdvancedCardDialogManager.instance.createCard(args);
        break;

      case CardTaskDialogNewMethod.Template:
        await AdvancedCardDialogManager.instance.createFromTemplate(
          dialogSettings.dialogTypeId,
          args
        );
        break;

      case CardTaskDialogNewMethod.DialogType:
        await this.showDialogCard(
          dialogSettings,
          cardModifierAction,
          args,
          dialogSettings.preparedDialogCard
        );
        break;

      case CardTaskDialogNewMethod.Form:
        await this.showForm(dialogSettings, cardModifierAction, args);
        break;

      default:
        throw new ArgumentOutOfRangeError(
          'CardTaskCompletionOptionSettings.cardNewMethod',
          cardNewMethod
        );
    }
  }

  private setDialogNonTaskCompletionOptionSettings(
    currentCardEditor: ICardEditorModel,
    parentCardEditor: ICardEditorModel | null,
    coSettings: CardTaskCompletionOptionSettings,
    context: IClientCommandHandlerContext
  ): void {
    currentCardEditor.info[systemKeyPrefix + 'CardEditorCompletionOptionSettings'] = coSettings;
    currentCardEditor.info[systemKeyPrefix + 'CardEditorCompletionOptionSettingsOnButtonPressed'] =
      async (
        dialogCardEditor: ICardEditorModel,
        cos: CardTaskCompletionOptionSettings,
        buttonName: string | null,
        completeTask: boolean
      ) =>
        await this.completeDialog(
          dialogCardEditor,
          parentCardEditor,
          cos,
          buttonName,
          completeTask,
          context
        );
  }

  private async showDialogCard(
    dialogSettings: CardTaskCompletionOptionSettings,
    cardModifierAction: (uiContext: IUIContext) => void,
    args: Omit<ShowCardArg, 'editor'>,
    dialogCard?: Card | null
  ): Promise<void> {
    const cardMetadata = await this._cardMetadataRepository.getCardMetadata();
    const cardType = cardMetadata.cardTypes.find(x => x.id === dialogSettings.dialogTypeId);
    if (!cardType) {
      throw new Error(
        `Card type with specified ID '${dialogSettings.dialogTypeId}' is not found in metadata.`
      );
    }
    const dialogResult = await createDialogForm(
      cardType.name,
      undefined,
      undefined,
      undefined,
      dialogCard
        ? async response => {
            AdvancedDialogCommandHandler.restoreFileVersions(dialogCard);
            response.card = dialogCard;
          }
        : undefined,
      undefined,
      {
        createFileContainer: true
      }
    );
    if (!dialogResult) {
      return;
    }

    const [form, model] = dialogResult;
    model.mainForm = form;

    const cardEditor = new CardEditorModel(
      async () => model,
      async () => form,
      () => model.fileContainer.source,
      () => model.fileContainer
    );
    cardEditor.withUIExtensions = true;
    cardEditor.dialogName = dialogSettings.dialogName;
    cardModifierAction(cardEditor.context);

    if (
      !(await cardEditor.createAndInitializeModel({
        card: model.card,
        sectionRows: model.sectionRows as Map<string, CardRow>
      }))
    ) {
      return;
    }

    await cardEditor.setCardModel(model);
    await AdvancedCardDialogManager.instance.showCard({
      editor: cardEditor,
      ...args
    });
  }

  /**
   * Восстанавливает версии файлов из `CardFile.info`.
   *
   * @param dialogCard Обрабатываемая карточка.
   */
  private static restoreFileVersions(dialogCard: Card): void {
    const files = dialogCard.tryGetFiles();
    if (!(files?.length === 0)) {
      return;
    }

    for (const file of files) {
      const versionStorage = StorageHelper.tryGet<IStorage>(
        file.info,
        systemKeyPrefix + 'FileVersion'
      );

      if (versionStorage) {
        const version = new CardFileVersion(versionStorage);
        file.versions.clear();
        file.versions.push(version);
      }
    }
  }

  private async showForm(
    dialogSettings: CardTaskCompletionOptionSettings,
    cardModifierAction: (uiContext: IUIContext) => void,
    args: Omit<ShowCardArg, 'editor'>,
    dialogCard?: Card
  ): Promise<void> {
    const editorForm = await this._formService.load(dialogSettings.dialogTypeId);
    if (!editorForm) {
      return;
    }

    const cardTypeId = editorForm.cardTypeId ?? '9de1ffde-031f-4a6f-a983-05bd24316933';
    const cardMetadata = await this._cardMetadataRepository.getCardMetadata();
    const cardTypeMetadata = await cardMetadata.getMetadataForType(cardTypeId);
    if (!cardTypeMetadata) {
      return;
    }

    let card: Card | undefined = dialogCard;
    if (!card) {
      const cardNewRequest = new CardNewRequest();
      cardNewRequest.cardTypeId = cardTypeId;

      const cardNewResponse = await CardNewStrategy.default.createResponse(
        new CardNewStrategyContext(
          cardNewRequest.cardTypeId,
          cardNewRequest.newMode,
          cardTypeMetadata,
          cardMetadata
        )
      );
      await CardNewStrategy.default.setUserInfo(cardNewResponse, this._session.user);
      CardHelper.grantAllPermissions(cardNewResponse.card);
      card = cardNewResponse.card;
    } else {
      AdvancedDialogCommandHandler.restoreFileVersions(card);
    }

    const model = await createCardModelWithMetadata(card, new Map(), cardTypeMetadata);
    const fileSource = createCardFileSource(model);
    const permissions = new FileContainerPermissions();
    permissions.canAdd = true;
    model.fileContainer = createCardFileContainer(fileSource, permissions);
    await model.fileContainer.initialize();

    const form = await this._formBuilder.build({
      form: editorForm,
      cardModel: model
    });

    if (!form) {
      return;
    }

    if (form.rootDataProvider) {
      form.rootDataProvider.mergeData(dialogSettings.formData);
    }

    const cardEditor = new CardEditorModel(
      async () => model,
      async () => form,
      () => model.fileContainer.source,
      () => model.fileContainer
    );
    cardEditor.withUIExtensions = true;
    cardEditor.dialogName = dialogSettings.dialogName;
    cardModifierAction(cardEditor.context);

    if (
      !(await cardEditor.createAndInitializeModel({
        card: model.card,
        sectionRows: model.sectionRows as Map<string, CardRow>
      }))
    ) {
      return;
    }

    await cardEditor.setCardModel(model);
    await AdvancedCardDialogManager.instance.showCard({
      editor: cardEditor,
      ...args
    });
  }

  //#endregion
}
