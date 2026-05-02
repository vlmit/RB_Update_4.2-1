import { AbExternalFile } from './abExternalFile';
import { FileGrouping, FileViewModel, GroupInfo } from 'tessa/ui/cards/controls';

export class AbFileSourceGrouping extends FileGrouping {
  public readonly cardSourceGroupName = 'AbCardSourceGroup';
  public readonly externalSourceGroupName = 'AbExternalSourceGroup';

  public getGroupInfo(file: FileViewModel): GroupInfo {
    const isExternal = file.model instanceof AbExternalFile;

    if (isExternal) {
      return {
        groupId: this.externalSourceGroupName,
        groupCaption: '$AbTest_ExternalFilesGroup',
        order: -1
      };
    } else {
      return {
        groupId: this.cardSourceGroupName,
        groupCaption: '$AbTest_MainFilesGroup'
      };
    }
  }

  public clone(): AbFileSourceGrouping {
    return new AbFileSourceGrouping(this.name, this.caption, this.isCollapsed);
  }
}
