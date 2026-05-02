import {
  Guid,
  FieldType,
  IStorage,
  TypedField,
  CancellationContext,
  CancellationError,
  EventHandler,
  RunGuard,
  debounce
} from '@tessa/core';
import {
  ICardMetadataColumn,
  ICardMetadataSection,
  CardTypeEntryControl,
  CardAutoCompleteSearchMode
} from '@tessa/platform';
import { FieldMapStorage } from 'tessa/cards/fieldMapStorage';
import { ITessaView } from 'tessa/views/viewExport';
import { ViewService } from 'tessa/views/viewService';
import { SelectedValue } from 'tessa/views/selectedValue';
import { ViewParameterMetadataSealed } from 'tessa/views/metadata/viewMetadataExport';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import { ICardModel } from 'tessa/ui/cards/interfaces';
import { IPropertyGridDataProvider } from 'tessa/ui/propertyGrid';
import { showViewsDialog } from 'tessa/ui/uiHost/showViewsDialog';
import {
  AutoCompleteValueEventArgs,
  AutoCompleteEntryDataSourceContext,
  IAutoCompleteItem,
  IAutoCompletePopupItem
} from 'tessa/ui/cards/controls';
import { AutoCompleteEntryDataSource } from 'tessa/ui/cards/controls/autoComplete/autoCompleteEntryDataSource';
import { OcrPropertyDataSource } from '../ocrPropertyDataSource';
import { IOcrReferencePropertySettings } from './ocrReferencePropertySettings';

/** Data source context for the recognized reference property. */
export class OcrReferencePropertyDataSourceContext extends AutoCompleteEntryDataSourceContext {
  //#region constructors

  /**
   * Creates an instance of {@link OcrReferencePropertyDataSourceContext} class.
   * @param args Constructor parameters for {@link AutoCompleteEntryDataSourceContext}.
   */
  constructor(...args: ConstructorParameters<typeof AutoCompleteEntryDataSourceContext>) {
    super(...args);
    // Initialize fields with an empty storage
    this.fields = new FieldMapStorage({});
  }

  //#endregion

  //#region properties

  /** Storage for the physical fields of the reference property. */
  readonly fields: FieldMapStorage;

  //#endregion
}

/** Data source for the recognized reference property. */
export class OcrReferencePropertyDataSource extends OcrPropertyDataSource {
  //#region constructors

  /**
   * Creates an instance of the {@link OcrReferencePropertyDataSource} class.
   * @param dataProvider The data provider for getting/setting the property value.
   * @param settings The settings for the property.
   */
  constructor(dataProvider: IPropertyGridDataProvider, settings: IOcrReferencePropertySettings) {
    super(dataProvider, settings);

    const model = settings.model!;
    const control = settings.control!;

    this.onItemSet = new EventHandler(this);
    this.onItemDelete = new EventHandler(this);
    this._cancellationContext = new CancellationContext();
    const metadataSection = this.getMetadataSection(control, model);
    const { field, fields } = this.getControlFields(control, metadataSection);
    const metadataColumn = this.getMetadataColumn(control, metadataSection, field);
    this._viewRefSection = this.getViewRefSectionFromSettings(settings, metadataColumn);
    this._viewRefInfo = this.getViewRefInfoFromMetadata(control, metadataSection, metadataColumn);
    const context = this.getContextFromSettings(settings, metadataSection, metadataColumn, fields);
    this._itemsSource = new AutoCompleteEntryDataSource(context);
  }

  //#endregion

  //#region fields

  private _runGuard = new RunGuard();
  private readonly _viewRefInfo: IStorage;
  private readonly _viewRefSection: ReadonlyArray<string>;
  private readonly _itemsSource: AutoCompleteEntryDataSource;
  private readonly _cancellationContext: CancellationContext;

  //#endregion

  //#region properties

  /**
   * Indicates if items are being loaded from the data source.
   * @default false
   */
  get itemsLoading(): boolean {
    return this._runGuard.active;
  }

  /** Event handler that is triggered when an item is set. */
  readonly onItemSet: EventHandler<AutoCompleteValueEventArgs<this>>;

  /** Event handler that is triggered when an item is deleted. */
  readonly onItemDelete: EventHandler<AutoCompleteValueEventArgs<this>>;

  //#endregion

  //#region base overrides

  override getValue(): string | null {
    return this.isChanged ? super.getValue() : (this.getItem()?.displayText ?? null);
  }

  override get(): { displayed: string | null; value: string | null } {
    return {
      displayed: this.getDisplayed(),
      value: this.convertNullableValue(this.getItemText())
    };
  }

  override set(displayed: string | null, value: string | null): void {
    const item = this.getItem();
    if (item) {
      const fields = this._itemsSource.dataSourceContext.fields.entries();
      const value = Object.fromEntries(fields);
      this.modifyData(displayed, value);
    } else {
      super.set(displayed, value);
    }
  }

  override clear(): void {
    super.clear();
    this.deleteItem();
  }

  override dispose(): void {
    this.cancelItemsLoading();
    this.onItemDelete.dispose();
    this.onItemSet.dispose();
    super.dispose();
  }

  //#endregion

  //#region public methods

  /**
   * Retrieves the current item from the source.
   * @returns The current item, or `null` if no item is available.
   */
  getItem(): IAutoCompleteItem | null {
    return this._itemsSource.getItem();
  }

  /**
   * Sets the current item in the source.
   * @param item The item to set, or `null` to clear the item.
   */
  setItem(item: IAutoCompletePopupItem | null): void {
    this._itemsSource.setItem(item);
  }

  /** Deletes the current item from the source. */
  deleteItem(): void {
    this._itemsSource.deleteItem();
  }

  /**
   * Retrieves the display text of the current item.
   * @returns The display text of the current item, or `null` if no item is available.
   */
  getItemText(): string | null {
    return this.getItem()?.displayText ?? null;
  }

  /**
   * Retrieves the reference of the current item.
   * @returns The reference of the current item if valid and not empty, or `null` otherwise.
   */
  getItemReference(): string | null {
    const reference = this.getItem()?.reference;
    return Guid.isValid(reference) && !Guid.isEmpty(reference) ? reference : null;
  }

  /**
   * Retrieves detailed information about the current item.
   * @returns An object containing detailed information about
   * the current item, or an empty object if no item is available.
   */
  getItemInfo(): IStorage {
    const item = this.getItem();
    return !!item
      ? {
          ID: TypedField.createGuid(item.reference),
          DisplayText: TypedField.createString(item.displayText),
          ColumnValues: item.columnValues.map(x => TypedField.createString(x)),
          ...this._viewRefInfo
        }
      : {};
  }

  /**
   * Opens a view dialog to select an item and returns the displayed value.
   * @returns A promise that resolves to the displayed value of the selected
   * item, or `null` if no item is selected or the dialog is canceled.
   */
  async selectItem(): Promise<string | null> {
    const parameters = null;
    let displayed: string | null = null;

    await showViewsDialog(
      this._viewRefSection,
      async selectedValue => {
        displayed = this.setSelectedItem(selectedValue);
      },
      parameters,
      async context => {
        displayed = this.setSelectedItem(context.selected[0]);
      }
    );

    return displayed;
  }

  /**
   * Finds items based on the specified filter and returns the text of the first found item.
   * @param filter The filter string to search for items.
   * @param delay The delay in milliseconds before starting the search.
   * @returns A promise that resolves to the text of the found item,
   * or `null` if no items are found or the search is canceled.
   */
  async findItem(filter = '', delay = 0): Promise<string | null> {
    this._cancellationContext.cancel();

    const guardValue = this._runGuard.use();

    try {
      if (delay > 0) {
        await this.debouncedDelay(delay);
        if (!this._runGuard.isActual(guardValue)) {
          return null;
        }
      }
      return await CancellationContext.create(this._cancellationContext).run(async () => {
        const cancellationToken = CancellationContext.currentToken;
        const items = await this._itemsSource.findItems(filter);
        if (cancellationToken.cancelled || !this._runGuard.isActual(guardValue)) {
          return null;
        }

        this.deleteItem();
        const item = !!items?.length ? items[0] : null;
        if (this._itemsSource.dataSourceContext.manualInput || item) {
          this.setItem(item);
        }

        return this.getItemText();
      });
    } catch (error) {
      if (!CancellationError.isCancellationError(error)) {
        throw error;
      }
    } finally {
      this._runGuard.resetIfActual(guardValue);
    }

    return null;
  }

  /** Cancels the loading items. */
  cancelItemsLoading(): void {
    this._cancellationContext.cancel();
    this._runGuard.reset();
  }

  /**
   * Checks for changes in the data source.
   * @param shallow A flag that determines the method of checking for changes.
   * If set to `true`, it checks for changes based only on the property key.
   * If set to `false` (default), it checks based on the value of {@link isChanged}.
   * @returns `true` if changes are detected, otherwise `false`.
   */
  hasChanges(shallow = false): boolean {
    return shallow ? this._propertyDataProvider.isChanged : this.isChanged;
  }

  //#endregion

  //#region private methods

  private getMetadataSection(
    control: CardTypeEntryControl,
    model: ICardModel
  ): ICardMetadataSection {
    if (!control.sectionId) {
      throw new Error(`Cannot find section id for control with caption '${control.caption}'.`);
    }

    const metadataSection = model.cardMetadata.sections.getSectionById(control.sectionId);
    if (!metadataSection) {
      throw new Error(`Cannot find metadata for section with id '${control.sectionId}'.`);
    }

    return metadataSection;
  }

  private getControlFields(
    control: CardTypeEntryControl,
    metadataSection: ICardMetadataSection
  ): { field: string; fields: string[] } {
    const { fieldNames, defaultFieldName } = control.getFieldNames(metadataSection);
    if (!fieldNames || fieldNames.length === 0 || !defaultFieldName) {
      throw new Error(`No columns are linked to control with caption '${control.caption}'.`);
    }

    return { field: defaultFieldName, fields: fieldNames };
  }

  private getMetadataColumn(
    control: CardTypeEntryControl,
    metadataSection: ICardMetadataSection,
    fieldName: string
  ): ICardMetadataColumn {
    let metadataColumn: ICardMetadataColumn | undefined;
    if (control.complexColumnId) {
      metadataColumn = metadataSection.getColumnById(control.complexColumnId!);
      if (!metadataColumn) {
        throw new Error(`Cannot find metadata for column with id '${control.complexColumnId!}'.`);
      }
    } else {
      metadataColumn = metadataSection.getColumnByName(fieldName);
      if (!metadataColumn) {
        throw new Error(`Cannot find metadata for column with name '${fieldName}'.`);
      }
    }

    return metadataColumn;
  }

  private getViewRefSectionFromSettings(
    settings: IStorage,
    metadataColumn: ICardMetadataColumn
  ): string[] {
    const refSection = tryGetFromSettings(settings, 'RefSection');
    return !!refSection
      ? typeof refSection === 'string'
        ? [refSection]
        : (refSection as string[])
      : [metadataColumn.referencedSection?.name ?? ''];
  }

  private getViewRefInfoFromMetadata(
    control: CardTypeEntryControl,
    metadataSection: ICardMetadataSection,
    metadataColumn: ICardMetadataColumn
  ): IStorage {
    return {
      ControlName: TypedField.create(control.name, FieldType.String),
      ReferenceSectionID: TypedField.createGuid(metadataSection.id),
      ReferenceSectionName: TypedField.createString(metadataSection.name),
      ReferenceColumnID: TypedField.createGuid(metadataColumn.id),
      ReferenceColumnName: TypedField.createString(metadataColumn.name)
    };
  }

  private getViewFromSettings(settings: IStorage): ITessaView {
    const viewAlias = tryGetFromSettings<string>(settings, 'ViewAlias', '');
    if (!viewAlias) {
      throw new Error('Can not find view alias at control settings.');
    }

    const view = ViewService.instance.getByName(viewAlias);
    if (!view) {
      throw new Error(`Can not find view with alias '${viewAlias}' at metadata.`);
    }

    return view;
  }

  private getViewComboBoxFromSettings(settings: IStorage): ITessaView | null {
    const comboBoxMode = tryGetFromSettings(settings, 'ComboBoxMode', false);
    const viewComboBoxAlias = tryGetFromSettings<string>(settings, 'ViewAliasComboBox', '');

    if (comboBoxMode && viewComboBoxAlias) {
      const view = ViewService.instance.getByName(viewComboBoxAlias);
      if (!view) {
        throw new Error(`Can not find view with alias '${viewComboBoxAlias}' at metadata.`);
      }
      return view;
    }

    return null;
  }

  private getViewParameterFromSettings(
    settings: IStorage,
    view: ITessaView
  ): ViewParameterMetadataSealed {
    const parameterName = tryGetFromSettings<string>(settings, 'ParameterAlias', '');
    if (!parameterName) {
      throw new Error('Can not find view parameter at control settings.');
    }

    const viewParameter = view?.metadata.parameters.get(parameterName);
    if (!viewParameter) {
      throw new Error(`Can not find view parameter '${parameterName}' at metadata.`);
    }

    return viewParameter;
  }

  private getViewReferencePrefixFromSettings(settings: IStorage): string {
    const viewRefPrefix = tryGetFromSettings<string>(settings, 'ViewReferencePrefix', undefined);
    if (viewRefPrefix == undefined) {
      throw new Error('Can not find view reference prefix at control settings.');
    }

    return viewRefPrefix;
  }

  private getContextFromSettings(
    settings: IOcrReferencePropertySettings,
    metadataSection: ICardMetadataSection,
    metadataColumn: ICardMetadataColumn,
    fieldNames: string[]
  ): OcrReferencePropertyDataSourceContext {
    const viewMapping = null;
    const view = this.getViewFromSettings(settings);
    const viewComboBox = this.getViewComboBoxFromSettings(settings);
    const viewParameter = this.getViewParameterFromSettings(settings, view);
    const viewReferencePrefix = this.getViewReferencePrefixFromSettings(settings);

    const context = new OcrReferencePropertyDataSourceContext(
      (item, fields) => this.onItemSet.invoke(new AutoCompleteValueEventArgs(item, this, fields)),
      item => this.onItemDelete.invoke(new AutoCompleteValueEventArgs(item, this)),
      settings.model!,
      metadataSection,
      metadataColumn,
      this._viewRefSection,
      view,
      viewParameter.alias,
      viewReferencePrefix || metadataColumn.name,
      viewMapping,
      viewComboBox,
      tryGetFromSettings<string | null>(settings, 'PopupColumnsIndexes', null),
      tryGetFromSettings<string | null>(settings, 'PopupColumnsIndexesComboBox', null),
      tryGetFromSettings<string | null>(settings, 'PopupColumnLengths', null),
      tryGetFromSettings<string | null>(settings, 'PopupColumnLengthsComboBox', null),
      settings.control!.displayFormat,
      tryGetFromSettings<number>(settings, 'MaxResultsCount', 15),
      tryGetFromSettings<number>(settings, 'PopupScreenLengthPercent', 100),
      tryGetFromSettings<boolean>(settings, 'ManualInput', false),
      tryGetFromSettings<string | null>(settings, 'ManualInputColumnID', null),
      tryGetFromSettings<number | null>(settings, 'SearchDelay', 125),
      tryGetFromSettings<boolean>(settings, 'ExtendendLocalization', false),
      tryGetFromSettings<number>(settings, 'SearchMode', CardAutoCompleteSearchMode.Contains)
    );

    const fields = settings.model!.card.sections.tryGet(metadataSection.name)?.fields;
    for (const fieldName of fieldNames) {
      const field = fields?.tryGetField(fieldName);
      context.fields.set(fieldName, field ?? null);
    }

    return context;
  }

  private setSelectedItem(value?: SelectedValue | null): string | null {
    if (value) {
      this._itemsSource.setItemFromViews(value);
      return this.getItemText() ?? value.displayText;
    }
    return null;
  }

  private debouncedDelay = (() => {
    let promise: Promise<void> | null = null;
    let delayedResolve: VoidFunction | null = null;

    return (delay: number) => {
      promise ??= new Promise(resolve => {
        delayedResolve = debounce(() => {
          resolve();
          promise = null;
          delayedResolve = null;
        }, delay);
      });

      delayedResolve?.();
      return promise;
    };
  })();

  //#endregion
}
