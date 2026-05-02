import { FileGrouping, FileViewModel, GroupInfo } from 'tessa/ui/cards/controls';
import { tryGetFromInfo } from 'tessa/ui';
import { LocalizationManager, localize } from 'tessa/localization';
import { formatToString, formatDate } from 'tessa/platform/formatting';
import { FileCategory } from 'tessa/files';

export class CycleGrouping extends FileGrouping {
  //#region ctor

  constructor(name: string, caption: string, isCollapsed: boolean = false) {
    super(name, caption, isCollapsed);
  }

  //#endregion

  //#region methods

  public getGroupInfo(file: FileViewModel): GroupInfo {
    const cycleNumber = tryGetFromInfo<number>(file.model.info, 'KrCycleID');
    const cycleOrder = tryGetFromInfo<number>(file.model.info, 'KrCycleorder');
    const maxCyclewNumber = tryGetFromInfo<number>(file.model.info, 'KrMaxCycleNumber');
    if (cycleNumber != undefined && cycleOrder != undefined && maxCyclewNumber != undefined) {
      return {
        groupId: `CycleGroup${cycleNumber}`,
        groupCaption: LocalizationManager.instance.format(
          '$UI_Controls_FilesControl_CycleGroup',
          cycleNumber
        ),
        order: cycleOrder + 1
      };
    }

    return {
      groupId: 'DocumentsOnApprovalGroup',
      groupCaption: LocalizationManager.instance.localize(
        '$UI_Controls_FilesControl_DocumentsOnApprovalGroup'
      ),
      order: 0
    };
  }

  public clone(): CycleGrouping {
    return new CycleGrouping(this.name, this.caption, this.isCollapsed);
  }

  public attach(file: FileViewModel): void {
    super.attach(file);
    file.additionalInfoDelegate.set(CycleGrouping.getAdditionalInfo);
  }

  public detach(file: FileViewModel): void {
    super.detach(file);
    const restoredDelegate = file.additionalInfoDelegate.restore();
    if (restoredDelegate !== CycleGrouping.getAdditionalInfo) {
      throw new Error('FileViewModel.additionalInfoDelegate stack is damaged.');
    }
  }

  private static getAdditionalInfo(viewModel: FileViewModel): string | null {
    const file = viewModel.model;
    const origin = file.origin;
    const category = file.category;

    if (
      !origin &&
      category &&
      viewModel.collection.some(
        x => x.model.category && FileCategory.equals(x.model.category, category)
      )
    ) {
      return localize(category.caption);
    }

    const cycle = tryGetFromInfo<number>(file.info, 'KrCycleID');
    const createdByName = tryGetFromInfo<string>(file.info, 'KrCreatedByName');
    const created = tryGetFromInfo<string>(file.info, 'KrCreated');

    if (!origin && cycle != undefined && createdByName != undefined && created != undefined) {
      return LocalizationManager.instance.format(
        '$UI_Controls_FilesControl_Version_Description',
        file.lastVersion.number,
        createdByName
      );
    }

    if (origin) {
      const format = localize('$UI_Controls_FilesControl_ByCopy_Description');

      const copy = localize('$UI_Controls_FilesControl_ByCopy_Copy');
      return formatToString(
        format,
        copy,
        file.lastVersion.createdByName,
        formatDate(file.lastVersion.created)
      );
    }

    return null;
  }

  //#endregion
}
