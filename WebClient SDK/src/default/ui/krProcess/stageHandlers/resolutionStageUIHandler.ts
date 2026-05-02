import {
  FieldStorageMap,
  FieldStorageMapChangedEventArgs,
  FieldType,
  Guid,
  ListChangedEventArgs,
  StorageArray,
  ValidationResult,
  ValidationResultType
} from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardRow,
  CardRowState,
  CardRowStateChangedEventArgs,
  KrStageTypeFormattingHelper,
  StageTypeDescriptor,
  StageTypeDescriptors,
  WorkflowCardTypes
} from '@tessa/platform';
import { Visibility } from 'tessa/platform';
import { IKrStageTypeUIHandlerContext, KrStageTypeUIHandler } from 'tessa/ui/workflow/krProcess';
import { IControlViewModel } from 'tessa/ui/cards';

/**
 * UI обработчик типа этапа {@link StageTypeDescriptors.resolutionDescriptor}.
 */
@extension({ name: 'ResolutionStageUIHandler' })
export class ResolutionStageUIHandler extends KrStageTypeUIHandler {
  //#region fields

  private static readonly _krResolutionSettingsVirtual = 'KrResolutionSettingsVirtual';

  private static readonly _krPerformersVirtual = 'KrPerformersVirtual';

  private static readonly _controllerId = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'ControllerID'
  );

  private static readonly _controllerName = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'ControllerName'
  );

  private static readonly _planned = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'Planned'
  );

  private static readonly _durationInDays = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'DurationInDays'
  );

  private static readonly _withControl = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'WithControl'
  );

  private static readonly _massCreation = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'MassCreation'
  );

  private static readonly _majorPerformer = KrStageTypeFormattingHelper.formatPlainColumnName(
    ResolutionStageUIHandler._krResolutionSettingsVirtual,
    'MajorPerformer'
  );

  private static readonly _krPerformersVirtualSynthetic =
    KrStageTypeFormattingHelper.formatSectionName(ResolutionStageUIHandler._krPerformersVirtual);

  private _settings: CardRow | null;
  private _performers: StorageArray<CardRow> | null;
  private _controller: IControlViewModel | null;
  private _subscribedTo: Set<CardRow> = new Set();

  //#endregion

  //#region handlers

  descriptors(): ReadonlyArray<StageTypeDescriptor> {
    return [StageTypeDescriptors.resolutionDescriptor];
  }

  async initialize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    this._settings = context.row;
    this._settings.fieldChanged.add(this.onSettingsFieldChanged);

    this._performers = context.cardModel.card.sections.get(
      ResolutionStageUIHandler._krPerformersVirtualSynthetic
    ).rows;
    this._performers.collectionChanged.add(this.onPerformersChanged);

    for (const performer of this._performers) {
      if (this.alivePerformer(performer)) {
        this._subscribedTo.add(performer);
        performer.stateChanged.add(this.onPerformerStateChanged);
      }
    }

    this._controller =
      context.settingsForms
        .find(i => i.name === WorkflowCardTypes.KrResolutionStageTypeSettingsTypeName)
        ?.blocks.find(i => i.name === 'MainInfo')
        ?.controls.find(i => i.name === 'Controller') ?? null;

    if (this._controller && this._settings.get(ResolutionStageUIHandler._withControl) === true) {
      this._controller.controlVisibility = Visibility.Visible;
    }
  }

  async finalize(context: IKrStageTypeUIHandlerContext): Promise<void> {
    if (this._settings) {
      if (
        this._settings.tryGet(ResolutionStageUIHandler._durationInDays) == null &&
        !this._settings.tryGet(ResolutionStageUIHandler._planned)
      ) {
        context.validationResult.add(
          ValidationResult.fromText(
            '$WfResolution_Error_ResolutionHasNoPlannedDate',
            ValidationResultType.Warning
          )
        );
      }

      this._settings.fieldChanged.remove(this.onSettingsFieldChanged);
      this._settings = null;
    }

    if (this._performers) {
      this._performers.collectionChanged.remove(this.onPerformersChanged);
      this._performers = null;
    }

    for (const performer of this._subscribedTo) {
      performer.stateChanged.remove(this.onPerformerStateChanged);
    }

    this._subscribedTo.clear();

    this._controller = null;
  }

  //#endregion

  //#region private methods

  private readonly onSettingsFieldChanged = (
    e: FieldStorageMapChangedEventArgs,
    s: FieldStorageMap
  ): void => {
    if (e.fieldName === ResolutionStageUIHandler._planned) {
      if (e.fieldValue) {
        s.set(ResolutionStageUIHandler._durationInDays, null);
      }
    } else if (e.fieldName === ResolutionStageUIHandler._durationInDays) {
      if (e.fieldValue) {
        s.set(ResolutionStageUIHandler._planned, null);
      }
    } else if (e.fieldName === ResolutionStageUIHandler._withControl) {
      let visibility = Visibility.Collapsed;

      if (e.fieldValue === true) {
        visibility = Visibility.Visible;
      } else {
        s.set(ResolutionStageUIHandler._controllerId, null);
        s.set(ResolutionStageUIHandler._controllerName, null);
      }

      if (this._controller) {
        this._controller.controlVisibility = visibility;
      }
    } else if (e.fieldName === ResolutionStageUIHandler._massCreation && e.fieldValue === false) {
      s.set(ResolutionStageUIHandler._majorPerformer, false, FieldType.Boolean);
    }
  };

  private readonly onPerformerStateChanged = (e: CardRowStateChangedEventArgs) => {
    if (e.newState === CardRowState.Deleted) {
      this.performersChanged(CardRowState.Deleted, e.row);
    }

    if (e.oldState === CardRowState.Deleted) {
      this.performersChanged(CardRowState.Inserted, e.row);
    }
  };

  private readonly onPerformersChanged = (e: ListChangedEventArgs<CardRow>): void => {
    for (const performer of e.added) {
      this.performersChanged(CardRowState.Inserted, performer);
    }

    for (const performer of e.removed) {
      this.performersChanged(CardRowState.Deleted, performer);
    }
  };

  private performersChanged(action: CardRowState, performer: CardRow): void {
    if (!this._performers) {
      return;
    }

    if (action === CardRowState.Inserted) {
      if (!this._subscribedTo.has(performer)) {
        this._subscribedTo.add(performer);
        performer.stateChanged.add(this.onPerformerStateChanged);
      }

      // Действия могут производиться только в текущем диалоге, а значит,
      // всякий новодобавленный оказывается в текущем этапе. По этой причине
      // требуется наличие лишь одного исполняющего в таблице. Второй уже
      // добавлен, но его связь и прочие поля будут указаны позже.
      if (this._performers.filter(x => this.alivePerformer(x)).length >= 2) {
        this.enableMassCreation(true);
      }
    } else if (action === CardRowState.Deleted) {
      this._subscribedTo.delete(performer);
      performer.stateChanged.remove(this.onPerformerStateChanged);

      if (this._performers.filter(x => this.alivePerformer(x)).length < 2) {
        this.enableMassCreation(false);
      }
    }
  }

  private alivePerformer(performer: CardRow): boolean {
    if (!this._settings || performer.state === CardRowState.Deleted) {
      return false;
    }

    return Guid.equals(performer.tryGet('StageRowID'), this._settings.rowId);
  }

  private enableMassCreation(value: boolean) {
    if (this._settings) {
      this._settings.set(ResolutionStageUIHandler._massCreation, value, FieldType.Boolean);
    }
  }

  //#endregion
}
