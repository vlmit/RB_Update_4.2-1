import { computed } from 'mobx';
import { FieldStorageMap, FieldType, StorageHelper } from '@tessa/core';
import { CardTypeEntryControl, CardMetadataRuntimeType } from '@tessa/platform';
import { ControlViewModelBase } from 'tessa/ui/cards/controls';
import { ICardModel } from 'tessa/ui/cards';
import { PermissionHelper } from 'tessa/cards';

export class SliderViewModel extends ControlViewModelBase {
  //#region ctor

  constructor(control: CardTypeEntryControl, model: ICardModel) {
    super(control);

    if (!control.sectionId) {
      throw new Error('Control section id is not defined.');
    }

    // пытаемся найти секцию через id
    const metadataSection = model.cardMetadata.sections.getSectionById(control.sectionId);
    if (!metadataSection) {
      throw new Error(`Can not find section metadata with id - ${control.sectionId}.`);
    }

    if (control.complexColumnId) {
      const complexColumn = metadataSection.columns.getColumnById(control.complexColumnId);
      if (!complexColumn) {
        throw new Error(
          `Can not find column in section metadata with id - ${control.complexColumnId}.`
        );
      }
      const physicalColumn = metadataSection.columns
        .getPhysicalColumns(complexColumn)
        .find(x => x.metadataType.type === CardMetadataRuntimeType.Boolean);
      if (!physicalColumn) {
        throw new Error(`Can not find physical column in section metadata with boolean type.`);
      }
      if (!physicalColumn.name) {
        throw new Error(`Column\`s name is not set.`);
      }
      this._fieldName = physicalColumn.name;
    } else {
      const columnId = control.physicalColumnIdList[0];
      const column = metadataSection.columns.getColumnById(columnId);
      if (!column) {
        throw new Error(`Can not find column in section metadata with id - ${columnId}.`);
      }
      if (!column.name) {
        throw new Error(`Column\`s name is not set.`);
      }
      this._fieldName = column.name;
    }

    if (!model.table) {
      const cardSection = model.card.sections.tryGet(metadataSection.name);
      if (!cardSection) {
        throw new Error(`Can not find section with name ${metadataSection.name} in card.`);
      }
      this._fields = cardSection.fields;
    } else {
      const row = model.table.row;
      this._fields = row;
    }

    this.isReadOnly = PermissionHelper.instance.getReadOnlyEntryControl(
      model,
      control,
      this._fieldName,
      metadataSection.name!
    );

    const settings = control.controlSettings;
    this.minValue = StorageHelper.tryGetValue(settings, 'MinValue', FieldType.Int) ?? 0;
    this.maxValue = StorageHelper.tryGetValue(settings, 'MaxValue', FieldType.Int) ?? 100;
    this.step = StorageHelper.tryGetValue(settings, 'Step', FieldType.Int) ?? 1;

    // TODO убрать после переноса в controlContainer
    this.useSubgrid = false;
  }

  //#endregion

  //#region fields

  private _fields: FieldStorageMap;

  private _fieldName: string;

  //#endregion

  //#region props

  readonly minValue: number;

  readonly maxValue: number;

  readonly step: number;

  @computed
  get value(): number {
    return this._fields.get(this._fieldName) ?? 0;
  }
  set value(value: number) {
    this._fields.set(this._fieldName, value, FieldType.Int);
  }

  //#endregion
}
