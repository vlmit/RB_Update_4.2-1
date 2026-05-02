import Moment from 'moment';
import { Guid, IStorage, StorageSerializableContext, StorageSerializableObject } from '@tessa/core';
import { CardFileSourceType } from '@tessa/platform';

/**
 * Информация о файле и цикле согласования.
 */
export class CycleGroupingFileInfo extends StorageSerializableObject {
  //#region Properties

  /**
   * Номер цикла согласования.
   */
  cycle: number;

  /**
   * Идентификатор файла.
   */
  fileId: string;

  /**
   * Идентификатор версии файла.
   */
  versionId: string;

  /**
   * Номер версии файла.
   */
  versionNumber: number;

  /**
   * Размер версии файла.
   */
  versionSize: number;

  /**
   * {@link CardFileSourceType}
   */
  versionSource: CardFileSourceType;

  /**
   * Дата и время создания версии файла.
   */
  versionCreated: string;

  /**
   * Идентификатор пользователя, создавшего версию (изменившего файл).
   */
  versionCreatedById: string;

  /**
   * Имя пользователя, создавшего версию (изменившего файл).
   */
  versionCreatedByName: string;

  //#endregion

  //#region Base Overrides

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    this.getStorageAccessor(storage, context)
      .setInt('Cycle', this.cycle)
      .setGuid('FileID', this.fileId)
      .setGuid('VersionID', this.versionId)
      .setInt('VersionNumber', this.versionNumber)
      .setLong('VersionSize', this.versionSize)
      .setInt('VersionSource', this.versionSource)
      .setDateTime('VersionCreated', this.versionCreated)
      .setGuid('VersionCreatedByID', this.versionCreatedById)
      .setString('VersionCreatedByName', this.versionCreatedByName)
      .removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.cycle = sa.tryGetInt('Cycle') ?? 0;
    this.fileId = sa.tryGetGuid('FileID') ?? Guid.empty;
    this.versionId = sa.tryGetGuid('VersionID') ?? Guid.empty;
    this.versionNumber = sa.tryGetInt('VersionNumber') ?? 0;
    this.versionSize = sa.tryGetLong('VersionSize') ?? 0;
    this.versionSource = sa.tryGetInt('VersionSource') ?? 0;
    this.versionCreated = sa.tryGetDateTime('VersionCreated') ?? Moment.min().format();
    this.versionCreatedById = sa.tryGetGuid('VersionCreatedByID') ?? Guid.empty;
    this.versionCreatedByName = sa.tryGetString('VersionCreatedByName') ?? '';
  }

  //#endregion
}
