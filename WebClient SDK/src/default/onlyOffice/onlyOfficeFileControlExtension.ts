import {
  FileControlExtension,
  IFileControl,
  IFileControlExtensionContext,
  IFileGroupExtensionContext
} from 'tessa/ui/files';
import {
  MenuAction,
  SeparatorMenuAction,
  showError,
  showLoadingOverlay,
  showNotEmpty,
  tryGetFromSettings,
  UIButton
} from 'tessa/ui';
import { OnlyOfficeApiSingleton } from './onlyOfficeApiSingleton';
import { ValidationResult } from 'tessa/platform/validation';
import { FileCategory, FileContainer } from 'tessa/files';
import { Visibility } from 'tessa/platform';
import { runInAction } from 'mobx';
import { CardUIExtension, ICardModel, ICardUIExtensionContext } from 'tessa/ui/cards';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms';
import {
  FileListViewModel,
  FileTagViewModel,
  ViewControlButtonPanelViewModel,
  ViewControlViewModel
} from 'tessa/ui/cards/controls';
import { extension, localize } from '@tessa/application';
import { CardHelper, ICardTypeExtensionContext, CardTypeExtensionTypes } from '@tessa/platform';
import { StorageHelper } from '@tessa/core';

// TODO: tag should have feature to store description to show
const OONamesKey = '.coeditnames';
// const OODateKey = '.coeditdate';

const withUiHandler = async (
  action: () => Promise<{ validation: ValidationResult }>
): Promise<void> => {
  try {
    const { validation } = await showLoadingOverlay(action);
    await showNotEmpty(validation);
  } catch (e) {
    await showError(e.message);
  }
};

/**
 * Представляет собой расширение, которое добавляет возможность создания файлов по шаблону.
 */
@extension()
export class OnlyOfficeFileControlExtension extends FileControlExtension {
  public initializing(context: IFileControlExtensionContext): void {
    const control = context.control;
    const container = control.fileContainer;
    const newFileNameCaption = localize('$OnlyOffice_NewDocumentCaption');
    const isHidden = !control.fileContainer.permissions.canAdd;

    const createEmptyFileGroupButton = UIButton.create({
      icon: 'ta icon-thin-080',
      name: 'CreateEmptyFileGroup',
      tooltip: '$OnlyOffice_CreateEmptyFile',
      type: 'small',
      theme: 'control',
      child: [
        UIButton.create({
          name: 'CreateEmptyWordFile',
          caption: 'Word',
          buttonAction: () =>
            OnlyOfficeFileControlExtension.createEmptyWordFileAction(
              control,
              container,
              newFileNameCaption
            )
        }),
        UIButton.create({
          name: 'CreateEmptyExcelFile',
          caption: 'Excel',
          buttonAction: () =>
            OnlyOfficeFileControlExtension.createEmptyExcelFileAction(
              control,
              container,
              newFileNameCaption
            )
        }),
        UIButton.create({
          name: 'CreateEmptyPowerPointFile',
          caption: 'PowerPoint',
          buttonAction: () =>
            OnlyOfficeFileControlExtension.createEmptyPowerPointFileAction(
              control,
              container,
              newFileNameCaption
            )
        })
      ],
      visibility: isHidden ? Visibility.Collapsed : Visibility.Visible
    });

    runInAction(() => {
      control.fileControlButtons.push(createEmptyFileGroupButton);
    });

    for (const file of control.files) {
      const cardFile = container.files.find(x => x.id === file.id);
      const coeditNames = StorageHelper.tryGet<string>(cardFile?.info, OONamesKey);
      if (coeditNames) {
        file.tag = new FileTagViewModel('ta icon-thin-196', 'rgba(0, 176, 0, 0.19)');
      }
    }
  }

  public openingMenu(context: IFileControlExtensionContext): void {
    const control = context.control;
    const container = control.fileContainer;
    const actions = this.getFileActions(control, container);

    const createEmptyFileGroup = new MenuAction(
      'CreateEmptyFileGroup',
      '$OnlyOffice_CreateEmptyFile',
      'ta icon-thin-080',
      null,
      actions,
      !context.control.fileContainer.permissions.canAdd
    );

    // ищем пункт "Загрузить", после него ищем ближайший разделитель, и перед ним вставляем группу "Создать файл"
    let uploadActionIndex = context.actions.findIndex(a => a.name === 'Upload');
    if (uploadActionIndex !== -1) {
      const separatorIndex = context.actions
        .slice(uploadActionIndex)
        .findIndex(a => a instanceof SeparatorMenuAction);
      if (separatorIndex !== -1) {
        uploadActionIndex = uploadActionIndex + separatorIndex - 1;
      }
    }

    const insertToIndex = uploadActionIndex !== -1 ? uploadActionIndex + 1 : context.actions.length;
    context.actions.splice(insertToIndex, 0, createEmptyFileGroup);
  }

  public openingLocalMenu(context: IFileGroupExtensionContext): void {
    const { control, group, actions } = context;
    const { fileContainer } = control;

    const menuActions = this.getFileActions(control, fileContainer, group.category);

    actions.push(
      MenuAction.create({
        name: 'CreateEmptyFileGroup',
        caption: '$OnlyOffice_CreateEmptyFile',
        icon: 'ta icon-thin-080',
        children: menuActions,
        isCollapsed: !control.fileContainer.permissions.canAdd
      })
    );
  }

  public static async createEmptyWordFileAction(
    control: IFileControl,
    container: FileContainer,
    newFileNameCaption: string,
    category?: FileCategory | null
  ): Promise<void> {
    await withUiHandler(() =>
      OnlyOfficeApiSingleton.instance.createTemplateFile(
        control,
        container,
        'empty.docx',
        newFileNameCaption + '.docx',
        category
      )
    );
  }

  public static async createEmptyExcelFileAction(
    control: IFileControl,
    container: FileContainer,
    newFileNameCaption: string,
    category?: FileCategory | null
  ): Promise<void> {
    await withUiHandler(() =>
      OnlyOfficeApiSingleton.instance.createTemplateFile(
        control,
        container,
        'empty.xlsx',
        newFileNameCaption + '.xlsx',
        category
      )
    );
  }

  public static async createEmptyPowerPointFileAction(
    control: IFileControl,
    container: FileContainer,
    newFileNameCaption: string,
    category?: FileCategory | null
  ): Promise<void> {
    await withUiHandler(() =>
      OnlyOfficeApiSingleton.instance.createTemplateFile(
        control,
        container,
        'empty.pptx',
        newFileNameCaption + '.pptx',
        category
      )
    );
  }

  private getFileActions(
    control: IFileControl,
    container: FileContainer,
    category?: FileCategory | null
  ) {
    const newFileNameCaption = localize('$OnlyOffice_NewDocumentCaption');

    const createWordFileAction = new MenuAction(
      'CreateEmptyWordFile',
      'Word',
      'ta icon-thin-069',
      () =>
        OnlyOfficeFileControlExtension.createEmptyWordFileAction(
          control,
          container,
          newFileNameCaption,
          category
        )
    );

    const createExcelFileAction = new MenuAction(
      'CreateEmptyExcelFile',
      'Excel',
      'ta icon-thin-085',
      () =>
        OnlyOfficeFileControlExtension.createEmptyExcelFileAction(
          control,
          container,
          newFileNameCaption,
          category
        )
    );

    const createPowerPointFileAction = new MenuAction(
      'CreateEmptyPowerPointFile',
      'PowerPoint',
      'ta icon-thin-073',
      () =>
        OnlyOfficeFileControlExtension.createEmptyPowerPointFileAction(
          control,
          container,
          newFileNameCaption,
          category
        )
    );

    return [createWordFileAction, createExcelFileAction, createPowerPointFileAction];
  }

  public shouldExecute(): boolean {
    return OnlyOfficeApiSingleton.isAvailable;
  }
}

// В данном расширении добавляются кнопки onlyOffice в контролы представления с расширением на файлы
@extension()
export class OnlyOfficeViewFileControlExtension extends CardUIExtension {
  public async initialized(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.InitializeFilesView,
      context.card,
      context.model.generalMetadata,
      this.executeInitializedAction,
      context
    );

    context.validationResult.add(result);
  }

  private executeInitializedAction = async (context: ICardTypeExtensionContext) => {
    const uiContext = context.externalContext as ICardUIExtensionContext;
    const settings = context.settings;
    const filesViewAlias = tryGetFromSettings<string>(settings, 'FilesViewAlias', '');
    if (!filesViewAlias) {
      return;
    }
    if (!context.cardTask) {
      this.initializeViewControl(uiContext.model, filesViewAlias);
    } else {
      const model = uiContext.model;
      const tasks = (model.mainForm as DefaultFormTabWithTasksViewModel).tasks;
      if (!tasks) {
        return;
      }
      const task = tasks.find(x => x.taskModel.cardTask === context.cardTask);
      if (task) {
        task.modifyWorkspace(async () => {
          this.initializeViewControl(task.taskModel, filesViewAlias);
        });
      }
    }
  };

  private initializeViewControl(cardModel: ICardModel, viewControlAlias: string) {
    const viewControlViewModel = cardModel.controls.get(viewControlAlias) as ViewControlViewModel;
    if (!viewControlViewModel) {
      return;
    }

    const fileControl = StorageHelper.tryGet<FileListViewModel | null>(
      cardModel.info,
      viewControlAlias
    );
    if (!fileControl) {
      return;
    }

    const container = fileControl.fileContainer;
    const permissions = fileControl.fileContainer.permissions;
    const newFileNameCaption = localize('$OnlyOffice_NewDocumentCaption');

    const createEmptyFileGroupButton = UIButton.create({
      icon: 'ta icon-thin-080',
      name: 'CreateEmptyFileGroup',
      className: `files-control-button`,
      tooltip: '$OnlyOffice_CreateEmptyFile',
      type: 'small',
      theme: 'transparent',
      child: [
        UIButton.create({
          name: 'CreateEmptyWordFile',
          caption: 'Word',
          buttonAction: () =>
            OnlyOfficeFileControlExtension.createEmptyWordFileAction(
              fileControl,
              container,
              newFileNameCaption
            )
        }),
        UIButton.create({
          name: 'CreateEmptyExcelFile',
          caption: 'Excel',
          buttonAction: () =>
            OnlyOfficeFileControlExtension.createEmptyExcelFileAction(
              fileControl,
              container,
              newFileNameCaption
            )
        }),
        UIButton.create({
          name: 'CreateEmptyPowerPointFile',
          caption: 'PowerPoint',
          buttonAction: () =>
            OnlyOfficeFileControlExtension.createEmptyPowerPointFileAction(
              fileControl,
              container,
              newFileNameCaption
            )
        })
      ],
      visibility:
        permissions.canAdd && OnlyOfficeApiSingleton.isAvailable
          ? Visibility.Visible
          : Visibility.Collapsed
    });

    const bottomPanelButtons = viewControlViewModel.bottomItems.find(
      x => x.content instanceof ViewControlButtonPanelViewModel
    )?.content as ViewControlButtonPanelViewModel;

    if (!bottomPanelButtons) {
      return;
    }

    runInAction(() => {
      bottomPanelButtons.buttons.push(createEmptyFileGroupButton);
    });
  }
}
