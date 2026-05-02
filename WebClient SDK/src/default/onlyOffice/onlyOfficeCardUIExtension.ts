import { extension } from '@tessa/application';
import { IFile } from 'tessa/files/file';
import { LocalizationManager } from 'tessa/localization/localizationManager';
import { ValidationResult, ValidationResultType } from 'tessa/platform/validation';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { OnlyOfficeApiSingleton } from './onlyOfficeApiSingleton';
import { OnlyOfficeOpenFileInfo } from './onlyOfficeOpenFileInfo';
import { TextBoxViewModel } from 'tessa/ui/cards/controls';
import { UIButton, ClipboardHelper } from 'tessa/ui';

/**
 * Представляет собой расширение, которое устанавливает в качестве средства предпросмотра OnlyOffice
 * и предупреждает пользователя об открытых файлах на редактирование при операциях с карточкой.
 */
@extension({ name: 'OnlyOfficeCardUIExtension' })
export class OnlyOfficeCardUIExtension extends CardUIExtension {
  public initialized(context: ICardUIExtensionContext): void {
    const cardModel = context.model;

    const jwtSecretControl = cardModel.controls.get('JWTSecret') as TextBoxViewModel;
    if (!jwtSecretControl) {
      return;
    }

    if (jwtSecretControl.controlContainer) {
      jwtSecretControl.controlContainer.showButtonsWhenDisabled = true;
    }
    const api = OnlyOfficeApiSingleton.instance;
    const editButton = UIButton.create({
      name: 'EditJWTSecret',
      icon: 'm-pen',
      theme: 'control',
      type: 'small',
      buttonAction: () => {
        jwtSecretControl.isReadOnly = false;
        jwtSecretControl.text = '';
        jwtSecretControl.buttonsContainer.removeButton(editButton);
        jwtSecretControl.buttonsContainer.removeButton(copyJWTSecretButton);
      }
    });
    const copyJWTSecretButton = UIButton.create({
      name: 'CopyJWTSecret',
      icon: 'm-copy',
      theme: 'control',
      type: 'small',
      buttonAction: async () => {
        const { jwtSecret, validationResult } = await api.getJWTSecret();
        if (
          validationResult &&
          (await showNotEmpty(validationResult)) &&
          validationResult.hasErrors
        ) {
          return;
        }
        jwtSecret && ClipboardHelper.copyToClipboard(jwtSecret);
      }
    });
    jwtSecretControl.buttonsContainer.addButton(editButton);
    jwtSecretControl.buttonsContainer.addButton(copyJWTSecretButton);
  }

  public reopening(context: ICardUIExtensionContext): void {
    const res = OnlyOfficeCardUIExtension.checkOpenFiles(
      context.fileContainer.files,
      context.card.id
    );

    if (res.hasErrors) {
      context.validationResult.add(res);
    }
  }

  public saving(context: ICardUIExtensionContext): void {
    const res = OnlyOfficeCardUIExtension.checkOpenFiles(
      context.fileContainer.files,
      context.card.id
    );

    if (res.hasErrors) {
      context.validationResult.add(res);
    }
  }

  public async finalizing(context: ICardUIExtensionContext): Promise<void> {
    const res = OnlyOfficeCardUIExtension.checkOpenFiles(
      context.fileContainer.files,
      context.card.id
    );

    if (res.hasErrors) {
      context.cancel = true;
      await showNotEmpty(res);
    }
  }

  private static checkOpenFiles(files: ReadonlyArray<IFile>, cardId: string): ValidationResult {
    const openFilesForEdit: OnlyOfficeOpenFileInfo[] = [];

    for (const file of files) {
      for (const version of file.versions) {
        const openedFileInfo = OnlyOfficeApiSingleton.instance.openFiles.find(
          of => of.forEdit && of.version.id === version.id && of.cardId === cardId
        );

        if (openedFileInfo) {
          openFilesForEdit.push(openedFileInfo);
        }
      }
    }

    if (openFilesForEdit.length === 0) {
      return ValidationResult.empty;
    }

    const openFileNames = openFilesForEdit.map(of => of.version.file.name).join('\n');
    return ValidationResult.fromText(
      LocalizationManager.instance.format(
        '$OnlyOffice_Error_CloseEditableFilesBeforeContinue',
        openFileNames
      ),
      ValidationResultType.Error
    );
  }

  public shouldExecute(): boolean {
    return OnlyOfficeApiSingleton.isAvailable;
  }
}
