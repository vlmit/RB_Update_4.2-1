import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { createCardFileSourceForCard } from 'tessa/ui';
import { IStorage, clear } from 'tessa/platform/storage';
import { Guid, TypedField } from 'tessa/platform';
import { VirtualFile, VirtualFileVersion } from 'tessa/files';
import { CardFileContentSource } from 'tessa/cards';
import { extension } from '@tessa/application';
import { FieldType, StorageHelper } from '@tessa/core';
import { CycleGroupingFileInfo } from './cycleGroupingFileInfo';

@extension({ name: 'KrGetCycleFileInfoUIExtension' })
export class KrGetCycleFileInfoUIExtension extends CardUIExtension {
  public async initializing(context: ICardUIExtensionContext): Promise<void> {
    if (context.model.inSpecialMode) {
      return;
    }

    const cardInfo = context.card.tryGetInfo();
    if (!cardInfo) {
      return;
    }

    const maxCycleNumberObj = StorageHelper.tryGet<number>(cardInfo, 'KrMaxCycleNumber');
    if (maxCycleNumberObj == undefined) {
      return;
    }

    const maxCycleNumber = Number(maxCycleNumberObj);

    const filesByCycles = StorageHelper.tryGet<{
      [key: string]: TypedField<FieldType.Guid, string>;
    }>(cardInfo, 'KrFilesByCycles');

    if (filesByCycles) {
      for (const fileId in filesByCycles) {
        const cycleIdObj = TypedField.tryGet(filesByCycles[fileId]);
        const cycleId = Number(cycleIdObj);
        const fileModel = context.fileContainer.files.find(x => Guid.equals(x.id, fileId));
        if (fileModel) {
          fileModel.info['KrCycleID'] = cycleId;
          fileModel.info['KrCycleorder'] = maxCycleNumber - cycleId;
          fileModel.info['KrMaxCycleNumber'] = maxCycleNumber;
        }
      }
    }

    const filesModifiedByCyclesStorage = StorageHelper.tryGet<IStorage[]>(
      cardInfo,
      'KrFilesModifiedByCycles'
    );
    if (filesModifiedByCyclesStorage && filesModifiedByCyclesStorage.length > 0) {
      // clone - пустая карточка с той же информацией по типу и по версии, и др. системной информацией, но без фактических данных;
      // в неё будут добавляться виртуальные файлы, чтобы незахламлять структуру основной карточки

      const clone = context.card.clone();
      clone.sections.clear();
      clone.files.clear();
      clone.tasks.clear();
      clone.taskHistory.clear();
      clone.taskHistoryGroups.clear();
      clear(clone.info);

      const cloneFileSource = createCardFileSourceForCard(clone);

      for (const versionInfo of filesModifiedByCyclesStorage) {
        const cycleGroupingFileInfo = new CycleGroupingFileInfo().deserializeFromStorage(
          versionInfo
        );

        const originalFile = context.fileContainer.files.find(x =>
          Guid.equals(x.id, cycleGroupingFileInfo.fileId)
        );
        if (!originalFile) {
          continue;
        }

        const originalCardFile = context.card.files.find(x =>
          Guid.equals(x.rowId, cycleGroupingFileInfo.fileId)
        );
        if (!originalCardFile) {
          continue;
        }

        // теперь создаём файл и наполняем его
        const virtualFile = await context.fileContainer.addVirtualFile(
          cloneFileSource,
          new VirtualFile(
            originalCardFile.typeName,
            Guid.newGuid(),
            originalCardFile.name,
            token => {
              token.size = cycleGroupingFileInfo.versionSize;
              token.lastVersionTags.push(...originalFile.lastVersion.tags);
            }
          ),
          new VirtualFileVersion(Guid.newGuid(), originalCardFile.name, token => {
            token.size = cycleGroupingFileInfo.versionSize;
            token.number = cycleGroupingFileInfo.versionNumber;
            token.created = cycleGroupingFileInfo.versionCreated;
            token.createdById = cycleGroupingFileInfo.versionCreatedById;
            token.createdByName = cycleGroupingFileInfo.versionCreatedByName;
            token.tags.push(...originalFile.lastVersion.tags);
          })
        );

        const virtualCardFile = clone.files.find(x => x.card.id === virtualFile.id)!;
        virtualCardFile.card.createdById = cycleGroupingFileInfo.versionCreatedById;
        virtualCardFile.card.createdByName = cycleGroupingFileInfo.versionCreatedByName;
        virtualCardFile.card.created = cycleGroupingFileInfo.versionCreated;
        virtualCardFile.lastVersion!.number = cycleGroupingFileInfo.versionNumber;
        virtualCardFile.lastVersion!.created = cycleGroupingFileInfo.versionCreated;
        virtualCardFile.lastVersion!.createdById = cycleGroupingFileInfo.versionCreatedById;
        virtualCardFile.lastVersion!.createdByName = cycleGroupingFileInfo.versionCreatedByName;

        virtualCardFile.externalSource = new CardFileContentSource();
        virtualCardFile.externalSource.cardId = context.card.id;
        virtualCardFile.externalSource.cardTypeId = context.model.cardType.id!;
        virtualCardFile.externalSource.fileId = cycleGroupingFileInfo.fileId;
        virtualCardFile.externalSource.storeSource = cycleGroupingFileInfo.versionSource;
        virtualCardFile.externalSource.versionRowId = cycleGroupingFileInfo.versionId;

        virtualFile.info['KrCreated'] = TypedField.createDateTime(
          cycleGroupingFileInfo.versionCreated
        );
        virtualFile.info['KrCreatedByName'] = TypedField.createString(
          cycleGroupingFileInfo.versionCreatedByName
        );
        virtualFile.info['KrCycleID'] = TypedField.createInt(cycleGroupingFileInfo.cycle);
        virtualFile.info['KrCycleorder'] = TypedField.createInt(
          maxCycleNumber - cycleGroupingFileInfo.cycle
        );
        virtualFile.info['KrMaxCycleNumber'] = TypedField.createInt(maxCycleNumber);
      }
    }
  }
}
