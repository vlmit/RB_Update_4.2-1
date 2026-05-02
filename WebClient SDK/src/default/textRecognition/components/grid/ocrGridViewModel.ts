import { reaction } from 'mobx';
import { Flags, IStorageArray } from '@tessa/core';
import { CardMetadataHelper, CardMetadataRuntimeType } from '@tessa/platform';
import { CardTypeCustomControl } from 'tessa/cards/types';
import { CardModelFlags, ICardModel } from 'tessa/ui/cards';
import { ControlViewModelBase } from 'tessa/ui/cards/controls';
import { tryGetFromSettings } from 'tessa/ui/uiHelper';
import {
  IPropertyGridDataProvider,
  PropertyGrid,
  PropertyGridBuilder
} from 'tessa/ui/propertyGrid';
import { OcrProperty } from './properties/ocrProperty';
import { IOcrControlSettings, IOcrPropertySettings } from './properties/ocrPropertySettings';
import { OcrPropertyDataSource } from './properties/ocrPropertyDataSource';
import { OcrDateProperty } from './properties/date/ocrDateProperty';
import { OcrDatePropertyDataSource } from './properties/date/ocrDatePropertyDataSource';
import { OcrReferenceProperty } from './properties/reference/ocrReferenceProperty';
import { OcrReferencePropertyDataSource } from './properties/reference/ocrReferencePropertyDataSource';
import { OcrGridDataConverter, OcrGridDataProvider } from './ocrGridTypes';

/** View model for the OCR property grid control. */
export class OcrGridViewModel extends ControlViewModelBase {
  //#region constructors

  /**
   *  Creates an instance of {@link OcrGridViewModel}.
   * @param control An object describing the control's layout and properties,
   * used for binding to the fields of the card's string section.
   * @param model The card model accessible within the UI.
   */
  constructor(control: CardTypeCustomControl, model: ICardModel) {
    super(control);

    const settings = tryGetFromSettings<IStorageArray>(control.controlSettings, 'Properties', []);
    const ocrMappingStorage = OcrGridDataConverter.deserializeFromCard(model.card);
    const ocrGridDataProvider = new OcrGridDataProvider(ocrMappingStorage, true);

    const builder = PropertyGridBuilder.create(ocrGridDataProvider).onGridInitialized(grid => {
      grid.leftCaption = false;
      grid.toolbarVisibility = false;
    });

    for (const controlSettings of settings) {
      const ocrControlSettings = controlSettings as IOcrControlSettings;
      const property = this.resolveProperty(ocrGridDataProvider, ocrControlSettings, model);
      property.setContextExecutor(model.executeInContext);
      builder.addProperty(property);
    }

    this.control = builder.build();

    this._disposer = reaction(
      () => ocrGridDataProvider.hasChanges,
      value => (model.flags = Flags.setFlag(model.flags, CardModelFlags.ForceChanges, value))
    );
  }

  //#endregion

  //#region fields

  private _disposer: VoidFunction | null = null;

  //#endregion

  //#region properties

  /** The OCR property grid control view model. */
  readonly control: PropertyGrid;

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    await this.control.initialize();
  }

  protected override disposeCore(): void {
    this._disposer?.();
    this._disposer = null;
    this.control.dispose();
    super.disposeCore();
  }

  //#endregion

  //#region protected methods

  /**
   * Resolves and creates an OCR property based on the provided settings.
   * @param dataProvider The data provider for the property.
   * @param settings The settings for the OCR control.
   * @param model The card model accessible within the UI.
   * @returns An instance of the appropriate OCR property.
   */
  protected resolveProperty(
    dataProvider: IPropertyGridDataProvider,
    settings: IOcrControlSettings,
    model: ICardModel
  ): OcrProperty {
    switch (CardMetadataHelper.getRuntimeTypeFromDbType(settings.schemeType)) {
      case CardMetadataRuntimeType.Object:
        const refSettings = this.createSettings(settings, model);
        const refData = new OcrReferencePropertyDataSource(dataProvider, refSettings);
        return new OcrReferenceProperty(refData, refSettings);
      case CardMetadataRuntimeType.DateTime:
      case CardMetadataRuntimeType.DateTimeOffset:
        const dateSettings = this.createSettings(settings, model);
        const dateData = new OcrDatePropertyDataSource(dataProvider, dateSettings);
        return new OcrDateProperty(dateData, dateSettings);
      default:
        const textSettings = this.createSettings(settings, model);
        const textData = new OcrPropertyDataSource(dataProvider, textSettings);
        return new OcrProperty(textData, textSettings);
    }
  }

  //#endregion

  //#region private methods

  /**
   * Creates OCR property settings from the provided parameters.
   * @param alias The alias for the property.
   * @param schemeType The scheme type of the control.
   * @param control  An object describing the control's layout and properties,
   * used for binding to the fields of the card's string section.
   * @param model The card model accessible within the UI.
   * @returns An instance of the appropriate OCR property settings.
   */
  private createSettings(settings: IOcrControlSettings, model: ICardModel): IOcrPropertySettings {
    const { alias, schemeType, control, ...controlSettings } = settings;

    return {
      alias,
      schemeType,
      caption: control.caption ?? undefined,
      tooltip: control.toolTip ?? undefined,
      requiredText: control.requiredText ?? undefined,
      required: control.isRequired(),
      disabled: control.isReadOnly(),
      visibility: control.isVisible(),
      ...controlSettings,
      model, // TODO: OCR - consider removing (used only in reference property)
      control // TODO: OCR - consider removing (used only in reference property)
    };
  }

  //#endregion
}
