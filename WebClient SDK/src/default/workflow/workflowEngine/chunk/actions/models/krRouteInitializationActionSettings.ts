import { FieldType, IStorage } from '@tessa/core';
import { undoredo } from '@tessa/platform';
import { IReadOnlyKeyValuePair } from 'tessa/ui/workflow';
import { WorkflowActionSettingsStorageBase } from 'tessa/ui/workflow/chunk';

/** Параметры действия "Инициализация маршрута". */
export class KrRouteInitializationActionSettings extends WorkflowActionSettingsStorageBase<KrRouteInitializationActionSettings> {
  //#region keys

  /** @category Static Keys */
  static readonly krRouteInitializationActionVirtualSectionName =
    'KrRouteInitializationActionVirtual';

  /** @category Static Keys */
  static readonly initiatorKey = 'Initiator';

  /** @category Static Keys */
  static readonly initiatorIdKey = 'ID';

  /** @category Static Keys */
  static readonly initiatorNameKey = 'Name';

  /** @category Static Keys */
  static readonly initiatorCommentKey = 'InitiatorComment';

  //#endregion

  //#region props

  get initiator(): IReadOnlyKeyValuePair<string, string | null> | null {
    return (
      this.tryGetSubObject(
        KrRouteInitializationActionSettings.krRouteInitializationActionVirtualSectionName
      )?.tryGetKeyPair(
        KrRouteInitializationActionSettings.initiatorKey,
        KrRouteInitializationActionSettings.initiatorIdKey,
        KrRouteInitializationActionSettings.initiatorNameKey
      ) ?? null
    );
  }
  @undoredo()
  set initiator(value: IReadOnlyKeyValuePair<string, string | null> | null) {
    this.getSubObject(
      KrRouteInitializationActionSettings.krRouteInitializationActionVirtualSectionName
    ).setBindingKeyPair(
      KrRouteInitializationActionSettings.initiatorKey,
      KrRouteInitializationActionSettings.initiatorIdKey,
      KrRouteInitializationActionSettings.initiatorNameKey,
      value
    );
  }

  get initiatorComment(): string | null {
    return (
      this.tryGetSubObject(
        KrRouteInitializationActionSettings.krRouteInitializationActionVirtualSectionName
      )?.tryGetValue(KrRouteInitializationActionSettings.initiatorCommentKey) ?? null
    );
  }
  @undoredo()
  set initiatorComment(value: string | null) {
    this.getSubObject(
      KrRouteInitializationActionSettings.krRouteInitializationActionVirtualSectionName
    ).setBindingField(
      KrRouteInitializationActionSettings.initiatorCommentKey,
      value,
      FieldType.String
    );
  }

  //#endregion

  //#region base overrides

  protected override currentObjectFactory(storage: IStorage): KrRouteInitializationActionSettings {
    return new KrRouteInitializationActionSettings(this.action, this.actionState, storage);
  }

  //#endregion
}
