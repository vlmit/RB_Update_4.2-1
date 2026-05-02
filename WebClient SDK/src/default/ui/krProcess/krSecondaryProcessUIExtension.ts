import { FieldType } from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardRowState,
  ICardService,
  ICardService$,
  IConditionTypesProvider,
  IConditionTypesProvider$
} from '@tessa/platform';
import { Visibility } from 'tessa/platform/visibility';
import { CardUIExtension, ICardModel, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ConditionsUIContext } from 'tessa/ui/conditions';

enum KrSecondaryProcessMode {
  PureProcess = 0,
  Button = 1,
  Action = 2
}

@extension({ name: 'KrSecondaryProcessUIExtension' })
export class KrSecondaryProcessUIExtension extends CardUIExtension {
  //#region ctor

  constructor(
    @ICardService$() private readonly _cardService: ICardService,
    @IConditionTypesProvider$() private readonly _conditionTypesProvider: IConditionTypesProvider
  ) {
    super();
  }

  //#endregion

  //#region fields

  private _model: ICardModel;

  private _dispose: Function | null;

  //#endregion

  //#region CardUIExtension

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    this._model = context.model;

    await this.initializeConditions();

    const currentMode = this.getCurrentMode();
    this.updateVisibility(currentMode);
    if (currentMode === KrSecondaryProcessMode.PureProcess) {
      this.updateCheckRestrictions();
    }

    this._dispose = context.card.sections
      .get('KrSecondaryProcesses')!
      .fields.fieldChanged.add(e => {
        switch (e.fieldName) {
          case 'ModeID':
            this.updateVisibility(this.getCurrentMode());
            break;

          case 'AllowClientSideLaunch':
            this.updateVisibilityForPureProcessMode();
            break;

          case 'CheckRecalcRestrictions':
            this.updateCheckRestrictions();
            break;

          case 'IsGlobal':
            this.updateIsGlobal();
            break;
        }
      });
  }

  override async finalized(): Promise<void> {
    if (this._dispose) {
      this._dispose();
      this._dispose = null;
    }
  }

  //#endregion

  //#region methods

  private updateVisibility(currentMode: number) {
    const getVisibility = (allowedMode: number, allowedMode2: number = -2) => {
      return allowedMode === currentMode || allowedMode2 === currentMode
        ? Visibility.Visible
        : Visibility.Collapsed;
    };

    const blocks = this._model.blocks;
    const buttonParametersVisibility = getVisibility(KrSecondaryProcessMode.Button);
    blocks.get('PureProcessParametersBlock')!.blockVisibility = getVisibility(
      KrSecondaryProcessMode.PureProcess
    );
    blocks.get('TileParametersBlock')!.blockVisibility = buttonParametersVisibility;
    blocks.get('LocalTileParametersBlock')!.blockVisibility = buttonParametersVisibility;
    blocks.get('ActionParametersBlock')!.blockVisibility = getVisibility(
      KrSecondaryProcessMode.Action
    );
    blocks.get('VisibilityScriptsBlock')!.blockVisibility = buttonParametersVisibility;
    blocks.get('ConditionsTable')!.blockVisibility = buttonParametersVisibility;

    blocks.get('RestictionsBlock')!.blockVisibility = Visibility.Visible;
    blocks.get('ExecutionAccessDeniedBlock')!.blockVisibility = Visibility.Visible;
    blocks.get('ExecutionScriptsBlock')!.blockVisibility = Visibility.Visible;

    if (buttonParametersVisibility === Visibility.Visible) {
      this.updateIsGlobal();
    }

    if (currentMode === KrSecondaryProcessMode.PureProcess) {
      this.updateVisibilityForPureProcessMode();
    }
  }

  private updateVisibilityForPureProcessMode() {
    const card = this._model.card;
    const sec = card.sections.get('KrSecondaryProcesses');
    const allowClientSideLaunch = sec.fields.tryGet('AllowClientSideLaunch') || false;
    const checkRecalcControl = this._model.blocks
      .get('PureProcessParametersBlock')!
      .controls.find(x => x.name === 'CheckRecalcRestrictionsCheckbox')!;

    checkRecalcControl.isReadOnly = !!allowClientSideLaunch;
    const checkRecalcRestrictions = sec.fields.tryGet('CheckRecalcRestrictions') || false;
    if (allowClientSideLaunch && !checkRecalcRestrictions) {
      sec.fields.set('CheckRecalcRestrictions', true, FieldType.Boolean);
    } else if (!allowClientSideLaunch && !checkRecalcRestrictions) {
      this.updateCheckRestrictions();
    }
  }

  private updateCheckRestrictions() {
    const blocks = this._model.blocks;
    const card = this._model.card;
    const checkRecalcRestrictions =
      card.sections.get('KrSecondaryProcesses').fields.tryGet('CheckRecalcRestrictions') || false;
    const visibilityForRestrictionFields = checkRecalcRestrictions
      ? Visibility.Visible
      : Visibility.Collapsed;
    blocks.get('RestictionsBlock')!.blockVisibility = visibilityForRestrictionFields;
    blocks.get('ExecutionAccessDeniedBlock')!.blockVisibility = visibilityForRestrictionFields;
    blocks.get('ExecutionScriptsBlock')!.blockVisibility = visibilityForRestrictionFields;

    if (!checkRecalcRestrictions) {
      const sec = card.sections.get('KrSecondaryProcesses');
      sec.fields.set('ExecutionAccessDeniedMessage', null);
      sec.fields.set('ExecutionSqlCondition', null);
      sec.fields.set('ExecutionSourceCondition', null);

      const clear = (name: string) => {
        const rows = card.sections.get(name).rows;
        const removeRows = rows.filter(x => x.state === CardRowState.Inserted);
        for (const row of removeRows) {
          rows.remove(row);
        }
        for (const row of rows) {
          row.state = CardRowState.Deleted;
        }
      };

      clear('KrStageDocStates');
      clear('KrStageTypes');
      clear('KrStageRoles');
      clear('KrSecondaryProcessRoles');
    }
  }

  private updateIsGlobal() {
    const card = this._model.card;
    const section = card.sections.get('KrSecondaryProcesses');
    const blocks = this._model.blocks;
    const isGlobal = section.fields.tryGet('IsGlobal') || false;
    blocks.get('LocalTileParametersBlock')!.blockVisibility = isGlobal
      ? Visibility.Collapsed
      : Visibility.Visible;
  }

  private getCurrentMode(): number {
    const modeId = this._model.card.sections
      .get('KrSecondaryProcesses')!
      .fields.tryGet<number>('ModeID');
    return modeId == undefined ? -1 : modeId;
  }

  private async initializeConditions() {
    const conditionContext = new ConditionsUIContext(
      this._cardService,
      this._conditionTypesProvider
    );
    await conditionContext.initialize(this._model);
  }

  //#endregion
}
