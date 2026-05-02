import { CancellationToken, IStorage } from '@tessa/core';
import { localize } from '@tessa/application';
import {
  AutocompleteMode,
  IAutocompleteDataContext,
  IAutocompleteItem,
  IAutocompleteItemLayout
} from 'ui/autocomplete';
import { showCardTypeSelectorDialog } from './cardTypeSelector/cardTypeSelectorDialog';
import { CardTypeSelectorViewModel } from './cardTypeSelector/cardTypeSelectorViewModel';
import {
  ICardTypeSelectorCardType,
  ICardTypeSelectorTypesProvider
} from './cardTypeSelector/cardTypeSelectorTypes';

export class AutocompleteDataCardTypeSelectorContext implements IAutocompleteDataContext {
  //#region fields

  private readonly _typesProvider: ICardTypeSelectorTypesProvider;
  private _itemsLayout: IAutocompleteItemLayout[] = [];

  //#endregion

  //#region props

  get unique(): boolean {
    return true;
  }

  get multiple(): boolean {
    return false;
  }

  //#endregion

  constructor(typesProvider: ICardTypeSelectorTypesProvider) {
    this._typesProvider = typesProvider;
  }

  //#region methods

  async initialize(): Promise<void> {
    this._itemsLayout = [{ alias: 'name' }];
  }

  async itemsFactory(
    _mode: AutocompleteMode,
    filter: string | null,
    _cancellationToken?: CancellationToken | undefined
  ): Promise<readonly IAutocompleteItem[]> {
    let types = (await this._typesProvider.getTypes()).map(group => group.types).flat();

    if (filter) {
      types = types.filter(type =>
        localize(type.docTypeTitle ?? type.cardTypeCaption)
          .toLowerCase()
          .includes(filter.toLowerCase())
      );
    }

    return types.map(type => this.convertToAutocompleteItem(type));
  }

  async layoutFactory(
    _mode: AutocompleteMode,
    _items: readonly IAutocompleteItem[],
    _cancellationToken?: CancellationToken | undefined
  ): Promise<readonly IAutocompleteItemLayout[]> {
    return this._itemsLayout;
  }

  canSelectItems(): boolean {
    return true;
  }

  async selectItems(): Promise<readonly IAutocompleteItem[]> {
    const viewModel = new CardTypeSelectorViewModel(this._typesProvider);
    await viewModel.initialize();

    const selected = await showCardTypeSelectorDialog(viewModel);
    const selectedNode = viewModel.selectedNode;

    if (selected && !!selectedNode) {
      const item = this.convertToAutocompleteItem(selectedNode.cardType);
      viewModel.dispose();

      return [item];
    }

    viewModel.dispose();
    return [];
  }

  private convertToAutocompleteItem(cardType: ICardTypeSelectorCardType): IAutocompleteItem {
    const data: IStorage = {
      cardTypeName: cardType.cardTypeName,
      cardTypeCaption: cardType.cardTypeCaption
    };

    if (cardType.docTypeId) {
      data.cardTypeId = cardType.cardTypeId;
      data.documentTypeTitle = cardType.docTypeTitle;
    }

    return {
      id: cardType.docTypeId ?? cardType.cardTypeId,
      name: localize(cardType.docTypeTitle ?? cardType.cardTypeCaption),
      data
    };
  }

  //#endregion
}
