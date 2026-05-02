import { AdvancedCardDialogManager, ICardModel } from 'tessa/ui/cards';
import { FileListViewModel, FileViewModel, ViewControlViewModel } from 'tessa/ui/cards/controls';
import { TaskViewModel } from 'tessa/ui/cards/tasks';
import { FileSigningOptionsStorage } from '../fileSigningOptionsStorage';
import { TableFileRowViewModel } from '../../cardFiles/tableFileRowViewModel';
import { createDialogForm, FormCreationOptions, UIButton } from 'tessa/ui';
import { systemKeyPrefix } from 'tessa/cards';

export namespace SignFileHelper {
  /**
   * Универсальный хелпер для подготовки и обработки сценариев подписания файлов.
   * Выполняет выборку и фильтрацию файлов, учитывая настройки задачи,
   * и передаёт управление обработчику, который определяет дальнейшие действия (например,
   * подписать файлы напрямую или показать диалог выбора файлов для подписи).
   *
   * @param cardModel Модель карточки, в которой производится подписание файлов.
   * @param taskViewModel Модель задания, из которой берутся настройки подписания.
   * @param handler Функция-обработчик, получающая подготовленные списки файлов и параметры,
   *               и реализующая логику подписания или показа диалога.
   * @returns true, если операция подписания завершилась успешно, иначе false.
   */
  export const handleSignFiles = async (
    cardModel: ICardModel,
    taskViewModel: TaskViewModel,
    handler: (
      shownFilesVMs: FileViewModel[],
      selectedFilesVMs: FileViewModel[] | null,
      cardModel: ICardModel,
      noCommentDialog: boolean,
      noSignFileDialog: boolean
    ) => Promise<boolean>
  ): Promise<boolean> => {
    const fileControlModel = cardModel.controls.get('Files') as FileListViewModel;
    if (!fileControlModel) {
      return false;
    }

    const setting = taskViewModel.taskModel.cardTask?.settings;
    if (!setting) {
      return false;
    }

    const {
      noSignFileDialog,
      doNotSignFileCopies,
      noCommentDialog,
      fileCategories,
      selectedFiles,
      hiddenFileCategories,
      hiddenFiles
    } = new FileSigningOptionsStorage(setting);

    const shownFilesVMs = fileControlModel.files.filter(x => {
      if (cardModel.card.files.find(y => y.rowId === x.id)?.isVirtual ?? true) {
        return false;
      }
      if (doNotSignFileCopies && x.model.origin) {
        return false;
      }
      if (
        hiddenFileCategories.length > 0 &&
        x.model.category &&
        hiddenFileCategories.includes(x.model.category.id!)
      ) {
        return false;
      }
      return hiddenFiles.length === 0 || !hiddenFiles.includes(x.id);
    });

    let selectedFilesVMs: FileViewModel[] | null = null;
    if (fileCategories.length > 0 || selectedFiles.length > 0) {
      selectedFilesVMs = shownFilesVMs.filter(
        x =>
          (selectedFiles.length > 0 && selectedFiles.includes(x.id)) ||
          (x.model.category && fileCategories.includes(x.model.category.id!))
      );
    }
    const result = await handler(
      shownFilesVMs,
      selectedFilesVMs,
      cardModel,
      noCommentDialog,
      noSignFileDialog
    );
    return result;
  };

  /**
   * Открывает диалог выбора файлов для подписи и настраивает обработчик кнопки "Подписать".
   * Позволяет выбрать файлы для подписи, автоматически выделить нужные,
   * и выполнить пользовательский обработчик при подтверждении действия.
   *
   * @param filesToShow Список файлов для отображения в диалоге.
   * @param filesToAutoSelect Список файлов, которые будут автоматически выделены.
   * @param mainCardModel Модель карточки, в которой производится подписание файлов.
   * @param signButtonAction Функция-обработчик, вызываемая при нажатии кнопки "Подписать".
   * @returns true, если операция завершилась успешно, иначе false.
   */
  export const showSignFilesDialog = async (
    filesToShow: FileViewModel[],
    filesToAutoSelect: FileViewModel[] | null,
    mainCardModel: ICardModel,
    signButtonAction: (
      selectedRows: TableFileRowViewModel[],
      mainCardModel: ICardModel
    ) => Promise<boolean>
  ): Promise<boolean> => {
    const result = await createDialogForm(
      'KrSignFilesDialog',
      'MainTab',
      FormCreationOptions.AlwaysCreateTabbedForm,
      undefined,
      undefined,
      undefined,
      {
        createFileContainer: true,
        containerFiles: filesToShow.map(f => f.model)
      }
    );
    if (!result) {
      throw new Error('Failed to create dialog. Dialog name: "KrSignFilesDialog".');
    }

    const [dialogForm, dialogModel] = result;
    dialogModel.mainForm = dialogForm;

    const fileControl = dialogModel.controlsBag.find(
      x => x.name === 'FilesView' && x instanceof ViewControlViewModel
    ) as ViewControlViewModel;
    if (!fileControl || !fileControl.table) {
      return false;
    }

    let isSuccessful = false;
    await AdvancedCardDialogManager.instance.showCardModel({
      cardModel: dialogModel,
      displayValue: '$UI_SignFilesDialogs_Title',
      dialogOptions: {
        withDialogWallpaper: true,
        dialogAutoSizeWidth: false,
        dialogAutoSizeHeight: false,
        cardModeDialog: false,
        disablePaddings: false,
        disableGap: false
      },
      prepareEditorAction: async editor => {
        await fileControl.initialize();
        fileControl.table!.rowContextMenuGenerators.length = 0;
        fileControl.bottomItems.length = 0;
        fileControl.table!.modifyRowActions.add(row => {
          if (
            row instanceof TableFileRowViewModel &&
            filesToAutoSelect?.some(x => x.id === row.fileViewModel.id)
          ) {
            row.selectRow(true);
          }
        });

        editor.context.info[systemKeyPrefix + 'DialogClosingAction'] = () => Promise.resolve(false);

        editor.toolbar.clearItems();
        editor.bottomDialogButtons.length = 0;

        editor.bottomDialogButtons.push(
          UIButton.create({
            caption: '$UI_Tasks_CompletionOptions_Sign',
            buttonAction: async () => {
              const selectedRows = fileControl.table?.rows
                .filter(x => x.isSelected)
                .map(x => x as TableFileRowViewModel);
              if (!selectedRows || selectedRows.length === 0) {
                return;
              }
              isSuccessful = await signButtonAction(selectedRows, mainCardModel);

              if (!isSuccessful) {
                return;
              }
              await editor.close();
            },
            theme: 'primary',
            type: 'normal'
          }),
          UIButton.create({
            caption: '$UI_Common_Cancel',
            buttonAction: async () => {
              await editor.close();
            },
            theme: 'secondary',
            type: 'normal'
          })
        );
        return true;
      }
    });

    return isSuccessful;
  };
}
