import {
  CipherInfo,
  DeskiManager,
  DeskiSuccessResponse,
  FileProcessingInfo,
  versionCheck
} from 'tessa/deski';
import { ApplicationExtension, IApplicationExtensionMetadataContext } from 'tessa';
import {
  MenuAction,
  showLoadingOverlay,
  showNotEmpty,
  showWarning,
  tryGetFromInfo,
  tryGetFromSettings,
  UIButton
} from 'tessa/ui';
import {
  FileControlExtension,
  FileExtension,
  FileExtensionContext,
  FileVersionExtension,
  FileVersionExtensionContext,
  IFileControl,
  IFileControlExtensionContext,
  IFileGroupExtensionContext
} from 'tessa/ui/files';
import { FileCategory, FileType, IFile, IFileVersion } from 'tessa/files';

import { ValidationKey, ValidationResult, ValidationResultType } from 'tessa/platform/validation';
import { CardUIExtension, ICardModel, ICardUIExtensionContext } from 'tessa/ui/cards';
import {
  FileListViewModel,
  FileViewModel,
  ViewControlButtonPanelViewModel,
  ViewControlViewModel
} from 'tessa/ui/cards/controls';
import Platform from 'common/platform';
import { getNameAndExtForFile } from 'common/utility';
import { CardFileType, CardTypeExtensionTypes } from 'tessa/cards';
import { Visibility } from 'tessa/platform';
import { runInAction } from 'mobx';
import { DefaultFormTabWithTasksViewModel } from 'tessa/ui/cards/forms';
import {
  extension,
  HttpHooksContext,
  HttpRequestHeaders,
  inject,
  ISessionHealth$,
  LocalizationHelper,
  localize,
  ISessionHealth
} from '@tessa/application';
import {
  CardHelper,
  ICardTypeExtensionContext,
  IInitializationRepository,
  IInitializationRepository$
} from '@tessa/platform';
import format = LocalizationHelper.format;

const MASTER_KEYS_UPDATE_INTERVAL = 1000 * 60 * 60 * 4;

@extension({ name: 'DeskiExtension' })
export class DeskiExtension extends ApplicationExtension {
  constructor(
    @inject(IInitializationRepository$)
    private _initializationRepository: IInitializationRepository,
    @inject(ISessionHealth$) protected readonly _sessionHealth: ISessionHealth
  ) {
    super();
  }

  private _lastMasterKeysUpdate: number;

  public async afterMetadataReceived(
    _context: IApplicationExtensionMetadataContext
  ): Promise<void> {
    if (!DeskiManager.instance.deskiEnabled || Platform.isMobile()) {
      return;
    }

    (async () => {
      await DeskiManager.instance.checkDeski(true);
      const cipher = await this._initializationRepository.tryGet<CipherInfo>('DeskiCipher');
      if (!cipher) {
        return;
      }
      const result = await DeskiManager.instance.updateMasterKeys(cipher);
      await this._initializationRepository.delete('DeskiCipher');
      if (!result.isSuccessful) {
        console.error(result.format());
        await showNotEmpty(result);
        return;
      }
      this._lastMasterKeysUpdate = Date.now();

      // каждые 5 минут проверяем время, которое прошло с последнего обновления мастер ключа
      // если сессия не истекла и прошло больше константы (4 часа), то обновляемся
      setInterval(async () => {
        const now = Date.now();
        if (
          this._sessionHealth.isHealthy &&
          now - this._lastMasterKeysUpdate >= MASTER_KEYS_UPDATE_INTERVAL
        ) {
          const scope = HttpHooksContext.create(
            new HttpHooksContext([
              {
                beforeRequest: async ctx =>
                  ctx.request.headers.append(HttpRequestHeaders.RequestType, 'background')
              }
            ])
          );
          const deskiResult = await scope.run(() => DeskiManager.instance.updateMasterKeys());

          if (!deskiResult.isSuccessful) {
            console.error(deskiResult.format());
            return;
          }

          this._lastMasterKeysUpdate = now;
        }
      }, 300000);
    })();
  }
}

@extension({ name: 'DeskiFileExtension' })
export class DeskiFileExtension extends FileExtension {
  public openingMenu(context: FileExtensionContext): void {
    if (!DeskiManager.instance.deskiAvailable || Platform.isMobile()) {
      return;
    }

    const control = context.control;
    const singleMode = context.files.length === 1;
    const editCollapsed = !context.file.model.permissions.canEdit;
    const previewIndex = context.actions.findIndex(x => x.name === 'Preview');

    const deskiInfo = DeskiManager.instance.deskiInfo;
    const minRequiredVer = '2.1.0';
    const isWordSupported = deskiInfo?.OS
      ? deskiInfo?.OS === 'windows'
      : window.navigator?.platform?.toLocaleLowerCase().includes('win');
    const isSomeNotDocFile = (files: readonly FileViewModel[]) =>
      files.some(x => {
        const ext = getNameAndExtForFile(x.model.lastVersion.name).ext?.toLocaleLowerCase();
        return ext !== 'docx' && ext !== 'odt';
      });

    const canEdit = context.file.model.permissions.canEdit;
    // скрываем "Открыть для чтения", если это новый файл или файл с заменённой версией, который доступен для редактирования;
    // т.е. показываем, если либо недоступен для редактирования, либо несколько файлов, либо есть добавленная версия
    const canRead = !canEdit || context.files.length > 1 || !context.file.model.versionAdded;

    context.actions.splice(
      previewIndex + 1,
      0,
      new MenuAction(
        'OpenForEdit',
        '$UI_Controls_FilesControl_OpenForEdit',
        'ta icon-thin-002',
        async () => {
          for (const fileVM of context.files) {
            const file = fileVM.model;
            await openFile(file, file.lastVersion, 'file', true, file.name);
          }
          control.multiSelectionMode = false;
        },
        null,
        !canEdit
      ),
      new MenuAction(
        'OpenInFolderForEdit',
        '$UI_Controls_FilesControl_OpenInFolderForEdit',
        'ta icon-thin-101',
        async () => {
          const file = context.file.model;
          await openFile(file, file.lastVersion, 'folder', true, file.name);
        },
        null,
        !singleMode || editCollapsed
      ),
      new MenuAction(
        'OpenForRead',
        '$UI_Controls_FilesControl_OpenForRead',
        'ta icon-thin-008',
        async () => {
          for (const fileVM of context.files) {
            const file = fileVM.model;
            await openFile(file, file.lastVersion, 'file', false, file.name);
          }
          control.multiSelectionMode = false;
        },
        null,
        !canRead
      ),
      new MenuAction(
        'OpenInFolderForRead',
        '$UI_Controls_FilesControl_OpenInFolderForRead',
        'ta icon-thin-101',
        async () => {
          const file = context.file.model;
          await openFile(file, file.lastVersion, 'folder', false, file.name);
        },
        null,
        !singleMode || !editCollapsed
      ),

      new MenuAction(
        'MergeWithFollowingInWord',
        '$UI_Controls_FilesControl_MergeWithFollowingInWord',
        'ta icon-thin-359',
        async () => {
          if (!(await versionCheck(minRequiredVer))) {
            return;
          }
          const file = context.file.model;

          await processMassFiles(
            file,
            file.lastVersion,
            context.files.map(x => x.model.lastVersion),
            DeskiManager.instance.mergeFiles.bind(DeskiManager.instance),
            true,
            true
          );
        },
        null,
        singleMode || editCollapsed || !isWordSupported || isSomeNotDocFile(context.files)
      ),
      new MenuAction(
        'CompareInWord',
        '$UI_Controls_FilesControl_CompareInWord',
        'ta icon-thin-420',
        async () => {
          if (!(await versionCheck(minRequiredVer))) {
            return;
          }
          const file = context.file.model;
          await processMassFiles(
            file,
            file.lastVersion,
            context.files.map(x => x.model.lastVersion),
            DeskiManager.instance.compareFiles.bind(DeskiManager.instance),
            false,
            true
          );
        },
        null,
        singleMode ||
          !isWordSupported ||
          context.files.length > 2 ||
          isSomeNotDocFile(context.files)
      ),
      new MenuAction(
        'CopyToClipboard',
        '$UI_Controls_FilesControl_CopyToClipboard',
        'ta icon-thin-063',
        async () => {
          if (!(await versionCheck(minRequiredVer))) {
            return;
          }
          const file = context.file.model;
          await processMassFiles(
            file,
            file.lastVersion,
            context.files.map(x => x.model.lastVersion),
            (source, otherFiles) => DeskiManager.instance.copyToClipboard([source, ...otherFiles]),
            false,
            true
          );
        },
        null
      )
    );
  }
}

@extension({ name: 'DeskiFileControlExtension' })
export class DeskiFileControlExtension extends FileControlExtension {
  //#region FileControlExtension

  public initializing(context: IFileControlExtensionContext): void {
    const control = context.control;
    const isHidden =
      !control.fileContainer.permissions.canAdd || !DeskiManager.instance.deskiAvailable;

    const pasteFromClipboardButton = UIButton.create({
      icon: 'ta icon-thin-050',
      name: 'PasteFromClipboard',
      buttonAction: () => DeskiFileControlExtension.pasteFromClipboardAction(control),
      tooltip: '$UI_Controls_FilesControl_PasteFromClipboard',
      type: 'small',
      theme: 'control',
      visibility: isHidden ? Visibility.Collapsed : Visibility.Visible
    });

    runInAction(() => {
      control.fileControlButtons.push(pasteFromClipboardButton);
    });
  }

  public openingMenu(context: IFileControlExtensionContext): void {
    if (!DeskiManager.instance.deskiAvailable || Platform.isMobile()) {
      return;
    }

    const control = context.control;

    let index = 0;
    const uploadIndex = context.actions.findIndex(x => x.name === 'Upload');
    if (uploadIndex > -1) {
      index = uploadIndex + 1;
    }

    context.actions.splice(
      index,
      0,
      new MenuAction(
        'PasteFromClipboard',
        '$UI_Controls_FilesControl_PasteFromClipboard',
        'ta icon-thin-050',
        () => DeskiFileControlExtension.pasteFromClipboardAction(control),
        null,
        !context.control.fileContainer.permissions.canAdd
      )
    );
  }

  public openingLocalMenu(context: IFileGroupExtensionContext): void {
    if (!DeskiManager.instance.deskiAvailable || Platform.isMobile()) {
      return;
    }

    let index = 0;
    const { control, group, actions } = context;
    const uploadIndex = actions.findIndex(x => x.name === 'Upload');
    if (uploadIndex > -1) {
      index = uploadIndex + 1;
    }

    actions.splice(
      index,
      0,
      MenuAction.create({
        name: 'PasteFromClipboard',
        caption: '$UI_Controls_FilesControl_PasteFromClipboard',
        icon: 'ta icon-thin-050',
        action: () => DeskiFileControlExtension.pasteFromClipboardAction(control, group.category),
        isCollapsed: !control.fileContainer.permissions.canAdd
      })
    );
  }

  public static async pasteFromClipboardAction(
    control: IFileControl,
    category?: FileCategory | null
  ): Promise<void> {
    const minRequiredVer = '2.1.0';

    if (!(await versionCheck(minRequiredVer))) {
      return;
    }
    const contents = await pasteFromClipboard();
    if (contents.length === 0) {
      return;
    }

    await control.handleDropFiles(contents, category);
  }
}

// В данном расширении добавляются кнопки дески в контролы представления с расширением на файлы
@extension({ name: 'DeskiViewFileControlExtension' })
export class DeskiViewFileControlExtension extends CardUIExtension {
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

    const fileControl = tryGetFromInfo<FileListViewModel | null>(
      cardModel.info,
      viewControlAlias,
      null
    );
    if (!fileControl) {
      return;
    }

    const permissions = fileControl.fileContainer.permissions;

    const pasteFromClipboardButton = UIButton.create({
      icon: 'ta icon-thin-050',
      name: 'PasteFromClipboard',
      className: `files-control-button`,
      buttonAction: () => DeskiFileControlExtension.pasteFromClipboardAction(fileControl),
      tooltip: '$UI_Controls_FilesControl_PasteFromClipboard',
      type: 'small',
      theme: 'transparent',
      visibility:
        DeskiManager.instance.deskiAvailable && permissions.canAdd
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
      bottomPanelButtons.buttons.push(pasteFromClipboardButton);
    });
  }
}

@extension({ name: 'DeskiFileVersionExtension' })
export class DeskiFileVersionExtension extends FileVersionExtension {
  public static showOpenForReadWarningMessage = true;

  public openingMenu(context: FileVersionExtensionContext): void {
    if (!DeskiManager.instance.deskiAvailable || Platform.isMobile()) {
      return;
    }

    const control = context.control;
    const singleMode = context.versions.length === 1;
    const editCollapsed = !context.file.model.permissions.canEdit;
    const downloadIndex = context.actions.findIndex(x => x.name === 'VersionDownload');

    const deskiInfo = DeskiManager.instance.deskiInfo;
    const minRequiredVer = '2.1.0';
    const isWordSupported = deskiInfo?.OS
      ? deskiInfo?.OS === 'windows'
      : window.navigator?.platform?.toLocaleLowerCase().includes('win');
    const IsSomeNotDocVersion = (versions: readonly IFileVersion[]) =>
      versions.some(x => getNameAndExtForFile(x.name).ext?.toLocaleLowerCase() !== 'docx');

    context.actions.splice(
      downloadIndex + 1,
      0,
      new MenuAction(
        'OpenForRead',
        '$UI_Controls_FilesControl_OpenForRead',
        'ta icon-thin-008',

        async () => {
          for (const version of context.versions) {
            // При открытии файла на редактирование и при открытии его последней версии на чтение,
            // их контент будет отличаться после объединения файлов в Word.
            // После сохранения карточки, открывается корректно.
            // https://gitlab.syntellect.ru/tessa_team/tessa/-/issues/2057
            if (
              DeskiFileVersionExtension.showOpenForReadWarningMessage &&
              version.file.versionAdded &&
              version.file.versionAdded.id === version.id &&
              version.file.isDirty
            ) {
              const contentInfo = await DeskiManager.instance.getContentInfo(version.id);
              if (contentInfo.isCached) {
                await showWarning('$Deski_Warning_OpenForRead');
              }
            }

            await openFile(context.file.model, version, 'file', false, version.name);
          }
          control.multiSelectionMode = false;
        }
      ),
      new MenuAction(
        'OpenInFolderForRead',
        '$UI_Controls_FilesControl_OpenInFolderForRead',
        'ta icon-thin-101',
        async () => {
          const file = context.file.model;
          for (const version of context.versions) {
            await openFile(file, version, 'folder', false, file.name);
          }
        },
        null,
        !singleMode || !editCollapsed
      ),

      new MenuAction(
        'MergeNewVersionWithFollowingInWord',
        '$UI_Controls_FilesControl_MergeNewVersionWithFollowingInWord',
        'ta icon-thin-359',
        async () => {
          const file = context.file.model;

          if (file.versionAdded !== null || file.wasModified || file.isDirty) {
            await showWarning('$UI_Cards_WarnAboutUnsavedChangesInFileBeforeMergingVersions');
            return;
          }

          if (!(await versionCheck(minRequiredVer))) {
            return;
          }

          const result = await context.version.ensureContentDownloaded();
          if (!result.isSuccessful) {
            await showNotEmpty(result);
            return;
          }

          const content = context.version.content;
          if (!content) {
            await showNotEmpty(
              ValidationResult.fromText('$Deski_Cant_Load_Content', ValidationResultType.Error)
            );
            return;
          }

          const fileNameIsChanged = file.name !== content.name;
          file.replace(content, fileNameIsChanged);

          // добавляем новую версию в кеш Deski
          const resultCacheContent = await DeskiManager.instance.cacheContent(
            file.lastVersion.id,
            file.lastVersion.name,
            content
          );
          if (!resultCacheContent.success) {
            await showNotEmpty(resultCacheContent.result);
            return;
          }

          // версии должны мерджиться от первой к последней
          const versions = context.versions
            .filter(version => version.id !== context.version.id)
            .sort((a, b) => a.number - b.number);

          context.dialog.close();
          await showLoadingOverlay(
            async () =>
              await processMassFiles(
                file,
                file.lastVersion,
                versions,
                DeskiManager.instance.mergeFiles.bind(DeskiManager.instance),
                true
              ),
            { text: localize('$UI_Controls_FilesControl_MergeFiles') }
          );
        },
        null,
        context.versions.length === 1 ||
          editCollapsed ||
          !isWordSupported ||
          IsSomeNotDocVersion(context.versions)
      ),
      new MenuAction(
        'CompareInWord',
        '$UI_Controls_FilesControl_CompareInWord',
        'ta icon-thin-420',
        async () => {
          if (!(await versionCheck(minRequiredVer))) {
            return;
          }
          const file = context.file.model;
          await processMassFiles(
            file,
            context.version,
            context.versions,
            DeskiManager.instance.compareFiles.bind(DeskiManager.instance),
            false
          );
        },
        null,
        singleMode ||
          !isWordSupported ||
          context.versions.length > 2 ||
          IsSomeNotDocVersion(context.versions)
      ),
      new MenuAction(
        'CopyToClipboard',
        '$UI_Controls_FilesControl_CopyToClipboard',
        'ta icon-thin-063',
        async () => {
          if (!(await versionCheck(minRequiredVer))) {
            return;
          }
          const file = context.file.model;
          await processMassFiles(
            file,
            context.version,
            context.versions,
            (source, otherFiles) => DeskiManager.instance.copyToClipboard([source, ...otherFiles]),
            false
          );
        },
        null
      )
    );
  }
}

@extension({ name: 'DeskiUIExtension' })
export class DeskiUIExtension extends CardUIExtension {
  public static openFiles: IFile[] = [];

  shouldExecute(): boolean {
    return DeskiManager.instance.deskiAvailable;
  }
  public initialized(context: ICardUIExtensionContext): void {
    const fileControls = context.model.controlsBag.filter(
      x => x instanceof FileListViewModel
    ) as FileListViewModel[];
    for (const control of fileControls) {
      control.fileDoubleClickAction = async fileVM => {
        const file = fileVM.model;
        const canEdit = file.permissions.canEdit;
        await openFile(file, file.lastVersion, 'file', canEdit, file.name);
      };
    }
  }
  public async saving(context: ICardUIExtensionContext): Promise<void> {
    const files = context.fileContainer.files;
    if (!files || files.length === 0) {
      return;
    }

    const savingResult = context.validationResult;
    const editedFiles = files.filter(x => x.isDirty);
    if (editedFiles.length) {
      for (const file of editedFiles) {
        const content = file.lastVersion.content;
        try {
          const { info: fileInfo, result: fileInfoResult } =
            await DeskiManager.instance.getFileInfo(file.lastVersion.id, true);

          if (fileInfoResult) {
            savingResult.add(fileInfoResult);
            continue;
          }

          if (!fileInfo) {
            savingResult.add(
              ValidationKey.unknown,
              ValidationResultType.Error,
              `Can not find deski's FileInfo. File version: ${file.lastVersion.id}`
            );
            continue;
          }

          if (fileInfo!.IsLocked) {
            const details: string[] = [];
            if (fileInfo!.LockInfo) {
              for (const lock of fileInfo!.LockInfo) {
                details.push(
                  `File is being locked by process: PID=${lock.PID}, EXE=${lock.Title}.`
                );
              }
            }
            savingResult.add(
              ValidationKey.unknown,
              ValidationResultType.Error,
              format('$UI_Cards_FileSaving_Locked', fileInfo.Name),
              undefined,
              undefined,
              undefined,
              details.join('\n')
            );
            continue;
          }

          if (!fileInfo.IsModified) {
            file.isDirty = false;
            continue;
          }

          const cardFile = context.card.files.find(x => x.rowId === file.id);
          if (!cardFile) {
            savingResult.add(
              ValidationKey.unknown,
              ValidationResultType.Error,
              `Can not find file in card storage. File: ${file.id}`
            );
            continue;
          }

          const cacheResult = await DeskiManager.instance.cacheModFileWithNewId(
            fileInfo.ID,
            cardFile.versionRowId
          );
          if (!cacheResult.success) {
            savingResult.add(cacheResult.result);
          }

          const result = await file.lastVersion.ensureContentDownloaded();
          if (!result.isSuccessful) {
            savingResult.add(result);
            continue;
          }

          if (!content) {
            savingResult.add(
              ValidationResult.fromText('$Deski_Cant_Load_Content', ValidationResultType.Error)
            );
            continue;
          }

          // TODO: Возможно стоит сделать доработку со стороны deski для cacheModFileWithNewId, чтобы он мог менять имя файла.
          // Т.к. при изменении имени файла, имя берется из текущей lastVersion.
          // После доработки код ниже нужно убрать.
          if (file.versionAdded) {
            file.versionAdded = null;
          }
          file.replace(content, false);
        } catch (error) {
          savingResult.add(ValidationResult.fromError(error));
        }
      }
    }
  }
  public async finalized(): Promise<void> {
    // очищаем открытые файлы дески при сохранении или закрытии карточки
    await clearEditedFiles();
  }
}

async function openFile(
  file: IFile,
  version: IFileVersion,
  mode: 'file' | 'folder',
  editable: boolean,
  curFileName: string
): Promise<void> {
  if (!(await DeskiManager.instance.checkDeski())) {
    return;
  }
  let result = await DeskiManager.instance.setAppKeys(DeskiManager.instance.masterKeys);
  if (!result.success) {
    await showNotEmpty(result.result);
    return;
  }

  const contentInfo = await DeskiManager.instance.getContentInfo(version.id);
  if (contentInfo.isCached && contentInfo.fileName !== curFileName) {
    const renameResult = await DeskiManager.instance.renameContent(
      version.id,
      curFileName,
      editable
    );

    if (!renameResult.isSuccessful) {
      await showNotEmpty(renameResult);
      return;
    }
  }
  if (contentInfo.result) {
    await showNotEmpty(contentInfo.result);
    return;
  }

  const isVirtual = isVirtualFile(file.type);
  if (!contentInfo.isCached || isVirtual) {
    if (isVirtual) {
      await DeskiManager.instance.removeFile(version.id, editable);
    }

    const validationResult = await version.ensureContentDownloaded();
    if (!validationResult.isSuccessful) {
      await showNotEmpty(validationResult);
      return;
    }

    const content = version.content;
    if (!content) {
      await showNotEmpty(
        ValidationResult.fromText('$Deski_Cant_Load_Content', ValidationResultType.Error)
      );
      return;
    }
    result = await DeskiManager.instance.cacheContent(version.id, curFileName, content);
    if (!result.success) {
      await showNotEmpty(result.result);
      return;
    }
  }

  result = await DeskiManager.instance.openFile(version.id, mode, editable);
  if (!result.success) {
    await showNotEmpty(result.result);
    return;
  }

  if (editable) {
    file.isDirty = true;
  }

  DeskiUIExtension.openFiles.push(file);
}

async function processMassFiles(
  sourceFile: IFile,
  sourceVersion: IFileVersion,
  fileVersions: readonly IFileVersion[],
  func: (source: FileProcessingInfo, otherFiles: FileProcessingInfo[]) => DeskiSuccessResponse,
  editable = false,
  useFileName = false
): Promise<void> {
  if (!(await DeskiManager.instance.checkDeski())) {
    return;
  }

  const forProcessingFiles: FileProcessingInfo[] = [];
  for (const version of fileVersions) {
    let result = await DeskiManager.instance.setAppKeys(DeskiManager.instance.masterKeys);
    if (!result.success) {
      await showNotEmpty(result.result);
      return;
    }
    const contentInfo = await DeskiManager.instance.getContentInfo(version.id);
    if (contentInfo.result) {
      await showNotEmpty(contentInfo.result);
      return;
    }

    let isCached = contentInfo.isCached;
    if (isCached && isVirtualFile(version.file.type)) {
      await DeskiManager.instance.removeFile(version.id, editable);
      isCached = false;
    }

    const currentFileName = useFileName ? version.file.name : version.name;
    if (isCached && contentInfo.fileName !== currentFileName) {
      const renameResult = await DeskiManager.instance.renameContent(
        version.id,
        currentFileName,
        useFileName && version.isDirty
      );

      if (!renameResult.isSuccessful) {
        await showNotEmpty(renameResult);
        return;
      }
    }
    if (!isCached) {
      const validationResult = await version.ensureContentDownloaded();
      if (!validationResult.isSuccessful) {
        await showNotEmpty(validationResult);
        return;
      }
      const content = version.content;

      if (!content) {
        await showNotEmpty(
          ValidationResult.fromText('$Deski_Cant_Load_Content', ValidationResultType.Error)
        );
        return;
      }
      result = await DeskiManager.instance.cacheContent(version.id, currentFileName, content);
      if (!result.success) {
        await showNotEmpty(result.result);
        return;
      }
    }
    if (version.id !== sourceVersion.id) {
      forProcessingFiles.push({
        id: version.id,
        author: version.createdByName,
        editable: useFileName && version.isDirty
      });
    }
  }

  const result = await func(
    {
      id: sourceVersion.id,
      author: sourceVersion.createdByName,
      editable: useFileName && sourceFile.isDirty
    },
    forProcessingFiles
  );
  if (!result.success) {
    await showNotEmpty(result.result);
    return;
  }

  if (editable) {
    sourceFile.isDirty = true;
  }
}

async function pasteFromClipboard(): Promise<File[]> {
  if (!(await DeskiManager.instance.checkDeski())) {
    return [];
  }

  const result = await DeskiManager.instance.pasteFromClipboard();
  if (result.result) {
    await showNotEmpty(result.result);
    return [];
  }
  return result.files;
}

const clearEditedFiles = async () => {
  for (const file of DeskiUIExtension.openFiles) {
    try {
      await removeFile(file);
    } catch (error) {
      console.error(error);
    }
  }

  DeskiUIExtension.openFiles.length = 0;
};

const removeFile = async (file: IFile) => {
  await DeskiManager.instance.removeFile(file.lastVersion.id, true);
};

const isVirtualFile = (fileType: FileType | null): boolean => {
  if (fileType && fileType instanceof CardFileType) {
    return fileType.isVirtual;
  }

  return false;
};
