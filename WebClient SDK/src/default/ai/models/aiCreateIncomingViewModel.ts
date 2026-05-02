import { localize } from '@tessa/application';
import {
  Guid,
  IStorage,
  StorageSerializableContext,
  StorageSerializableObject,
  ValidationError,
  ValidationResult,
  ValueOrFactory
} from '@tessa/core';
import { DateTimeTypeFormat } from '@tessa/platform';
import { InitializableViewModelBase, ShadowPropsHelper } from '@tessa/ui';
import { observable, reaction, runInAction } from 'mobx';
import {
  AiChatMessage,
  AiEmployeeInfo,
  AiHelper,
  AiPartnerInfo,
  IAiToolDataViewModel,
  TextFormat
} from 'tessa/ui/ai';
import {
  AutocompleteProperty,
  DateTimeProperty,
  IProperty,
  PropertyGrid,
  PropertyGridBuilder,
  PropertyGridDataProvider,
  TextProperty
} from 'tessa/ui/propertyGrid';
import { AutocompleteDataViewContext } from 'ui/autocomplete';
import { AiPartnerInfoAutocompleteDataConverter } from '../tools/aiPartnerInfoAutocompleteDataConverter';
import { AiToolHelper } from '../aiToolHelper';
import { AiEmployeeInfoAutocompleteDataConverter } from '../tools/aiEmployeeInfoAutocompleteDataConverter';

/** Вью модель инструмента создания входящих договоров. */
export class AiCreateIncomingViewModel
  extends InitializableViewModelBase
  implements IAiToolDataViewModel
{
  protected _propertyGrid: PropertyGrid;

  //#region constructors

  constructor(
    private readonly _propertyData: IncomingDocumentInfo,
    private readonly _isActive: ValueOrFactory<boolean>
  ) {
    super();
  }

  //#endregion

  //#region properties

  get propertyGrid(): PropertyGrid {
    return this._propertyGrid;
  }

  //#endregion

  //#region base overrides

  protected override async initializeCore(): Promise<void> {
    await super.initializeCore();
    await this.initializePropertyGrid();
  }

  //#endregion

  //#region IAiToolDataViewModel members

  toText(format: TextFormat = TextFormat.Markdown): string {
    const properties = this.propertyGrid.getProperties(true);

    if (format === TextFormat.Plain) {
      const propertyTemplate = (caption: string, text: string) => `${caption}${text}`;
      return properties
        .map(property => AiCreateIncomingViewModel.getPropertyText(property, propertyTemplate))
        .join('\n');
    }

    if (format === TextFormat.HTML) {
      const propertyTemplate = (caption: string, text: string) => `<li>${caption}${text}</li>`;
      const propertiesText = properties
        .map(property => AiCreateIncomingViewModel.getPropertyText(property, propertyTemplate))
        .join('');

      return `<ul>${propertiesText}</ul>`;
    }

    if (format === TextFormat.Markdown) {
      const propertyTemplate = (caption: string, text: string) => `- ${caption}${text}`;
      return properties
        .map(property => AiCreateIncomingViewModel.getPropertyText(property, propertyTemplate))
        .join('\n');
    }

    const error = localize('$Ai_AiAgent_Validation_TextFormatNotSupported');
    throw new ValidationError(ValidationResult.fromText(error));
  }

  async applyChanges(aiMessage: AiChatMessage): Promise<void> {
    if (this.propertyGrid.dataProvider.hasChanges) {
      const dataTyped = this.propertyGrid.dataProvider.data as unknown as IncomingDocumentInfo;

      if (dataTyped.partner) {
        dataTyped.partnerNotFound = false;
      }

      aiMessage.data = dataTyped.serializeToStorage();
    }
  }

  //#endregion

  //#region private methods

  /**
   * Инициализирует PropertyGrid, используя исходные данные.
   * @param isActive Указывает активно сообщение или нет.
   */
  private async initializePropertyGrid(): Promise<void> {
    let failPartner: AiPartnerInfo | null = null;

    if (this._propertyData.partnerNotFound) {
      failPartner = this._propertyData.partner;
      this._propertyData.partner = null;
    }

    const dataProvider = new PropertyGridDataProvider(this._propertyData, false);
    const propertyGrid = PropertyGridBuilder.create({ dataProvider, toolbarVisibility: false })
      .addTextProperty({
        data: dataProvider,
        alias: 'number',
        caption: '$Ai_CreateIncomingAiAgentPlugin_OutgoingNumber'
      })
      .addDateTimeProperty({
        data: dataProvider,
        alias: 'docDate',
        caption: '$Ai_CreateIncomingAiAgentPlugin_DocDate',
        formatType: DateTimeTypeFormat.Date
      })
      .addAutocompleteProperty({
        data: dataProvider,
        alias: 'partner',
        caption: '$Ai_CreateIncomingAiAgentPlugin_Partner',
        dataContext: new AutocompleteDataViewContext({
          viewAlias: 'Partners',
          idColumn: 'PartnerID',
          nameColumn: 'PartnerName',
          parameterAlias: 'Name',
          unique: true
        }),
        dataConverter: new AiPartnerInfoAutocompleteDataConverter(),
        onInitialized: async ({ control }) => {
          control.menu.openAction.action = async () => {
            const cardId = control.selectedRecord?.model.id as string;

            if (Guid.isValid(cardId)) {
              await AiHelper.openCard({ cardId });
            }
          };

          control.validationContainer.resultMode = 'full';

          if (failPartner) {
            control.validationContainer.add(context => {
              if (!failPartner) {
                return;
              }

              if (control.record) {
                failPartner = null;
                return;
              }

              context.addWarning({
                message: `${localize('$Ai_CreateIncomingAiAgentPlugin_PartnerNotFound')} ${AiToolHelper.formatPartnerName(failPartner.shortName, failPartner.fullName)}`
              });
            });
          }
        }
      })
      .addAutocompleteProperty({
        data: dataProvider,
        alias: 'recipients',
        caption: '$Ai_CreateIncomingAiAgentPlugin_Receivers',
        dataContext: new AutocompleteDataViewContext({
          viewAlias: 'Users',
          idColumn: 'UserID',
          nameColumn: 'UserName',
          parameterAlias: 'Name',
          unique: true,
          multiple: true
        }),
        dataConverter: new AiEmployeeInfoAutocompleteDataConverter(),
        onInitialized: async ({ control }) => {
          control.menu.openAction.action = async () => {
            const cardId = control.selectedRecord?.model.id as string;

            if (Guid.isValid(cardId)) {
              await AiHelper.openCard({ cardId });
            }
          };

          control.recordsLineBreak = true;
        }
      })
      .addTextProperty({
        data: dataProvider,
        alias: 'subject',
        captionVisibility: false,
        onInitialized: async property => {
          property.leftCaption = false;

          property.control.minRows = 2;
          property.control.maxRows = 6;
        }
      })
      .onGridInitialized(grid => {
        const isActive = this._isActive;

        if (typeof isActive === 'boolean') {
          grid.disabled = !isActive;
          if (!isActive) {
            grid.selectProperty(null);
          }
        } else {
          ShadowPropsHelper.add(grid, 'disabled', value => value() || !isActive());
          grid.disposeList.add(reaction(isActive, value => !value && grid.selectProperty(null)));
        }

        grid.themeLimitations = { noPadding: true };
      })
      .build();

    await propertyGrid.initialize();
    this._propertyGrid = propertyGrid;
  }

  private static getPropertyText(
    property: IProperty,
    propertyTemplate: (caption: string, text: string) => string
  ): string {
    if (!property.alias) {
      return '';
    }

    const captionText = property.caption.text ? `${localize(property.caption.text)}: ` : '';

    if (property instanceof TextProperty) {
      return propertyTemplate(captionText, property.value ?? '');
    }

    if (property instanceof DateTimeProperty) {
      return propertyTemplate(captionText, `${property.value ?? ''}`);
    }

    if (property instanceof AutocompleteProperty) {
      let valueText: string;
      if (property.dataSource.multiple) {
        valueText = property.values.map(value => value.display).join(', ');
      } else {
        valueText = property.value ? property.value.display : '';
      }

      return propertyTemplate(captionText, valueText);
    }

    return '';
  }

  //#endregion
}

/**
 * Information about the incoming document to be filled in by the tool.
 */
export class IncomingDocumentInfo extends StorageSerializableObject {
  //#region fields

  @observable.ref
  private _number: string | null = null;

  @observable.ref
  private _docDate: string | null = null;

  @observable.ref
  private _subject: string | null = null;

  @observable.ref
  private _partner: AiPartnerInfo | null = null;

  @observable.ref
  private _partnerNotFound = false;

  @observable.ref
  private _recipients: AiEmployeeInfo[] = observable.array([]);

  //#endregion

  //#region properties

  get number(): string | null {
    return this._number;
  }
  set number(value: string | null) {
    runInAction(() => (this._number = value));
  }

  get docDate(): string | null {
    return this._docDate;
  }
  set docDate(value: string | null) {
    runInAction(() => (this._docDate = value));
  }

  get subject(): string | null {
    return this._subject;
  }
  set subject(value: string | null) {
    runInAction(() => (this._subject = value));
  }

  get partner(): AiPartnerInfo | null {
    return this._partner;
  }
  set partner(value: AiPartnerInfo | null) {
    runInAction(() => (this._partner = value));
  }

  get partnerNotFound(): boolean {
    return this._partnerNotFound;
  }
  set partnerNotFound(value: boolean) {
    runInAction(() => (this._partnerNotFound = value));
  }

  get recipients(): AiEmployeeInfo[] {
    return this._recipients;
  }
  set recipients(value: AiEmployeeInfo[]) {
    runInAction(() => (this._recipients = observable.array(value)));
  }

  //#endregion

  //#region base overrides

  protected override serializeToStorageCore(
    storage: IStorage = {},
    context?: StorageSerializableContext
  ): void {
    super.serializeToStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    sa.setStringIfNotEmpty('Number', this.number);
    sa.setDateTimeIfNotDefault('DocDate', this.docDate);
    sa.setStringIfNotEmpty('Subject', this.subject);
    sa.setIfNotNull('Partner', this.partner?.serializeToStorage({}, context));
    sa.setBooleanIfNotDefault('PartnerNotFound', this.partnerNotFound);
    sa.setIfNotEmpty(
      'Recipients',
      this.recipients.map(x => x.serializeToStorage({}, context))
    );
    sa.removeEmptyFields();
  }

  protected override deserializeFromStorageCore(
    storage: IStorage,
    context?: StorageSerializableContext
  ): void {
    super.deserializeFromStorageCore(storage, context);

    const sa = this.getStorageAccessor(storage, context);
    this.number = sa.tryGetString('Number');
    this.docDate = sa.tryGetDateTime('DocDate');
    this.subject = sa.tryGetString('Subject');
    this.partner = sa.tryGetObject('Partner', x =>
      new AiPartnerInfo().deserializeFromStorage(x, context)
    );
    this.partnerNotFound = sa.tryGetBooleanOrDefault('PartnerNotFound');
    this.recipients =
      sa.tryGetList('Recipients', x => new AiEmployeeInfo().deserializeFromStorage(x, context)) ??
      [];
  }

  //#endregion
}
