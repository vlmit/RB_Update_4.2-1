import { reaction } from 'mobx';
import { CardHelper, CardRow } from '@tessa/platform';
import { FieldType, Guid } from '@tessa/core';
import { extension } from '@tessa/application';
import { FieldMapStorage } from 'tessa/cards/fieldMapStorage';
import { ArrayStorage } from 'tessa/platform/storage/arrayStorage';
import { IControlViewModel } from 'tessa/ui/cards/interfaces';
import { CardUIExtension } from 'tessa/ui/cards/cardUIExtension';
import { ICardUIExtensionContext } from 'tessa/ui/cards/cardUIExtensionContext';
import { CardTableViewControlViewModel } from '../../ui/tableViewExtension/cardTableViewControlViewModel';
import { CardTableViewRowData } from '../../ui/tableViewExtension/cardTableViewRowData';
import { OcrSettingsTypeId } from '../misc/ocrConstants';

/**
 * Расширение, реализующее логику взаимодействия с карточкой настроек OCR.
 * @remarks Детальное описание расширения:
 * 1. При настройке маппинга полей выполняется очистка связанных полей и секций.
 * 2. Проброс параметров маппинга в форму строк дочерних таблиц с настройками верификации.
 */
@extension({ name: 'OcrSettingsUIExtension' })
export class OcrSettingsUIExtension extends CardUIExtension {
  //#region base overrides

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return Guid.equals(context.card.typeId, OcrSettingsTypeId);
  }

  initialized(context: ICardUIExtensionContext): void {
    const cardSections = context.card.sections;
    const virtualFields = cardSections.get('OcrMappingSettingsVirtual').fields;
    const types = cardSections.get('OcrMappingSettingsTypes').rows;
    const sections = cardSections.get('OcrMappingSettingsSections').rows;
    const fields = cardSections.get('OcrMappingSettingsFields').rows;

    this.onDataChanged(types, 'TypeID', sections);
    this.onDataChanged(sections, 'SectionID', fields);

    this.selectedRowChangedHandler(
      'FieldsMappingTypesSettings',
      'TypeID',
      virtualFields,
      context.model.controls
    );
    this.selectedRowChangedHandler(
      'FieldsMappingSectionsSettings',
      'SectionID',
      virtualFields,
      context.model.controls
    );
  }

  //#endregion

  //#region handlers

  private onDataChanged(
    observable: ArrayStorage<CardRow>,
    observableFieldName: string,
    observer: ArrayStorage<CardRow>
  ): void {
    for (const observableRow of observable) {
      this.onFieldChanged(observableRow, observableFieldName, observer);
    }
    this.onCollectionChanged(observable, observableFieldName, observer);
  }

  private onCollectionChanged(
    observable: ArrayStorage<CardRow>,
    observableFieldName: string,
    observer: ArrayStorage<CardRow>
  ): void {
    this.disposeList.add(
      observable.collectionChanged.add(args => {
        for (const observableRow of args.added) {
          this.onFieldChanged(observableRow, observableFieldName, observer);
        }
      })
    );
  }

  private onFieldChanged(
    observableRow: CardRow,
    observableFieldName: string,
    observer: ArrayStorage<CardRow>
  ): void {
    this.disposeList.add(
      observableRow.fieldChanged.add(args => {
        if (args.fieldName === observableFieldName) {
          const rowId = observableRow.rowId;
          CardHelper.clearStorageRows(observer, r => Guid.equals(r.parentRowId, rowId));
        }
      })
    );
  }

  private selectedRowChangedHandler(
    controlName: string,
    fieldName: string,
    virtualStorage: FieldMapStorage,
    controls: ReadonlyMap<string, IControlViewModel>
  ): void {
    const control = controls.get(controlName) as CardTableViewControlViewModel;
    this.disposeList.add(
      reaction(
        () => control.selectedRow,
        () => {
          const cardRow = (control.selectedRow as CardTableViewRowData)?.cardRow;
          virtualStorage.rawSet(fieldName, cardRow?.get(fieldName), FieldType.Guid);
        }
      )
    );
  }

  //#endregion
}
