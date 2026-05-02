import { extension } from '@tessa/application';
import { UIContext } from 'tessa/ui';
import { FileExtension, IFileExtensionContext } from 'tessa/ui/files';
import { PreviewFilesList } from 'tessa/ui/preview';
import { checkCanDownloadFile, checkCanDownloadFilesAndShowMessages } from 'tessa/files';

const AbCarCardTypeID = 'd0006e40-a342-4797-8d77-6501c4b7c4ac';

@extension({ name: 'abCarFileControlExtension' })
export class AbCarFileControlExtension extends FileExtension {
  public shouldExecute(): boolean {
    if (UIContext.current.cardEditor?.cardModel?.cardType.id !== AbCarCardTypeID) {
      return false;
    }

    return true;
  }

  openingMenu(context: IFileExtensionContext): void {
    const fileVM = context.file;
    const fileVersion = fileVM.model.lastVersion;
    const previewAction = context.actions.find(x => x.name === 'Preview');
    if (!previewAction) {
      return;
    }

    previewAction.action = async () => {
      if (!(await checkCanDownloadFilesAndShowMessages([fileVersion]))) {
        return;
      }

      const manager = context.control.manager;
      const filesListControl = fileVM.listModel;
      if (!manager || !filesListControl) {
        return;
      }

      const getFiles = () => {
        const files = filesListControl.groups
          ? filesListControl.resultGroupedFiles.flatMap(group => group.files)
          : filesListControl.filteredFiles;

        return files
          .map(vm => vm.model.lastVersion)
          .filter(x => checkCanDownloadFile(x).items.length === 0);
      };

      const previewList = new PreviewFilesList(getFiles, fileVersion);
      await manager.showPreview(previewList);
    };
  }
}
