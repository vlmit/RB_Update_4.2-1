import { reaction } from 'mobx';
import { RoleHelper } from 'tessa/roles';
import { TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import { CardRow } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ButtonViewModel } from 'tessa/ui/cards/controls/buttonViewModel';
import {
  ColorProperty,
  PropertyGridBuilder,
  PropertyGridDataProvider,
  PropertyGridHelper
} from 'tessa/ui/propertyGrid';
import {
  AutocompleteDataViewContext,
  IAutocompleteRecord,
  AutocompleteMode
} from 'ui/autocomplete';
import {
  ComplexEntryPropertyFieldsDataProvider,
  EntryPropertyFieldsDataProvider,
  TablePropertyFieldsDataProvider
} from './36_propertyFieldsDataProvider';
import { NumberProperty } from './properties/36_numberPropertyViewModel';
import { NumberPropertyDataSource } from './properties/36_numberPropertyTypes';

/**
 * Расширение для добавления возможности редактирования
 * полей карточки с помощью обозревателя свойств.
 */
@extension({ name: 'PropertyGridUIExtension' })
export class PropertyGridUIExtension extends CardUIExtension {
  //#region fields

  private static readonly rolesCollection = new Map([
    [RoleHelper.departmentRoleTypeName, RoleHelper.departmentRoleTypeId],
    [RoleHelper.personalRoleTypeName, RoleHelper.personalRoleTypeId],
    [RoleHelper.dynamicRoleTypeName, RoleHelper.dynamicRoleTypeId],
    [RoleHelper.contextRoleTypeName, RoleHelper.contextRoleTypeId],
    [RoleHelper.staticRoleTypeName, RoleHelper.staticRoleTypeId],
    [RoleHelper.metaRoleTypeName, RoleHelper.metaRoleTypeId],
    [RoleHelper.taskRoleTypeName, RoleHelper.taskRoleTypeId]
  ]);

  //#endregion

  //#region base overrides

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // поиск полей секции с основной информацией
    const mainSection = context.card.sections.tryGet('AbCarMainInfo');
    // поиск полей табличной секции с информацией по владельцам
    const carOwnersSection = context.card.sections.tryGet('AbCarOwners');
    // поиск блока "Общая информация"
    const block = context.model.blocks.get('MainInfo');
    // поиск кнопки "Open property grid"
    const button = block?.controls.find(c => c.name === 'OpenPropertyGrid') as ButtonViewModel;
    if (mainSection?.fields && !!carOwnersSection && button) {
      // установка действия при нажатии на кнопку
      button.onClick = async () => {
        // создание провайдера данных и указание, что объект не должен быть наблюдаемым
        const propertyGridDataProvider = new PropertyGridDataProvider({}, false, {
          entry: key =>
            new EntryPropertyFieldsDataProvider(propertyGridDataProvider, mainSection, key),
          complexEntry: key =>
            new ComplexEntryPropertyFieldsDataProvider(propertyGridDataProvider, mainSection, key),
          table: key =>
            new TablePropertyFieldsDataProvider(
              propertyGridDataProvider,
              context.model,
              carOwnersSection,
              key
            )
        });

        const propertyGrid = PropertyGridBuilder.create(propertyGridDataProvider)
          .startGroup('$CardTypes_Blocks_GeneralInformation')
          .addColorProperty({
            data: propertyGridDataProvider,
            alias: 'Color',
            caption: '$CardTypes_Controls_Color'
          })
          .addTextProperty({
            data: propertyGridDataProvider,
            alias: 'Xml',
            caption: 'Xml'
          })
          .addSelectorProperty({
            data: propertyGridDataProvider,
            alias: 'NullableGuid',
            caption: '$AbTest_CardTypes_Controls_Guid',
            items: PropertyGridUIExtension.rolesCollection
          })
          .addProperty(() => {
            // создание собственного числового свойства
            const speedData = new NumberPropertyDataSource('MaxSpeed', propertyGridDataProvider);
            const speed = new NumberProperty(speedData, {
              alias: speedData.key,
              caption: '$AbTest_CardTypes_Controls_MaximumSpeed',
              minValue: 1,
              maxValue: 100
            });

            return speed;
          })
          .addAutocompleteProperty({
            alias: 'Driver',
            caption: 'Driver name',
            data: propertyGridDataProvider,
            dataContext: new AutocompleteDataViewContext({
              unique: true,
              viewAlias: 'Users',
              idColumn: 'UserID',
              nameColumn: 'UserName',
              parameterAlias: 'Name',
              multiple: false
            }),
            dataConverter: {
              fromRecord(record: IAutocompleteRecord): object {
                return {
                  ['DriverID']: TypedField.createGuid(String(record.id)),
                  ['DriverName']: record.name ? TypedField.createString(record.name) : null
                };
              },
              toRecord(value: object): IAutocompleteRecord {
                return {
                  id: value['DriverID'],
                  name: value['DriverName']
                };
              }
            },
            onInitialized: async ({ control }) => {
              control.mode = AutocompleteMode.NonDroppable;
              control.menu.openAction.isCollapsed = true;
            }
          })
          .addAutocompleteProperty({
            alias: 'AbCarOwners',
            caption: 'Car owners',
            data: propertyGridDataProvider,
            dataContext: new AutocompleteDataViewContext({
              unique: true,
              viewAlias: 'Users',
              idColumn: 'UserID',
              nameColumn: 'UserName',
              parameterAlias: 'Name',
              multiple: true
            }),
            dataConverter: {
              fromRecord(record: IAutocompleteRecord): object {
                return {
                  ['UserID']: TypedField.createGuid(String(record.id)),
                  ['UserName']: record.name ? TypedField.createString(record.name) : null
                };
              },
              toRecord(value: CardRow): IAutocompleteRecord {
                return {
                  id: value.get('UserID'),
                  name: value.get('UserName')
                };
              }
            },
            onInitialized: async ({ control }) => {
              control.mode = AutocompleteMode.NonDroppable;
              control.menu.openAction.isCollapsed = true;
            }
          })
          .startGroup('$CardTypes_Blocks_AdditionalInformation')
          // создание свойства, отсутствующего в полях карточки
          .addBooleanProperty({
            alias: 'BaseColor',
            caption: '$AbTest_CardTypes_Controls_BaseColor',
            onInitialized: async property =>
              reaction(
                () => property.value,
                () => {
                  const color = propertyGrid.findProperty<ColorProperty>('Color');
                  if (color) {
                    color.visibility = !property.value;
                  }
                },
                { fireImmediately: true }
              )
          })
          .build();

        propertyGrid.title = mainSection.name;

        try {
          // инициализация и отображение обозревателя свойств
          await propertyGrid.initialize();
          await PropertyGridHelper.showDialog(propertyGrid, { autoSizeHeight: true });
        } finally {
          propertyGrid.dispose();
        }
      };
    }
  }

  //#endregion
}
