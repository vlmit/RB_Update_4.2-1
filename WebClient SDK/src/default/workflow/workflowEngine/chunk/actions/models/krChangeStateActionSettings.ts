import { FieldType, IStorage } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import {
  WorkflowActionSettingsStorageBase,
  WorkflowActionStateStorage,
  WorkflowActionStorage
} from 'tessa/ui/workflow/chunk';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow/types';

/** Параметры действия "Смена состояния". */
export class KrChangeStateActionSettings extends WorkflowActionSettingsStorageBase<KrChangeStateActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly weChangeStateActionSectionName = 'KrChangeStateAction';

  /** @category Static Keys */
  static readonly stateKey = 'State';

  /** @category Static Keys */
  static readonly stateIdKey = 'ID';

  /** @category Static Keys */
  static readonly stateNameKey = 'Name';

  //#endregion

  //#region ctor

  constructor(
    action: WorkflowActionStorage,
    actionState?: WorkflowActionStateStorage,
    storage: IStorage = {}
  ) {
    super(action, actionState, storage);
  }

  //#endregion

  //#region props

  get state(): IReadOnlyKeyValuePair<number, string | null> | null {
    return (
      this.tryGetSubObject(
        KrChangeStateActionSettings.weChangeStateActionSectionName
      )?.tryGetKeyPair(
        KrChangeStateActionSettings.stateKey,
        KrChangeStateActionSettings.stateIdKey,
        KrChangeStateActionSettings.stateNameKey
      ) ?? null
    );
  }
  @undoredo()
  set state(value: IReadOnlyKeyValuePair<number, string | null> | null) {
    this.getSubObject(KrChangeStateActionSettings.weChangeStateActionSectionName).setBindingKeyPair(
      KrChangeStateActionSettings.stateKey,
      KrChangeStateActionSettings.stateIdKey,
      KrChangeStateActionSettings.stateNameKey,
      value,
      FieldType.Int
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrChangeStateActionSettings {
    return new KrChangeStateActionSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
