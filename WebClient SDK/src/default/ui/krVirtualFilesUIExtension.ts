import { FieldType, IStorage, StorageHelper, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import {
  CardRowStateChangedEventArgs,
  CardSectionType,
  CardTableType,
  ICardService,
  ICardService$,
  IConditionTypesProvider,
  IConditionTypesProvider$
} from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ConditionsUIContext } from 'tessa/ui/conditions';
import { ButtonViewModel } from 'tessa/ui/cards/controls';
import { UIContext } from 'tessa/ui';

@extension({ name: 'KrVirtualFilesUIExtension' })
export class KrVirtualFilesUIExtension extends CardUIExtension {
  //#region ctor

  constructor(
    @ICardService$() private readonly _cardService: ICardService,
    @IConditionTypesProvider$() private readonly _conditionTypesProvider: IConditionTypesProvider
  ) {
    super();
  }

  //#endregion

  //#region CardUIExtension

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    const cardModel = context.model;
    const card = cardModel.card;

    const conditionContext = new ConditionsUIContext(
      this._cardService,
      this._conditionTypesProvider
    );
    await conditionContext.initialize(context.model);

    const virtualSection = card.sections.getOrAdd('KrVirtualFileVersions');
    virtualSection.type = CardSectionType.Table;
    virtualSection.tableType = CardTableType.Collection;

    virtualSection.rows.collectionChanged.add(e => {
      if (e.added.length > 0) {
        for (const item of e.added) {
          item.stateChanged.add(this.rowStateChanged);
        }
      }
    });

    const button = cardModel.controls.get('CompileButton') as ButtonViewModel;
    if (button) {
      button.onClick = this.compile;
    }
  }

  //#endregion

  //#region methods

  private rowStateChanged = (e: CardRowStateChangedEventArgs) => {
    e.row.set('FileVersionID', e.row.rowId, FieldType.Guid);
    e.row.stateChanged.remove(this.rowStateChanged);
  };

  private compile = async () => {
    const editor = UIContext.current.cardEditor!;
    const model = editor.cardModel!;

    if (await model.hasChanges()) {
      const success = await editor.saveCard(UIContext.current);
      if (!success) {
        return;
      }
    }

    const storeInfo: IStorage = {};
    storeInfo[StorageHelper.systemKeyPrefix + 'Compile'] = TypedField.trueBoolean;

    await editor.saveCard(UIContext.current, storeInfo);
  };

  //#endregion
}
