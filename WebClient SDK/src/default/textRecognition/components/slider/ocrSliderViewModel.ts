import { computed, runInAction } from 'mobx';
import { localize } from '@tessa/application';
import { CardMetadataRuntimeType, CardMetadataSection } from 'tessa/cards/metadata';
import { CardTypeEntryControl } from 'tessa/cards/types';
import { ControlViewModelBase } from 'tessa/ui/cards/controls';
import { DotNetType, unseal } from 'tessa/platform';
import { FieldMapStorage, PermissionHelper } from 'tessa/cards';
import { ICardModel } from 'tessa/ui/cards';
import { tryGetFromSettings } from 'tessa/ui';
import { ISliderSettings, RoundingType } from 'ui/slider/definition';
import { ControlContainerProps } from 'ui/controlContainer/definitions';

/** Модель-представление контрола ползунка для ввода вещественных значений. */
export class OcrSliderViewModel extends ControlViewModelBase {
  //#region fields

  /** Хранилище с данными полей, среди которых имеется отслеживаемое поле. */
  private _fields: FieldMapStorage;
  /** Название отслеживаемого поля. */
  private _fieldName: string;

  private _sliderSettings: ISliderSettings;

  private _displayValues = true;

  private _roundValues: RoundingType = 'floor';

  //#endregion

  //#region properties

  /** Минимальное значение. */
  public readonly minValue: number;

  /** Максимальное значение. */
  public readonly maxValue: number;

  /** Шаг смещения значения. */
  public readonly step: number;

  /** Текущее значение. */
  @computed
  public get value(): number {
    return this._fields.get(this._fieldName) ?? 0;
  }
  public set value(value: number) {
    this._fields.set(this._fieldName, value, DotNetType.Int);
  }

  public get displayValues(): boolean {
    return this._displayValues;
  }
  public set displayValues(value: boolean) {
    runInAction(() => {
      this._displayValues = value;
    });
  }

  public get roundValues(): RoundingType {
    return this._roundValues;
  }
  public set roundValues(value: RoundingType) {
    runInAction(() => {
      this._roundValues = value;
    });
  }

  /** Цвет ползунка */
  @computed
  public get thumbColor(): string {
    return this.controlTheme.getValue('--slider-thumb-background') ?? '';
  }
  public set thumbColor(value: string) {
    this.controlTheme.setValue('--slider-thumb-background', value);
  }

  /** Цвет ползунка при зажатии мышью */
  @computed
  public get activeThumbColor(): string {
    return this.controlTheme.getValue('--slider-thumb-active-background') ?? '';
  }
  public set activeThumbColor(value: string) {
    this.controlTheme.setValue('--slider-thumb-active-background', value);
  }

  /** Цвет ползунка при наведении мыши */
  @computed
  public get hoverThumbColor(): string {
    return this.controlTheme.getValue('--slider-thumb-hover-background') ?? '';
  }
  public set hoverThumbColor(value: string) {
    this.controlTheme.setValue('--slider-thumb-hover-background', value);
  }

  /** Настройки слайдера */
  @computed
  public get sliderSettings(): ISliderSettings {
    return this._sliderSettings;
  }
  public set sliderSettings(value: ISliderSettings) {
    this._sliderSettings = value;
  }

  public override get caption(): string {
    return `${localize(this._propertyContainer.caption)} (${this.value})`;
  }
  public override set caption(value: string) {
    this._propertyContainer.caption = value;
  }

  public override get controlContainer(): ControlContainerProps {
    return this._controlContainer;
  }

  //#endregion

  //#region constructors

  /**
   * Создает экземпляр класса {@link OcrSliderViewModel}.
   * @param {CardTypeEntryControl} control
   * Объект, описывающий расположение и свойства элемента
   * управления для привязки к полям строковой секции карточки.
   * @param {ICardModel} model Модель карточки, доступная в UI.
   */
  constructor(control: CardTypeEntryControl, model: ICardModel) {
    super(control);

    if (!control.sectionId) {
      throw new Error('Control section id is not defined.');
    }

    // пытаемся найти секцию через id
    const metadataSectionSealed = model.cardMetadata.sections.getSectionById(control.sectionId);
    if (!metadataSectionSealed) {
      throw new Error(`Can not find section metadata with id - ${control.sectionId}.`);
    }

    const metadataSection = unseal<CardMetadataSection>(metadataSectionSealed);

    if (control.complexColumnId) {
      const complexColumn = metadataSection.getColumnById(control.complexColumnId);
      if (!complexColumn) {
        throw new Error(
          `Can not find column in section metadata with id - ${control.complexColumnId}.`
        );
      }
      const physicalColumn = metadataSection
        .getPhysicalColumns(complexColumn)
        .find(x => !!x.metadataType && x.metadataType.type === CardMetadataRuntimeType.Boolean);
      if (!physicalColumn) {
        throw new Error('Can not find physical column in section metadata with boolean type.');
      }
      if (!physicalColumn.name) {
        throw new Error(`Column\`s name is not set.`);
      }
      this._fieldName = physicalColumn.name;
    } else {
      const columnId = control.physicalColumnIdList[0];
      const column = metadataSection.getColumnById(columnId);
      if (!column) {
        throw new Error(`Can not find column in section metadata with id - ${columnId}.`);
      }
      if (!column.name) {
        throw new Error('Column name is not set.');
      }
      this._fieldName = column.name;
    }

    if (!model.table) {
      const cardSection = model.card.sections.tryGet(metadataSection.name!);
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
    this.step = tryGetFromSettings(settings, 'Step', 1);
    this.minValue = tryGetFromSettings(settings, 'MinValue', 1);
    this.maxValue = tryGetFromSettings(settings, 'MaxValue', 100);

    this.controlContainer.section = 'slider';
    this.controlContainer.background = 'none';
    this.controlContainer.border = 'none';
    this.controlContainer.font = 'none';
    this.controlContainer.padding = 'none';
    this.controlContainer.noScroll = true;
  }

  //#endregion
}
