import { localize } from '@tessa/application';
import { computed, observable, runInAction } from 'mobx';
import { ViewModelBase } from '@tessa/ui';
import { AutocompleteDataSource, AutocompleteViewModel, IAutocompleteItem } from 'ui/autocomplete';

export interface StampPlace {
  RowID: string;
  Name: string;
  Stamps: {
    Name: string;
  }[];
}

export class StampPlaceSelectionDialogViewModel extends ViewModelBase {
  //#region ctor

  constructor(stampPlaces: StampPlace[]) {
    super();

    this.stampPlaces = stampPlaces;

    this.autocompleteFieldCaption = localize('$UI_FacsimileAndAnnotationsGroup_SelectStampPlace');
    this.autocompleteFieldPlaceholder = !stampPlaces.length
      ? localize('$UI_FacsimileAndAnnotationsGroup_NoAvailableStampPlaces')
      : '';
    this.chromeTitle = localize('$UI_FacsimileAndAnnotationsGroup_StampPlaceSelectionDialogTitle');

    this.initAutocompleteViewModel();
  }

  //#endregion

  //#region fields

  @observable
  private _isInvalid = false;

  private _autocomplete: AutocompleteViewModel;

  //#endregion

  //#region props

  get autocomplete(): AutocompleteViewModel {
    return this._autocomplete;
  }

  public readonly stampPlaces: ReadonlyArray<StampPlace>;

  public autocompleteFieldCaption: string;

  public autocompleteFieldPlaceholder: string;

  public chromeTitle: string;

  @computed
  public get selectedStampPlace(): StampPlace | null {
    return this.stampPlaces.find(x => x.RowID == this.autocomplete.records?.[0].model.id) ?? null;
  }

  @computed
  public get confirmationButtonIsDisabled(): boolean {
    return !this.autocomplete.records.length;
  }

  @computed
  public get confirmationButtonCaption(): string {
    return this.confirmationButtonIsDisabled
      ? '$UI_Controls_FilesControl_NoStampPlace'
      : '$UI_Controls_FilesControl_Done';
  }

  //#endregion

  //#region methods

  private async initAutocompleteViewModel(): Promise<void> {
    const dataSource = new AutocompleteDataSource({
      unique: true,
      multiple: false,
      layoutFactory: async () => [],
      itemsFactory: async (_, filter, __) => {
        const stampPlaces = filter
          ? this.stampPlaces.filter(stampPlace =>
              stampPlace.Name.toLowerCase().includes(filter.toLowerCase())
            )
          : this.stampPlaces;

        const dropdownItems: IAutocompleteItem[] = stampPlaces.map(item => {
          return {
            id: item.RowID,
            name: item.Name
          };
        });

        return dropdownItems;
      }
    });

    this._autocomplete = new AutocompleteViewModel(dataSource);
    await this._autocomplete.initialize();
  }

  public get isInvalid(): boolean {
    return this._isInvalid;
  }
  public set isInvalid(value: boolean) {
    runInAction(() => {
      this._isInvalid = value;
    });
  }

  //#endregion
}
