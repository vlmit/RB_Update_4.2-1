import { extension } from '@tessa/application';
import {
  CardMetadataSectionArray,
  CardSectionType,
  CardTypeEntryControl,
  ICardMetadataSection,
  ICardTypeDataLoader,
  ICardTypeDataLoader$,
  ICardTypeMetadata,
  CardSectionPermissionInfo,
  CardTypeTabControl,
  ICardMetadata,
  ICardMetadata$,
  ICardMetadataColumn,
  CardMetadataColumnType
} from '@tessa/platform';
import { Guid, TypedField, FieldType, StorageHelper, IStorageArray } from '@tessa/core';
import {
  createCardModelWithMetadata,
  createCardEditorModel,
  createCardModel,
  CreateModelFunc,
  tryGetFromSettings
} from 'tessa/ui/uiHelper';
import {
  CardUIExtension,
  ICardUIExtensionContext,
  ICardEditorModel,
  ICardModel
} from 'tessa/ui/cards';
import { IUIContext } from 'tessa/ui/uiContext';
import { Visibility } from 'tessa/platform/visibility';
import { PermissionHelper } from 'tessa/cards/permissionHelper';
import { OcrGridData, OcrGridDataConverter } from '../components/grid/ocrGridTypes';
import { IOcrControlSettings } from '../components/grid/properties/ocrPropertySettings';
import { OcrOperationTypeId, OcrSourceEditorKey } from '../misc/ocrConstants';
import { OcrSettings } from '../misc/ocrSettings';
import { OcrHelper } from '../misc/ocrHelper';

// TODO: #OCR - необходимо изолировать карточку операции OCR от метаданных исходной карточки:
// 1. Игнорировать права на секции, строки и поля.
// 2. Игнорировать настройки контролов.

/**
 * Расширение, реализующее инициализацию редактора исходной карточки
 * и модификацию модели карточки операции OCR перед её отображением.
 * @remarks Детальное описание расширения:
 * 1. Расширение выполняется в первую очередь перед другими расширениями.
 * 2. При инициализации редактора исходной карточки выполняется:
 *   - Создание редактора, если работа с карточкой операции OCR
 *     происходит вне контекста исходной карточки.
 *   - Редактор исходной размещается в дополнительной информации текущего UI-контекста.
 *   - Для идентификации созданного редактора от существующего, в дополнительную
 *     информацию редактора устанавливается специальный признак.
 * 3. При инициализации модели карточки операции OCR выполняется:
 *   - Модификация метаданных карточки операции OCR.
 *   - Настройка прав доступа на те или иные секции и поля.
 *   - Настройка контролов, в соответствии с настройками контролов в исходной карточке.
 */
@extension({ name: 'OcrCreateModelUIExtension' })
export class OcrCreateModelUIExtension extends CardUIExtension {
  //#region constructors

  constructor(
    @ICardMetadata$() private readonly _cardMetadata: ICardMetadata,
    @ICardTypeDataLoader$() private readonly _cardTypeDataLoader: ICardTypeDataLoader
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  shouldExecute(context: ICardUIExtensionContext): boolean {
    return Guid.equals(context.card.typeId, OcrOperationTypeId);
  }

  async createCardModel(context: ICardUIExtensionContext): Promise<void> {
    // Получение информации об исходной карточки из карточки операции OCR
    const ocrOperations = context.card.sections.get('OcrOperations').fields;
    const sourceCardId = ocrOperations.getString('CardID');
    const sourceCardTypeId = ocrOperations.getString('CardTypeID')!;
    const sourceCardTypeName = ocrOperations.getString('CardTypeName')!;

    // Инициализация редактора исходной карточки.
    // Если редактор не задан в родительском UI-контексте, то выполняется попытка его создания.
    const sourceEditor = await this.createSourceCardEditor(
      context.uiContext,
      sourceCardTypeId,
      sourceCardTypeName,
      sourceCardId
    );

    // Сохранение информации о редакторе исходной карточки в UI-контексте расширения
    sourceEditor?.cardModel && OcrHelper.setSourceEditor(context.uiContext.info, sourceEditor);

    // Инициализация фабрики для создания модели карточки операции OCR
    context.modelToCreate = this.createOcrCardModelWithMetadataFactory(
      sourceCardTypeId,
      sourceEditor?.cardModel
    );
  }

  //#endregion

  //#region private methods

  /**
   * Выполняет создание редактора исходной карточки, если он отсутствовал в родительском UI-контексте.
   * Если редактор присутствовал в родительском контексте, то он будет возвращён.
   * @param uiContext Контекст операции с пользовательским интерфейсом.
   * @param sourceCardTypeId Идентификатор типа исходной карточки.
   * @param sourceCardTypeId Название типа исходной карточки.
   * @param sourceCardId Идентификатор исходной карточки, если он задан.
   * @returns Редактор исходной карточки или `null`, если он не был найден или не был создан.
   */
  private async createSourceCardEditor(
    uiContext: IUIContext,
    sourceCardTypeId: string,
    sourceCardTypeName: string,
    sourceCardId?: string | null
  ): Promise<ICardEditorModel | null> {
    let sourceEditor = uiContext.parent?.cardEditor;

    // Если родительский контекст не соответствует исходной карточке, то пытаемся её создать
    if (!Guid.equals(sourceEditor?.cardModel?.card.id, sourceCardId)) {
      sourceEditor = createCardEditorModel();
      sourceEditor.info[OcrSourceEditorKey] = TypedField.trueBoolean;
      if (
        sourceCardId &&
        !(await sourceEditor.openCard({
          cardId: sourceCardId,
          cardTypeId: sourceCardTypeId,
          cardTypeName: sourceCardTypeName,
          context: uiContext
        }))
      ) {
        return null;
      }
    }

    return sourceEditor ?? null;
  }

  /**
   * Фабрика для создания модели карточки с учетом модифицированных метаданных.
   * @param cardTypeId Идентификатор типа исходной карточки.
   * @param cardModel Модель исходной карточки с файлом, на основании которого создается карточка OCR.
   * @returns Фабрика для создания модели карточки с учетом модифицированных метаданных.
   */
  private createOcrCardModelWithMetadataFactory(
    cardTypeId: string,
    cardModel?: ICardModel | null
  ): CreateModelFunc {
    return async (ocrCard, ocrSectionRows) => {
      // Поиск записи в таблице с настройками маппинга для типа исходной карточки
      const ocrMappingSettingsType = OcrSettings.instance.mapping.types.getById(cardTypeId);
      if (!ocrMappingSettingsType) {
        // Если найти не удалось, то создаем модель карточки операции OCR без изменений
        return createCardModel(ocrCard, ocrSectionRows);
      }

      // Получение метаданных для карточки операции OCR
      let ocrCardTypeMetadata = await this._cardMetadata.getMetadataForType(ocrCard.typeId);
      if (!ocrCardTypeMetadata) {
        throw new Error(`Can not find card type with id '${ocrCard.typeId}' as metadata.`);
      }
      // Глубокая копия типа карточки операции OCR и ее подмена в метаданных
      ocrCardTypeMetadata = ocrCardTypeMetadata.clone();
      const ocrCardType = ocrCardTypeMetadata.cardType;

      // Поиск контрола-контейнера, хранящегося в карточке операции OCR, в который будут добавлены новые контролы для полей
      const ocrControlsContainer = ocrCardType.forms
        .find(f => f.name === 'VerificationTab')
        ?.blocks.find(b => b.name === 'VerificationBlock')
        ?.controls?.find(c => c.name == 'ControlsContainer') as CardTypeTabControl;
      const ocrControls = ocrControlsContainer?.forms?.[0].blocks?.[0].controls;

      if (!ocrControls) {
        throw new Error(
          `Can not find controls for card type with id '${ocrCard.typeId}'` +
            " at form with alias 'VerificationTab', block with alias 'VerificationBlock'" +
            " and control container 'ControlsContainer'."
        );
      }

      // Поиск контрола обозревателя свойств в карточке операции OCR и получение его настроек
      const ocrGridSettings = ocrControls?.find(c => c.name === 'OcrGrid')?.controlSettings;
      if (!ocrGridSettings) {
        throw new Error(
          `Can not find grid control settings for card type with id '${ocrCard.typeId}'` +
            " at form with alias 'VerificationTab', block with alias 'VerificationBlock'."
        );
      }
      const ocrControlsSettings = tryGetFromSettings<IStorageArray<IOcrControlSettings>>(
        ocrGridSettings,
        'Properties'
      );

      // Данные, содержащие информацию о полях и секциях, которые будут перенесены в исходную карточку
      const ocrMappingStorage = OcrGridDataConverter.deserializeFromCard(ocrCard);

      // Права для секций исходной карточки и карточки операции OCR
      const sectionsPermissions = cardModel?.card?.tryGetPermissions()?.tryGetSections();
      const ocrSectionsPermissions = ocrCard.permissions.sections;

      // Данные исходной карточки, включающие содержимое всех строк и полей.
      const sections = cardModel?.card?.tryGetSections();

      const sectionsParams = {
        cardTypeMetadata: ocrCardTypeMetadata,
        sectionsIds: Array.from(ocrMappingSettingsType.sections).map(x => x.id),
        sections: null
      };

      // Поиск секции в метаданных для каждой секции из настроек маппинга в соответствии с найденным типом карточки
      for (const ocrMappingSettingSection of ocrMappingSettingsType.sections) {
        const sectionId = ocrMappingSettingSection.id;
        const sectionMetadataResult = await this.getSectionMetadataForType(
          sectionId,
          sectionsParams
        );
        if (!sectionMetadataResult) {
          throw new Error(`Can not find section metadata with id '${sectionId}'.`);
        }

        const [sectionMetadata, sectionIndex] = sectionMetadataResult;

        if (sectionMetadata.sectionType !== CardSectionType.Entry) {
          throw new Error(`Section metadata with id '${sectionId}' must be entry type.`);
        }

        // Вычисление актуального имени секции и получение списка всех полей объявленных в секции
        const sectionName = sectionMetadata.name || ocrMappingSettingSection.name;
        const sectionFields = sections?.tryGet(sectionName)?.tryGetFields();

        // Добавление секции из метаданных в карточку операции OCR
        const sectionMetadataCopy = sectionMetadata.clone();
        sectionMetadataCopy.cardTypeIdList.push(ocrCard.typeId);
        sectionIndex === -1
          ? ocrCardTypeMetadata.sections.push(sectionMetadataCopy)
          : (ocrCardTypeMetadata.sections[sectionIndex] = sectionMetadataCopy);
        const ocrSectionFields = ocrCard.sections.getOrAdd(sectionName).fields;

        // Настройка прав на секцию в карточке OCR, если права были настроены в исходной карточке
        let ocrSectionPermissions: CardSectionPermissionInfo | null | undefined = null;
        const sectionPermissions = sectionsPermissions?.tryGet(sectionName);
        const fieldsPermissions = sectionPermissions?.tryGetFieldPermissions();
        if (sectionPermissions) {
          ocrSectionPermissions = ocrSectionsPermissions.add(sectionName);
          ocrSectionPermissions.setSectionPermissions(sectionPermissions.sectionPermissions);
        }

        // Поиск поля в метаданных для каждого поля из настроек маппинга в соответствии с найденной секцией
        for (const ocrMappingSettingField of ocrMappingSettingSection.fields) {
          const fieldId = ocrMappingSettingField.id;
          const columnMetadataCopy = sectionMetadataCopy.getColumnById(fieldId);
          if (!columnMetadataCopy) {
            throw new Error(`Can not find column metadata with id '${fieldId}'.`);
          }

          // Вычисление актуального имени поля, из которого будет взято значение для инициализации
          const fieldName = columnMetadataCopy.name || ocrMappingSettingField.name;
          // Создание ключа для доступа к данным
          const ocrAlias = `${sectionName}.${fieldName}`;

          columnMetadataCopy.name = fieldName;
          // Проверка условия, что текущая колонка является ссылочной
          const isColumnComplex = columnMetadataCopy.columnType === CardMetadataColumnType.Complex;
          // Добавление поля из метаданных в карточку операции OCR
          columnMetadataCopy.cardTypeIdList.push(ocrCard.typeId);
          // Если колонка является ссылочной, то получаем для нее метаданные всех связанных физических колонок
          let physicalColumns: ICardMetadataColumn[] = [];
          if (isColumnComplex) {
            physicalColumns = sectionMetadataCopy.columns.getPhysicalColumns(columnMetadataCopy);
            for (const physicalColumn of physicalColumns) {
              // Добавление метаданных каждой физической колонки в карточку операции OCR
              physicalColumn.cardTypeIdList.push(ocrCard.typeId);
            }
          }

          // Поиск ранее верифицированных полей в карточке операции OCR в соответствии с ключом
          let ocrMappingItem = StorageHelper.tryGet<OcrGridData>(ocrMappingStorage, ocrAlias);

          if (!ocrMappingItem) {
            ocrMappingItem = { value: null, displayed: null, modified: false, refs: null };
            if (isColumnComplex) {
              ocrMappingItem.value = {};
              for (const { name } of physicalColumns) {
                const fieldValue = sectionFields?.tryGet(name) ?? null;
                ocrSectionFields.rawSet(name, fieldValue, FieldType.String);
                ocrMappingItem.value[name] = fieldValue;
              }
            } else {
              const fieldValue = sectionFields?.tryGet(fieldName) ?? null;
              ocrSectionFields.rawSet(fieldName, fieldValue, FieldType.String);
              ocrMappingItem.value = fieldValue;
            }
            ocrMappingStorage[ocrAlias] = ocrMappingItem;
          } else {
            if (isColumnComplex) {
              const value = ocrMappingItem.value;
              const isNull = value === null || StorageHelper.isPrimitiveType(value);
              for (const { name } of physicalColumns) {
                const fieldValue = !isNull ? StorageHelper.tryGetValue(value, name) : null;
                ocrSectionFields.rawSet(name, fieldValue ?? null, FieldType.String);
              }
            } else {
              const fieldValue = ocrMappingItem.displayed;
              ocrSectionFields.rawSet(fieldName, fieldValue, FieldType.String);
            }
          }

          // Настройка прав на поле в карточке операции OCR
          if (fieldsPermissions) {
            const setFieldPermissions = (sourceFieldName: string) => {
              const fieldPermissions = fieldsPermissions.tryGet(sourceFieldName);
              if (fieldPermissions != null) {
                ocrSectionPermissions?.setFieldPermissions(sourceFieldName, fieldPermissions);
              }
            };
            // Если колонка является ссылочной, то необходимо установить права для каждой физической колонки
            if (isColumnComplex) {
              for (const { name } of physicalColumns) {
                setFieldPermissions(name);
              }
            } else {
              setFieldPermissions(fieldName);
            }
          }

          // Инициализация контрола в карточке операции OCR
          const ocrControlType = new CardTypeEntryControl();
          ocrControlType.sectionId = sectionMetadataCopy.id;
          if (isColumnComplex) {
            ocrControlType.physicalColumnIdList = physicalColumns.map(c => c.id!);
            ocrControlType.complexColumnId = columnMetadataCopy.id!;
          } else {
            ocrControlType.physicalColumnIdList = [columnMetadataCopy.id!];
          }
          ocrControlType.blockSettings = { StartAtNewLine: true };
          const ocrControlSettings = ocrControlType.controlSettings;

          // Поиск контрола в исходной карточке, связанного с секцией и полем
          const control = cardModel?.controlsBag.find(c => {
            const typeControl = c.cardTypeControl as CardTypeEntryControl;
            return (
              typeControl &&
              Guid.equals(typeControl.sectionId, sectionMetadataCopy.id) &&
              (isColumnComplex
                ? Guid.equals(typeControl.complexColumnId, columnMetadataCopy.id)
                : typeControl.physicalColumnIdList.includes(columnMetadataCopy.id!))
            );
          });

          // Если контрол был найден, то выполняется копирование свойств
          if (control) {
            ocrControlType.name = control.name;
            ocrControlType.caption = control.caption;
            ocrControlType.toolTip = control.tooltip;
            ocrControlType.requiredText = control.requiredText;
            ocrControlType.setReadOnly(control.isReadOnly);
            ocrControlType.setRequired(control.isRequired);
            ocrControlType.setVisible(control.controlVisibility === Visibility.Visible);
            const controlType = control.cardTypeControl as CardTypeEntryControl;
            ocrControlType.displayFormat = controlType?.displayFormat;
            Object.assign(ocrControlSettings, control.cardTypeControl.controlSettings);
          }

          // Установка настроек ссылки
          ocrControlSettings['RefSection'] =
            ocrMappingSettingField.get('ViewRefSection') ?? ocrControlSettings['RefSection'];
          ocrControlSettings['ViewAlias'] =
            ocrMappingSettingField.get('ViewAlias') ?? ocrControlSettings['ViewAlias'];
          ocrControlSettings['ParameterAlias'] =
            ocrMappingSettingField.get('ViewParameter') ?? ocrControlSettings['ParameterAlias'];
          ocrControlSettings['ViewReferencePrefix'] ||=
            ocrMappingSettingField.get('ViewReferencePrefix') || fieldName;

          // Корректировка заголовка
          const ocrMappingFieldCaption = ocrMappingSettingField.get('Caption') as string;
          ocrControlType.caption = ocrMappingFieldCaption || ocrControlType.caption;

          // Корректировка доступности контрола с учётом прав
          if (cardModel) {
            ocrControlType.setReadOnly(
              PermissionHelper.instance.getReadOnlyEntryControl(
                cardModel,
                ocrControlType,
                ocrControlType.getFieldNames(sectionMetadataCopy).fieldNames,
                sectionMetadataCopy.name
              )
            );
          }

          // Корректировка возможности пустого значения
          ocrControlSettings['Nullable'] = columnMetadataCopy.metadataType.isNullable;

          // Установка настроек свойства
          ocrControlsSettings.push({
            alias: ocrAlias,
            control: ocrControlType,
            schemeType: columnMetadataCopy.metadataType.dataType,
            ...ocrControlSettings
          });
        }
      }

      OcrGridDataConverter.serializeToCard(ocrCard, ocrMappingStorage, true);

      return createCardModelWithMetadata(ocrCard, ocrSectionRows, ocrCardTypeMetadata);
    };
  }

  private async getSectionMetadataForType(
    sectionId: string,
    data: {
      readonly cardTypeMetadata: ICardTypeMetadata;
      readonly sectionsIds: readonly string[];
      sections: CardMetadataSectionArray | null;
    }
  ): Promise<[ICardMetadataSection, number] | null> {
    const { cardTypeMetadata, sectionsIds } = data;
    let { sections } = data;

    const sectionIndex = cardTypeMetadata.sections.findIndex(x => Guid.equals(x.id, sectionId));
    if (sectionIndex !== -1) {
      const sectionMetadata = cardTypeMetadata.sections[sectionIndex];
      return [sectionMetadata, sectionIndex];
    }

    if (!sections) {
      sections = data.sections = await this._cardTypeDataLoader.getSections(sectionsIds);
    }

    const sectionMetadata = sections.getSectionById(sectionId);
    return sectionMetadata ? [sectionMetadata, -1] : null;
  }

  //#endregion
}
