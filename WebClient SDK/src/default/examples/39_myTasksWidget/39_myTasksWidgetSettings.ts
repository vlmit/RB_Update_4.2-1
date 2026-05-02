import { IStorage, StorageSerializableContext } from '@tessa/core';
import { observable, runInAction } from 'mobx';
import { ButtonWidgetSettings } from '../../dashboard/widgets/button/buttonWidgetSettings';
import { TaskTypeReference } from './39_taskTypeReference';

export class MyTasksWidgetSettings extends ButtonWidgetSettings {
  //#region keys

  /** @category Static Keys */
  static readonly taskTypeKey = 'TaskType';

  //#endregion

  //#region fields

  @observable.ref
  private _taskType: TaskTypeReference | null = null;

  //#endregion

  //#region properties

  get taskType(): TaskTypeReference | null {
    return this._taskType;
  }
  set taskType(value: TaskTypeReference | null) {
    runInAction(() => {
      this._taskType = value;
    });
  }

  //#endregion

  //#region IStorageSerializable implementation

  protected override serializeToStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.set(MyTasksWidgetSettings.taskTypeKey, this.taskType?.serializeToStorage({}, context));

    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.taskType = sa.tryGetObject(MyTasksWidgetSettings.taskTypeKey, storage =>
      new TaskTypeReference().deserializeFromStorage(storage)
    );
  }

  //#endregion
}
