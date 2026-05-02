import { IStorage, StorageHelper, StorageSerializable } from '@tessa/core';
import { IViewMetadata, SortDirection } from '@tessa/platform';
import { tagColumnName } from 'components/cardElements/grid';
import { ViewControlViewModelBase } from 'tessa/ui/cards/controls';
import { TableColumnSettingsDataProviderBase } from 'tessa/ui/views/settings';
import {
  ColumnSettings,
  ColumnsSettings,
  SortingSettings
} from 'tessa/ui/views/settings/columnSettings';
import { ColumnSettingsStorage } from './columnSettingsStorage';
import { SortingColumnStorage } from './sortingColumnStorage';

export class ViewWidgetTableColumnSettingsProvider extends TableColumnSettingsDataProviderBase<ColumnSettingsStorage> {
  //#region private static content keys

  private static settingsKey = 'Settings';

  //#endregion

  //#region ctor

  constructor(
    private _getView: () => ViewControlViewModelBase,
    public widgetContent: IStorage,
    private _onStoreSettings?: () => Promise<void>
  ) {
    super();

    this.widgetContent = StorageHelper.tryClone(widgetContent) ?? {};
  }

  //#endregion

  //#region private props

  private _viewControl: ViewControlViewModelBase | null;

  private get viewControl(): ViewControlViewModelBase {
    if (!this._viewControl) {
      this._viewControl = this._getView();
    }

    return this._viewControl;
  }

  private get viewMetadata(): IViewMetadata {
    if (!this.viewControl.viewMetadata) {
      throw new Error('View control metadata must be initialized');
    }
    return this.viewControl.viewMetadata;
  }

  //#endregion

  //#region public methods

  async storeSettings(settings: ColumnsSettings | null): Promise<void> {
    const initSettings = this.convertToInitialSettings(settings ?? new ColumnsSettings());
    this.widgetContent.Settings = initSettings.serializeToStorage();

    await this._onStoreSettings?.();
  }

  //#endregion

  //#region protected methods

  protected getInitialSettings(): ColumnSettingsStorage {
    const settings = StorageHelper.tryGet<IStorage>(
      this.widgetContent,
      ViewWidgetTableColumnSettingsProvider.settingsKey
    );
    if (!settings) {
      return new ColumnSettingsStorage();
    }
    return StorageSerializable.deserialize(ColumnSettingsStorage, settings);
  }

  protected initColumns(
    resultSettings: ColumnsSettings,
    initialSettings: ColumnSettingsStorage
  ): void {
    resultSettings.isColumnsOrderingDefault = true;

    let orderedColumns = initialSettings.ordering;
    if (!orderedColumns?.some(c => this.viewMetadata.columns.has(c))) {
      orderedColumns = this.getDefaultColumnsOrdering();
    }

    orderedColumns = orderedColumns.filter(c => this.viewMetadata.columns.has(c));

    for (const orderedColumn of orderedColumns) {
      const invisibleByDefault =
        this.viewMetadata.columns.get(orderedColumn)?.invisibleByDefault ?? false;

      const initialVisibility = initialSettings.visibilities?.get(orderedColumn);

      const visible =
        (invisibleByDefault && initialVisibility) ||
        (!invisibleByDefault && initialVisibility !== false);

      const width = initialSettings.widths?.get(orderedColumn);
      resultSettings.columns.push(new ColumnSettings(orderedColumn, visible, width));
    }
  }

  protected initSortingColumns(
    resultSettings: ColumnsSettings,
    initialSettings: ColumnSettingsStorage
  ): void {
    let currentSortingSettings = initialSettings.sortingColumns?.map(
      x => new SortingSettings(x.alias, x.sortDirection)
    );

    if (!currentSortingSettings?.some(c => this.viewMetadata.columns.has(c.alias))) {
      currentSortingSettings =
        this.viewMetadata.defaultSortColumns.map(
          x => new SortingSettings(x.alias, x.sortDirection)
        ) ?? [];
    }

    currentSortingSettings = currentSortingSettings.filter(c =>
      this.viewMetadata.columns.has(c.alias)
    );

    resultSettings.sortingColumns = currentSortingSettings;
  }

  protected initGrouping(
    resultSettings: ColumnsSettings,
    initialSettings: ColumnSettingsStorage
  ): void {
    resultSettings.groupingColumn =
      initialSettings.groupingColumn !== undefined &&
      (initialSettings.groupingColumn === null ||
        this.viewMetadata.columns.has(initialSettings.groupingColumn))
        ? initialSettings.groupingColumn
        : this.viewMetadata.groupingColumn || null;
  }

  protected initTagsPosition(
    resultSettings: ColumnsSettings,
    initialSettings: ColumnSettingsStorage
  ): void {
    resultSettings.tagsPosition =
      initialSettings.tagsPosition !== null
        ? initialSettings.tagsPosition
        : (this.viewMetadata.tagsPosition ?? null);
  }

  protected convertToInitialSettings(settings: ColumnsSettings): ColumnSettingsStorage {
    const resultSettings = new ColumnSettingsStorage();

    for (const column of settings.columns) {
      if (column.width !== undefined) {
        if (!resultSettings.widths) {
          resultSettings.widths = new Map<string, number>();
        }

        resultSettings.widths.set(column.alias, column.width);
      }

      const invisibleByDefault =
        this.viewMetadata.columns.get(column.alias)?.invisibleByDefault ?? false;

      if ((invisibleByDefault && column.visible) || (!invisibleByDefault && !column.visible)) {
        if (!resultSettings.visibilities) {
          resultSettings.visibilities = new Map<string, boolean>();
        }
        resultSettings.visibilities.set(column.alias, column.visible);
      }
    }

    const orderedColumns = settings.columns.map(c => c.alias);
    const defaultOrderedColumns = this.getDefaultColumnsOrdering();
    if (
      !ViewWidgetTableColumnSettingsProvider.arraysAreEqualWithIgnoreCase(
        defaultOrderedColumns,
        orderedColumns
      )
    ) {
      resultSettings.ordering = orderedColumns;
    }

    const actualSortingSettings = settings.sortingColumns.map(
      x => (x.direction === SortDirection.Ascending ? '+' : '-') + x.alias
    );
    const defaultSortingSettings =
      this.viewMetadata.defaultSortColumns.map(x => (x.descending ? '-' : '+') + x.alias) ?? [];

    if (
      !ViewWidgetTableColumnSettingsProvider.arraysAreEqualWithIgnoreCase(
        actualSortingSettings,
        defaultSortingSettings
      )
    ) {
      resultSettings.sortingColumns = settings.sortingColumns.map(x => {
        const n = new SortingColumnStorage();
        n.alias = x.alias;
        n.sortDirection = x.direction;

        return n;
      });
    }

    const actualGrouping = settings.groupingColumn;
    const defaultGrouping = this.viewMetadata.groupingColumn || null;
    if (actualGrouping !== defaultGrouping) {
      resultSettings.groupingColumn = actualGrouping;
    }

    const actualTagsPosition = settings.tagsPosition;
    const defaultTagsPosition = this.viewMetadata.tagsPosition ?? null;
    if (actualTagsPosition && actualTagsPosition !== defaultTagsPosition) {
      resultSettings.tagsPosition = actualTagsPosition;
    }

    return resultSettings;
  }

  //#endregion

  //#region private methods

  private getDefaultColumnsOrdering(): string[] {
    const orderedColumns =
      [...this.viewMetadata.columns.entries()]
        .filter(x => !x[1]?.hidden && !!x[0])
        .map(x => x[0]) || [];

    const tagsPosition = this.viewMetadata.tagsPosition;
    if (tagsPosition) {
      if (this.viewControl.tagsInFirstColumn) {
        orderedColumns.unshift(tagColumnName);
      } else {
        orderedColumns.push(tagColumnName);
      }
    }

    return orderedColumns;
  }

  private static arraysAreEqualWithIgnoreCase(
    first: readonly string[],
    second: readonly string[]
  ): boolean {
    if (first.length !== second.length) {
      return false;
    }

    for (let i = 0; i < first.length; i++) {
      if (first[i].toUpperCase() !== second[i].toUpperCase()) {
        return false;
      }
    }

    return true;
  }

  //#endregion
}
