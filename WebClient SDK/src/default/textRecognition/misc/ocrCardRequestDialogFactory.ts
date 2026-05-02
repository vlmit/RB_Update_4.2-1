import {
  ValidationKey,
  ValidationResultBuilder,
  ValidationResultType,
  FieldType,
  Guid
} from '@tessa/core';
import { CardRow, CardRowState } from '@tessa/platform';
import { localize } from '@tessa/application';
import userSession from 'common/utility/userSession';
import { FieldMapStorage } from 'tessa/cards/fieldMapStorage';
import { Visibility } from 'tessa/platform/visibility';
import { ArrayStorage } from 'tessa/platform/storage/arrayStorage';
import { showDialog } from 'tessa/ui/uiHost/showFormDialog';
import { CustomStyle } from 'tessa/ui/cards/customElementStyle';
import { createDialogForm, FormCreationOptions, showNotEmpty, UIButton } from 'tessa/ui';
import { OcrRequestDialogTypeName } from './ocrConstants';
import { OcrRequestStates } from './ocrTypes';

/**
 * Выполняет создание и отображение диалога "Создание запроса на распознавание".
 * @param isMultipage Признак многостраничного файла.
 * @param quiet Признак создания диалога со значениями по умолчанию без его отображения.
 * @returns Информация по запросу, заполненная пользователем - если создание выполнено успешно, в противном случае - null.
 */
export async function ocrCardRequestDialogFactory(
  isMultipage: boolean,
  quiet = true
): Promise<{
  request: FieldMapStorage;
  languages: ArrayStorage<CardRow>;
} | null> {
  // Создаем форму диалогового окна для создания запроса на распознавание файла
  const createDialogResult = await createDialogForm(
    OcrRequestDialogTypeName,
    OcrRequestDialogTypeName,
    FormCreationOptions.None
  );

  // Если по каким-то причинам создать форму не удалось, то выходим
  if (!createDialogResult) {
    return null;
  }

  // Получаем форму и модель диалогового окна и выполняем инициализацию параметров
  const [form, cardModel] = createDialogResult;
  const languagesCtrl = cardModel.controls.get('Languages');
  const request = cardModel.card.sections.tryGet('OcrRequest')?.fields;
  const requestLanguages = cardModel.card.sections.tryGet('OcrRequestLanguages')?.rows;
  if (!languagesCtrl || !request || !requestLanguages) {
    return null;
  }

  const createDetectableLanguage = () => {
    const language = requestLanguages.add();
    language.set('LanguageID', -1, FieldType.Int);
    language.set('LanguageISO', null);
    language.set('LanguageCaption', 'Auto', FieldType.String);
    language.rowId = Guid.newGuid();
    language.state = CardRowState.Inserted;
  };

  // Выполняем необходимые настройки и модификации в диалоговом окне перед отображением
  languagesCtrl.isReadOnly = !!request.get('DetectLanguages');
  if (languagesCtrl.isReadOnly) {
    createDetectableLanguage();
  }

  const fieldChangedDisposer = request.fieldChanged.add(e => {
    if (e.fieldName === 'DetectLanguages') {
      languagesCtrl.isReadOnly = !languagesCtrl.isReadOnly;
      requestLanguages.clear();
      if (e.fieldValue) {
        createDetectableLanguage();
      }
    }
  });

  const textLayerBlock = cardModel.blocks.get('TextLayerSettingsBlock');
  if (textLayerBlock) {
    // Скрытие метки и признака перезаписи многостраничного файла
    textLayerBlock.blockVisibility = isMultipage ? Visibility.Visible : Visibility.Collapsed;
    // Установка стиля для блока с настройками текстового слоя
    textLayerBlock.customStyle = CustomStyle.createCustomBlockStyle({
      mainBackgroundColor: 'var(--ocr-request-layer-background)',
      controlsPadding: '10px',
      mainBorderRadius: true
    });
  }

  // Показываем форму диалогового окна с вариантами "Подтвердить" и "Отмена"
  const dialogResult =
    quiet ||
    (await showDialog<boolean>(
      form,
      null,
      [
        UIButton.create({
          caption: '$UI_Common_OK',
          theme: 'primary',
          type: 'normal',
          buttonAction: async button => {
            const validationResult = new ValidationResultBuilder();

            // Вспомогательная функция для добавления ошибки валидации и подсветки контрола
            const addError = (controlName: string, message: string) => {
              validationResult.add(ValidationKey.unknown, ValidationResultType.Error, message);
              const control = cardModel.controls.get(controlName);
              if (control) {
                control.hasActiveValidation = true;
              }
            };

            // Выполняем валидацию полей, заполненных пользователем
            if (request?.get('SegmentationModeID') == null) {
              addError('SegmentationMode', '$CardTypes_Validators_ImagePageSegmentationMode');
            }
            if (!requestLanguages?.some(l => l.state !== CardRowState.Deleted)) {
              addError('Languages', '$CardTypes_Validators_Languages');
            }

            // Если все данные введены корректно, то выходим из диалога
            if (!(await showNotEmpty(validationResult.build()))) {
              fieldChangedDisposer?.();
              button.close(true);
            }
          }
        }),
        UIButton.create({
          caption: '$UI_Common_Cancel',
          theme: 'secondary',
          type: 'normal',
          buttonAction: button => {
            fieldChangedDisposer?.();
            button.close(false);
          }
        })
      ],
      undefined,
      undefined,
      undefined,
      {
        showChrome: true,
        autoSizeWidth: true,
        chromeSettings: {
          title: localize('$UI_Cards_NewRecognitionRequestTitle'),
          showFullscreenButton: true
        },
        type: 'controls'
      }
    ));

  return dialogResult
    ? {
        request: cardModel.card.sections.get('OcrRequest').fields,
        languages: cardModel.card.sections.get('OcrRequestLanguages').rows
      }
    : null;
}

/**
 * Создает строку {@link CardRow} и переносит в нее данные по запросу из {@link request}.
 * @param request Запрос на распознавание текса.
 * @returns Строку {@link CardRow} с информацией по распознаванию.
 */
export function ocrCardRequestRowFactory(request: FieldMapStorage): CardRow {
  const row = new CardRow();
  row.state = CardRowState.Inserted;
  row.rowId = Guid.newGuid();
  row.set('Created', new Date().toISOString(), FieldType.DateTime);
  row.set('CreatedByID', userSession.UserID, FieldType.Guid);
  row.set('CreatedByName', userSession.UserName, FieldType.String);
  row.set('StateID', OcrRequestStates.Created, FieldType.Int);
  row.set('Confidence', request.getField('Confidence')!);
  row.set('Preprocess', request.getField('Preprocess')!);
  row.set('SegmentationModeID', request.getField('SegmentationModeID')!);
  row.set('SegmentationModeName', request.getField('SegmentationModeName')!);
  row.set('DetectLanguages', request.getField('DetectLanguages')!);
  row.set('Overwrite', request.getField('Overwrite')!);
  row.set('DetectRotation', request.getField('DetectRotation')!);
  row.set('DetectTables', request.getField('DetectTables')!);
  row.set('DetectBarcodes', request.getField('DetectBarcodes')!);
  return row;
}

/**
 * Создает строку {@link CardRow} и переносит в нее данные по языкам запроса из {@link languages}.
 * @param languages Языки для запроса на распознавание текса.
 * @param requestId Идентификатор созданной строки с запросом на распознавание текста.
 * @returns Строку {@link CardRow} с информацией по распознаванию.
 */
export function ocrCardLanguagesRowsFactory(
  languages: ArrayStorage<CardRow>,
  requestId: string
): CardRow[] {
  const uniqueLanguages = new Map<number, CardRow>();
  for (const language of languages) {
    const languageId = language.get<number>('LanguageID')!;
    if (!uniqueLanguages.has(languageId)) {
      const row = new CardRow();
      row.state = CardRowState.Inserted;
      row.rowId = Guid.newGuid();
      row.parentRowId = requestId;
      row.set('LanguageID', languageId, FieldType.Int);
      row.set('LanguageISO', language.getField('LanguageISO')!);
      row.set('LanguageCaption', language.getField('LanguageCaption')!);
      uniqueLanguages.set(languageId, row);
    }
  }
  return [...uniqueLanguages.values()];
}
