import { Guid, IStorage, StorageHelper, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { FileVersionState } from 'tessa/files/fileVersion';
import { IFile } from 'tessa/files/file';
import { UIContext } from 'tessa/ui/uiContext';
import { MenuAction } from 'tessa/ui/menuAction';
import { showLoadingOverlay } from 'tessa/ui/loadingOverlay';
import { FileExtension } from 'tessa/ui/files/fileExtension';
import { IFileExtensionContext } from 'tessa/ui/files/interfaces';
import { ICardEditorModel, ICardModel } from 'tessa/ui/cards';
import { AdvancedCardDialogManager } from 'tessa/ui/cards/advancedCardDialogManager';
import {
  OcrOperationTypeId,
  OcrOperationTypeName,
  MultipageFileExtensions,
  OcrKey,
  SupportedFileExtensions
} from '../misc/ocrConstants';
import { ocrCardRequestDialogFactory } from '../misc/ocrCardRequestDialogFactory';
import { OcrSettings } from '../misc/ocrSettings';
import { OcrHelper } from '../misc/ocrHelper';

/**
 * Расширение, добавляющее пункт "Распознавание текста" в контекстное меню файла.
 * @remarks Детальное описание расширения:
 * 1. Пункт контекстного меню добавляется в файл, если:
 *   - Включена функциональность OCR.
 *   - Файл не был добавлен или изменён.
 *   - У текущего пользователя есть права на замену файла.
 *   - В файловом контроле выбран только один файл.
 *   - Расширение файла соответствует допустимым для распознавания.
 * 2. При нажатии на пункт контекстного меню выполняется:
 *   - Проверка наличия уже созданной карточки операции OCR.
 *   - В случае отсутствия карточки операции OCR отображается диалог
 *     с параметрами нового запроса на распознавание (только в экспертном режиме)
 *     и создаётся карточка с заданными параметрами запроса на распознавание.
 *   - Отображение карточки операции OCR в диалоговом окне.
 */
@extension({ name: 'OcrSourceFileMenuExtension' })
export class OcrSourceFileMenuExtension extends FileExtension {
  //#region base overrides

  shouldExecute(_context: IFileExtensionContext): boolean {
    return OcrSettings.instance.isEnabled;
  }

  openingMenu(context: IFileExtensionContext): void {
    const { editor, model } = OcrHelper.getContextEditorModel();
    if (!editor || !model) {
      return;
    }

    const file = context.file.model;
    const fileVersion = file.lastVersion;

    // Проверка на возможность выполнения распознавания файла
    const canRecognize =
      !file.isDirty &&
      context.files.length === 1 &&
      file.permissions.canReplace &&
      fileVersion !== file.versionAdded &&
      fileVersion.state === FileVersionState.Success &&
      SupportedFileExtensions.includes(file.getExtension());
    if (!canRecognize) {
      return;
    }

    // Находим пункт "Предпросмотр" контекстного меню файла
    const previewIndex = context.actions.findIndex(x => x.name === 'Preview');
    if (previewIndex === -1) {
      return;
    }

    // Добавляем в контекстное меню файла пункт "Распознавание текста" после пункта "Предпросмотр"
    context.actions.splice(
      previewIndex + 1,
      0,
      MenuAction.create({
        name: 'TextRecognition',
        caption: '$UI_Controls_FilesControl_TextRecognition',
        icon: getTessaIcon('Thin76'),
        action: async event => await this.textRecognitionAction(event, editor, model, file)
      })
    );
  }

  //#endregion

  //#region private methods

  private async textRecognitionAction(
    event: React.MouseEvent,
    editor: ICardEditorModel,
    model: ICardModel,
    file: IFile
  ): Promise<void> {
    // Пытаемся получить параметры OCR из опций исходного файла
    const ocrOptions = StorageHelper.tryGet<IStorage>(file.options, OcrKey);
    let ocrCardId = StorageHelper.tryGet<string>(ocrOptions, 'CardID');

    if (!ocrCardId) {
      const isMultipage = MultipageFileExtensions.includes(file.getExtension());
      // Отображаем диалог инициации нового запроса на распознавание
      const result = await ocrCardRequestDialogFactory(isMultipage, !event.shiftKey);
      if (!result) {
        return;
      }

      // Достаем карточку файла, запоминаем состояние и опции файла
      const cardFile = model.card.files.find(f => Guid.equals(f.rowId, file.id))!;
      const cardFileFlags = cardFile.flags;

      // В карточке уже может быть несколько распознанных файлов,
      // или же может быть несколько измененных файлов, поэтому
      // добавляем флаг, чтобы однозначно отличить текущий распознаваемый файл.
      cardFile.info[OcrKey] = TypedField.trueBoolean;
      ocrCardId = Guid.newGuid();

      // В опции исходного файла добавляется идентификатор создаваемой карточки OCR
      file.options[OcrKey] = { ['CardID']: TypedField.createGuid(ocrCardId) };
      file.source.notifyOptionsModified(file);

      // Параметры для OCR запроса добавляем в качестве дополнительной информации при сохранении карточки
      const info: IStorage = {
        [OcrKey]: {
          OcrRequests: result.request.getStorage(),
          OcrRequestsLanguages: result.languages.getStorage()
        }
      };

      // Пытаемся сохранить карточку, если неуспешно, то удаляем признак OCR из файла
      let saved = false;
      try {
        saved = await editor.saveCard(editor.context, info);
      } finally {
        if (!saved) {
          delete cardFile.info[OcrKey];
          delete file.options[OcrKey];
          file.source.notifyOptionsModified(file);
          cardFile.flags = cardFileFlags;
          ocrCardId = null;
          return;
        }
      }
    }

    // К этому моменту, на сервере уже была создана карточка OCR и известным нам идентификатором,
    // а также создан запрос на распознавание файла, поэтому можем открыть карточку OCR
    await showLoadingOverlay(async splashResolve => {
      await AdvancedCardDialogManager.instance.openCard({
        cardId: ocrCardId ?? undefined,
        cardTypeId: OcrOperationTypeId,
        cardTypeName: OcrOperationTypeName,
        context: UIContext.current,
        withUIExtensions: true,
        splashResolve: splashResolve,
        dialogOptions: { openInFullscreen: true },
        cardModifierAction: async ({ card }) => {
          card.info[OcrKey] = TypedField.trueBoolean;
        },
        cardEditorModifierAction: async ({ editor }) => {
          event.shiftKey && OcrHelper.setExpertMode(editor.info, true);
        }
      });
    });
  }

  //#endregion
}
