import { extension, localize } from '@tessa/application';
import {
  ValidationResult,
  ValidationResultType,
  Guid,
  Primitive,
  StorageHelper,
  TypedField
} from '@tessa/core';
import { ICardMetadataSection, CardHelper, CardRow, CardRowState } from '@tessa/platform';
import userSession from 'common/utility/userSession';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { createCard, openCard } from 'tessa/ui/uiHost';
import { UIContext } from 'tessa/ui/uiContext';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { FieldMapStorage } from 'tessa/cards/fieldMapStorage';
import { CardUIExtension } from 'tessa/ui/cards/cardUIExtension';
import { CardToolbarAction } from 'tessa/ui/cards/cardToolbarAction';
import { ICardToolbarViewModel } from 'tessa/ui/cards/cardToolbarViewModel';
import { CardToolbarActionGroup } from 'tessa/ui/cards/cardToolbarActionGroup';
import { ICardUIExtensionContext } from 'tessa/ui/cards/cardUIExtensionContext';
import { CardSavingMode, CardSavingRequest } from 'tessa/ui/cards/cardSavingRequest';
import { FileListViewModel } from 'tessa/ui/cards/controls/fileList/fileListViewModel';
import { OcrGridDataConverter } from '../components/grid/ocrGridTypes';
import {
  OcrKey,
  MultipageFileExtensions,
  OcrSourceEditorKey,
  OcrDeleteOperationIdKey,
  OcrOperationTypeId
} from '../misc/ocrConstants';
import {
  ocrCardRequestRowFactory,
  ocrCardRequestDialogFactory,
  ocrCardLanguagesRowsFactory
} from '../misc/ocrCardRequestDialogFactory';
import { OcrRequestStates } from '../misc/ocrTypes';
import { OcrHelper } from '../misc/ocrHelper';

/**
 * Расширение, выполняющее модификацию панели инструментов в карточке операции OCR.
 * @remarks Детальное описание расширения:
 * 1. Удаляется кнопка "Сохранить и закрыть".
 * 2. Добавляется кнопка "Сохранить результат OCR", которая переносит результат OCR в исходную карточку.
 *    Если действие выполняется вне UI-контекста исходной карточки, то редактор исходной карточки
 *    будет отображён на новой вкладке с перенесённым результатом OCR.
 * 3. Добавление кнопки "Экспертный режим" в группу "Другие".
 * 4. Добавление кнопки "Повторить распознавание". Если экспертный режим отключён, то будет создан запрос
 *    на распознавание файла с параметрами по умолчанию. Иначе - будет отображён диалог с настройками запроса
 *    на распознавание файла. При открытии карточки диалог будет также автоматически отображён, если отсутствует
 *    успешный запрос на распознавание файла в таблице со всеми запросами.
 */
@extension({ name: 'OcrToolbarUIExtension' })
export class OcrToolbarUIExtension extends CardUIExtension {
  //#region base overrides

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return Guid.equals(context.card.typeId, OcrOperationTypeId);
  }

  initialized(context: ICardUIExtensionContext): void {
    const { editor } = OcrHelper.getContextEditorModel(context);
    const toolbar = editor?.toolbar;
    if (toolbar) {
      const expertMode = OcrHelper.getExpertMode(editor.info);
      const hasCompleted = OcrHelper.checkRequestsStates(context.card, OcrRequestStates.Completed);
      this.initializeSaveAndCloseAction(toolbar);
      this.initializeOcrProcessRunAction(toolbar, expertMode || !hasCompleted);
      this.initializeOcrResultSaveAction(toolbar);
    }
  }

  contextInitialized(context: ICardUIExtensionContext): void {
    const { editor } = OcrHelper.getContextEditorModel(context);
    if (editor) {
      const expertMode = OcrHelper.getExpertMode(editor.info);
      if (editor.toolbar) {
        this.initializeOcrExpertModeAction(editor.toolbar, !expertMode);
      }
      if (expertMode) {
        const tittle = `${editor.localizedWorkspaceName} (${localize('$UI_Common_OcrExpertMode')})`;
        editor.changeWorkspace(tittle, editor.workspaceInfo);
      }
    }

    if (
      StorageHelper.tryGet<boolean>(context.card.info, OcrKey) &&
      !OcrHelper.checkRequestsStates(
        context.card,
        OcrRequestStates.Created,
        OcrRequestStates.Active,
        OcrRequestStates.Completed
      )
    ) {
      // Если в карточке нет ни одного успешного запроса, то отображаем диалог создания нового запроса
      this.ocrProcessRunCommand();
    }
  }

  //#endregion

  //#region toolbar actions

  private initializeSaveAndCloseAction(toolbar: ICardToolbarViewModel): void {
    toolbar.removeItemIfExists('SaveAndCloseCard');
  }

  private initializeOcrProcessRunAction(toolbar: ICardToolbarViewModel, visibility: boolean): void {
    if (visibility) {
      if (!toolbar.items.some(item => item.name === 'OcrProcessRun')) {
        toolbar.addItem(
          new CardToolbarAction({
            order: 1,
            name: 'OcrProcessRun',
            icon: getTessaIcon('Thin76'),
            caption: '$UI_ToolbarButtons_OcrProcessRun',
            toolTip: '$UI_ToolbarButtons_OcrProcessRun_Tooltip',
            command: this.ocrProcessRunCommand
          }),
          {
            name: 'Ctrl+Shift+R',
            key: 'KeyR',
            modifiers: { ctrl: true, shift: true }
          }
        );
      }
    } else {
      toolbar.removeItemIfExists('OcrProcessRun');
    }
  }

  private initializeOcrResultSaveAction(toolbar: ICardToolbarViewModel): void {
    if (!toolbar.items.some(item => item.name === 'OcrResultSave')) {
      toolbar.addItem(
        new CardToolbarAction({
          order: 2,
          name: 'OcrResultSave',
          icon: getTessaIcon('Thin418'),
          caption: '$UI_ToolbarButtons_OcrResultSave',
          toolTip: '$UI_ToolbarButtons_OcrResultSave_Tooltip',
          command: this.ocrResultSaveCommand
        }),
        {
          name: 'Ctrl+Shift+S',
          key: 'KeyS',
          modifiers: { ctrl: true, shift: true }
        }
      );
    }
  }

  private initializeOcrExpertModeAction(toolbar: ICardToolbarViewModel, visibility: boolean): void {
    const otherGroup = toolbar.items.find(x => x.name === 'CardOthers') as CardToolbarActionGroup;
    if (otherGroup) {
      if (visibility) {
        if (!otherGroup.actions.some(action => action.name === 'OcrExpertMode')) {
          otherGroup.addAction(
            new CardToolbarAction({
              order: 1,
              name: 'OcrExpertMode',
              icon: getTessaIcon('Thin52'),
              caption: '$UI_ToolbarButtons_OcrExpertMode',
              toolTip: '$UI_ToolbarButtons_OcrExpertMode_Tooltip',
              command: this.ocrExpertModeCommand
            }),
            {
              name: 'Ctrl+Shift+E',
              key: 'KeyE',
              modifiers: { ctrl: true, shift: true }
            }
          );
        }
      } else {
        otherGroup.removeActionIfExists('OcrExpertMode');
      }
    }
  }

  //#endregion

  //#region toolbar commands

  private ocrProcessRunCommand = async () => {
    const { uiContext, editor, model } = OcrHelper.getContextEditorModel();
    if (!editor || !model) {
      return;
    }
    // Отображаем диалог создания нового запроса на распознавание
    const fileName = model.card.sections.get('OcrOperations').fields.get<string>('FileName')!;
    const dotIndex = fileName.lastIndexOf('.');
    const fileExtension = dotIndex !== -1 ? fileName.substring(dotIndex + 1) : '';
    const isMultipage = MultipageFileExtensions.includes(fileExtension);
    const expertMode = OcrHelper.getExpertMode(editor.info);
    const result = await ocrCardRequestDialogFactory(isMultipage, !expertMode);
    if (result) {
      // Создаем запрос на распознавание в карточке OCR
      const requests = model.card.sections.get('OcrRequests').rows;
      const languages = model.card.sections.get('OcrRequestsLanguages').rows;
      const requestRow = requests.add(ocrCardRequestRowFactory(result.request));
      languages.push(...ocrCardLanguagesRowsFactory(result.languages, requestRow.rowId));
      // Сохраняем карточку OCR
      if (!(await editor.saveCard(uiContext))) {
        CardHelper.clearStorageRows(requests, row => row.state === CardRowState.Inserted);
        CardHelper.clearStorageRows(languages, row => row.state === CardRowState.Inserted);
      }
    }
  };

  private ocrResultSaveCommand = async () => {
    const { uiContext } = OcrHelper.getContextEditorModel();
    if (!uiContext.cardEditor?.cardModel) {
      return;
    }

    // Сохранение карточки OCR с последующим её обновлением
    if (await uiContext.cardEditor.cardModel.hasChanges()) {
      const savingRequest = new CardSavingRequest(CardSavingMode.RefreshOnSuccess);
      if (!(await uiContext.cardEditor.saveCard(uiContext, undefined, savingRequest))) {
        return;
      }
    }

    const { editor, model } = OcrHelper.getContextEditorModel();
    if (!editor || !model) {
      return;
    }

    // Выбор текущего файла, установленного в предпросмотре (вкладка "Верификация")
    const requests = model.card.sections.get('OcrRequests')!.rows;
    const fileId = OcrToolbarUIExtension.getRecognizedFileId(requests);
    if (!fileId) {
      await showNotEmpty(
        ValidationResult.fromText('$UI_Controls_Preview_FileNotLoaded', ValidationResultType.Error)
      );
      return;
    }

    // Получение редактора исходной карточки
    let sourceEditor = OcrHelper.getSourceEditor(uiContext.info);
    const isNewSourceEditor = StorageHelper.tryGet<boolean>(sourceEditor?.info, OcrSourceEditorKey);
    const sourceUIContextScope = isNewSourceEditor ? UIContext.create(sourceEditor!.context) : null;
    try {
      const ocrOperations = model.card.sections.get('OcrOperations').fields;
      const sourceCardId = ocrOperations.getString('CardID');

      if (!sourceEditor) {
        sourceEditor = await createCard({
          cardId: sourceCardId ?? undefined,
          cardTypeId: ocrOperations.getString('CardTypeID')!,
          cardTypeName: ocrOperations.getString('CardTypeName')!,
          info: {
            docTypeID: ocrOperations.getField('DocTypeID'),
            docTypeTitle: ocrOperations.getField('DocTypeTitle')
          }
        });
      } else if (isNewSourceEditor) {
        const sourceCard = sourceEditor.cardModel!.card;
        sourceEditor = await openCard({
          cardId: sourceCard.id,
          cardTypeId: sourceCard.typeId,
          cardTypeName: sourceCard.typeName
        });
      }

      const sourceModel = sourceEditor?.cardModel;
      if (!sourceModel) {
        return;
      }

      // Поиск распознанного файла в файловом контейнере карточки операции OCR
      const file = model.fileContainer.files.find(file => Guid.equals(file.id, fileId));
      if (!file) {
        await showNotEmpty(
          ValidationResult.fromText(
            localize('$UI_Common_FileNotFound', fileId),
            ValidationResultType.Error
          )
        );
        return;
      }

      // Загрузка контента распознанного файла
      const validationResult = await file.ensureContentDownloaded();
      if (await showNotEmpty(validationResult)) {
        return;
      }

      // Поиск в исходной карточке файла, проассоциированный с карточкой операции OCR
      if (sourceCardId) {
        const sourceFileId = ocrOperations.getString('FileID');
        const sourceFile = sourceModel.fileContainer.files.find(f =>
          Guid.equals(f.id, sourceFileId)
        );
        const sourceCardFile = sourceModel.card.files.find(f => Guid.equals(f.rowId, sourceFileId));
        if (!sourceFile || !sourceCardFile) {
          await showNotEmpty(
            ValidationResult.fromText(
              localize('$UI_Common_FileNotFound', sourceFileId),
              ValidationResultType.Error
            )
          );
          return;
        }

        // Удаление признака и тэга OCR из исходного файла в исходной карточке
        delete sourceFile.options[OcrKey];
        sourceFile.source.notifyOptionsModified(sourceFile);
        for (const control of sourceModel.controlsBag) {
          if (control instanceof FileListViewModel) {
            const fileListViewModel = control as FileListViewModel;
            const fileViewModel = fileListViewModel.files.find(f =>
              Guid.equals(f.id, sourceFileId)
            );
            if (fileViewModel) {
              // сбрасываем тэг OCR с модели представления файла
              fileViewModel.tag = null;
            }
          }
        }

        // Выполнение замены исходного файла распознанным файлом
        sourceFile.replace(file.lastVersion.content!, true);
        // Добавление идентификатора карточки операции OCR (для ее удаления) в info файла исходной карточки
        sourceCardFile.info[OcrDeleteOperationIdKey] = TypedField.createGuid(model.card.id);
      } else {
        // Загрузка контента распознанного файла
        const validationResult = await file.ensureContentDownloaded();
        if (await showNotEmpty(validationResult)) {
          return;
        }

        // Создание нового файла на основе распознанного контента
        const newFile = sourceModel.fileContainer.createFile(
          file.lastVersion.content!,
          file.type,
          file.category,
          file.name,
          userSession
        );

        // Добавление нового файла в карточку
        await sourceModel.fileContainer.addFile(newFile);
        // Поиск карточки нового файла и добавление идентификатора карточки операции OCR (для ее удаления) в info файла исходной карточки
        const newCardFile = sourceModel.card.files.find(f => Guid.equals(f.rowId, newFile.id));
        newCardFile!.info[OcrDeleteOperationIdKey] = TypedField.createGuid(model.card.id);
      }

      // Попытка получения секций исходной карточки и переноса в них значений из карточки операции OCR
      const sections = sourceModel.card.tryGetSections();
      if (sections) {
        const fieldsCache = new Map<string, FieldMapStorage | null | undefined>();
        const sectionsMetadataCache = new Map<string, ICardMetadataSection | null>();
        const ocrMappingStorage = OcrGridDataConverter.deserializeFromCard(model.card);

        for (const [ocrAlias, ocrData] of Object.entries(ocrMappingStorage)) {
          const [sectionName, fieldName] = ocrAlias.split('.');

          let fields: FieldMapStorage | null | undefined;
          if (!fieldsCache.has(sectionName)) {
            fields = sections.tryGet(sectionName)?.tryGetFields();
            fieldsCache.set(sectionName, fields);
          } else {
            fields = fieldsCache.get(sectionName);
          }

          if (!fields || fields.size <= 0) {
            continue;
          }

          const copyField = (fieldName: string, value: Primitive | null) => {
            if (!fields?.has(fieldName)) {
              return;
            }

            let fieldType = TypedField.tryGetType(fields.tryGetField(fieldName));

            if (!fieldType) {
              let sectionMetadata: ICardMetadataSection | null | undefined;
              if (!sectionsMetadataCache.has(sectionName)) {
                sectionMetadata = sourceModel.cardMetadata.sections.getSectionByName(sectionName);
                sectionsMetadataCache.set(sectionName, sectionMetadata);
              } else {
                sectionMetadata = sectionsMetadataCache.get(sectionName);
              }

              const columnMetadata = sectionMetadata?.columns.getColumnByName(fieldName);
              fieldType = columnMetadata?.metadataType?.fieldType ?? null;
              if (!fieldType) {
                return;
              }
            }

            fields.set(fieldName, value, fieldType);
          };

          if (ocrData.value === null || StorageHelper.isPrimitiveType(ocrData.value)) {
            copyField(fieldName, ocrData.value);
          } else {
            for (const [key, value] of Object.entries(ocrData.value)) {
              copyField(key, value as Primitive | null);
            }
          }
        }
      }

      // Закрытие редактора карточки операции OCR
      await editor.close();
    } finally {
      sourceUIContextScope?.dispose();
    }
  };

  private ocrExpertModeCommand = async () => {
    const { uiContext, editor } = OcrHelper.getContextEditorModel();
    if (editor) {
      OcrHelper.setExpertMode(editor.info, true);
      await editor.saveCard(uiContext);
    }
  };

  //#endregion

  //#region helpers

  private static getRecognizedFileId(storage: CardRow[]): string | null | undefined {
    return (
      // поиск файла, отмеченного, как основной
      storage.find(r => r.get('IsMain'))?.get('ContentFileID') ??
      // поиск последнего успешно распознанного файла (сортировка выполняется в расширении на тип карточки)
      storage.find(r => r.get('StateID') === OcrRequestStates.Completed)?.get('ContentFileID')
    );
  }

  //#endregion
}
