import { extension, inject, localize } from '@tessa/application';
import { FieldType, IStorage, StorageHelper, TypedField } from '@tessa/core';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { GridRowAction, GridViewModel } from 'tessa/ui/cards/controls';
import {
  IDocLoadFilesBehaviorUIConfigurator,
  IDocLoadFilesBehaviorUIConfiguratorResolver,
  IDocLoadFilesBehaviorUIConfiguratorResolver$
} from 'tessa/ui/imaging';
import {
  AutocompleteProperty,
  MultiplePropertyGrid,
  PropertyGridBuilder,
  PropertyGridDataProvider,
  PropertyGridHelper
} from 'tessa/ui/propertyGrid';
import {
  ComplexEntryRowPropertyFieldsDataProvider,
  EntryPropertyRowFieldsDataProvider
} from './fieldsDataProviders';
import {
  AutocompleteDataViewContext,
  IAutocompleteRecord,
  AutocompleteMode
} from 'ui/autocomplete';
import { Button } from 'ui/button/buttonViewModel';
import { observable, reaction, runInAction } from 'mobx';
import { CardTypeColumn } from '@tessa/platform';

/** Расширение для модификации строки диалога настройки параметров обработчик потокового ввода. */
@extension({ name: 'DocLoadFilesBehaviorSettingsUIExtension' })
export class DocLoadFilesBehaviorSettingsUIExtension extends CardUIExtension {
  //#region constructors

  constructor(
    @inject(IDocLoadFilesBehaviorUIConfiguratorResolver$)
    private readonly _uiConfiguratorResolver: IDocLoadFilesBehaviorUIConfiguratorResolver
  ) {
    super();
  }

  //#endregion

  //#regoion private methods

  private async getUIConfigurator(
    name: string | undefined | null
  ): Promise<IDocLoadFilesBehaviorUIConfigurator | undefined> {
    if (name) {
      return this._uiConfiguratorResolver(name);
    }
    return;
  }

  //#endregion

  //#region base overrides

  public override async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (!context.validationResult.isSuccessful) {
      return;
    }

    const subfoldersSettingsTableControlAlias = 'DocLoadSubfolderSettingsTable';

    const controls = context.model.controls;
    const subfoldersSettingsGrid = controls.get(
      subfoldersSettingsTableControlAlias
    ) as GridViewModel;

    subfoldersSettingsGrid.gridCellFormatFunc = context => {
      const typeColumn = Object.entries(context.column).filter(
        ([key, _value]) => key === '_type'
      )[0]?.[1] as CardTypeColumn;
      if (
        typeColumn &&
        typeColumn.caption === '$CardTypes_Bloks_Columns_DocLoad_SubfolderSettings'
      ) {
        const behaviorAlias = context.row.getString('BehaviorAlias');
        if (behaviorAlias) {
          const uiConfigurator = this._uiConfiguratorResolver(behaviorAlias);
          if (uiConfigurator) {
            return uiConfigurator.getReadableSettings(context.formattedValue);
          }
        }
        return context.formattedValue === '{}' ? '' : context.formattedValue;
      }
      return context.formattedValue;
    };

    subfoldersSettingsGrid.rowInvoked.add(async e => {
      if (e.action === GridRowAction.Inserted || e.action === GridRowAction.Opening) {
        // Определим текущие настройки для выбранного Behavior-а
        const currentSettingsString = StorageHelper.tryGetValue(
          e.row.getStorage(),
          'BehaviorSettings',
          FieldType.String
        );
        const settings = observable.object(
          currentSettingsString ? (JSON.parse(currentSettingsString) as IStorage) : {}
        );
        // Соберём PropertyGrid вместо формы редактирования строки таблицы
        const propertyGridDataProvider = new PropertyGridDataProvider({}, false, {
          entry: key =>
            new EntryPropertyRowFieldsDataProvider<FieldType.String>(
              propertyGridDataProvider,
              e.row,
              key,
              value => TypedField.create(value, FieldType.String)
            ),
          complexEntry: key =>
            new ComplexEntryRowPropertyFieldsDataProvider(
              propertyGridDataProvider,
              e.row,
              key,
              ...['ID', 'Name', 'Alias', 'Settings']
            )
        });
        const multipleGrid = new MultiplePropertyGrid();
        const propertyGridBuilder = PropertyGridBuilder.create(propertyGridDataProvider)
          .addTextProperty({
            data: propertyGridDataProvider,
            alias: 'Path',
            required: true,
            caption: '$CardTypes_Controls_Columns_DocLoad_SubfolderPath',
            tooltip: '$CardTypes_Controls_DocLoad_SubfolderPath_Tooltip',
            onInitialized: async property => {
              property.control.validationContainer.add(context => {
                if (property.control.required && !property.control.text) {
                  context.addError({
                    fieldName: 'Path',
                    message: localize(
                      '$CardTypes_Validators_DocLoad_FolderBehaviorsSettings_SubfolderPath'
                    )
                  });
                  context.handled = true;
                }
              });
            }
          })
          .addAutocompleteProperty({
            data: propertyGridDataProvider,
            alias: 'Behavior',
            required: true,
            caption: '$CardTypes_Controls_Columns_DocLoad_Behavior',
            dataContext: new AutocompleteDataViewContext({
              unique: true,
              viewAlias: 'DocLoadBehaviors',
              idColumn: 'BehaviorID',
              nameColumn: 'BehaviorName',
              parameterAlias: 'Name',
              multiple: false
            }),
            dataConverter: {
              fromRecord(record: IAutocompleteRecord): object {
                return {
                  ['BehaviorID']: TypedField.createGuid(String(record.id)),
                  ['BehaviorName']: record.name ? TypedField.createString(record.name) : null,
                  ['BehaviorAlias']: record.data
                    ? StorageHelper.tryGetValue(record.data, 'BehaviorAlias', FieldType.String)
                    : null,
                  ['BehaviorSettings']: record.data
                    ? StorageHelper.tryGetValue(record.data, 'BehaviorSettings', FieldType.String)
                    : null
                };
              },
              toRecord(value: object): IAutocompleteRecord {
                return {
                  id: value['BehaviorID'],
                  name: value['BehaviorName'],
                  data: {
                    BehaviorAlias: value['BehaviorAlias'],
                    BehaviorSettings: value['BehaviorSettings']
                  }
                };
              }
            },
            onInitialized: async ({ control }) => {
              control.mode = AutocompleteMode.NonDroppable;
              control.menu.openAction.isCollapsed = true;
              control.validationContainer.add(context => {
                if (control.required && !control.record?.model.id) {
                  context.addError({
                    fieldName: 'Behavior',
                    message: localize(
                      '$CardTypes_Validators_DocLoad_FolderBehaviorsSettings_Behavior'
                    )
                  });
                  context.handled = true;
                }
              });
            }
          });

        propertyGridBuilder.onGridInitialized(grid => {
          const behaviorProperty = grid.findProperty<AutocompleteProperty>('Behavior')!;
          this.disposeList.add(
            reaction(
              () => behaviorProperty.value,
              async record => {
                // Определим текущие настройки для выбранного Behavior-а
                const newSettingsString = StorageHelper.tryGetValue(
                  record?.model.data,
                  'BehaviorSettings',
                  FieldType.String
                );
                if (newSettingsString) {
                  // Не меняя ссылки перенаполняем объек настройками по умолчанию, которые должны прийти вместе с выбраным алиасом Behavior-а
                  const newSettings = JSON.parse(newSettingsString) as IStorage;
                  runInAction(() => {
                    Object.keys(settings).forEach(key => delete settings[key]);
                    Object.entries(newSettings).forEach(([key, value]) => (settings[key] = value));
                  });
                }

                const newBehaviorAlias = StorageHelper.tryGetValue(
                  record?.model.data,
                  'BehaviorAlias',
                  FieldType.String
                );

                if (multipleGrid.grids.length > 1) {
                  multipleGrid.grids.forEach(grid => {
                    if (
                      grid.title == newBehaviorAlias ||
                      grid.title == '$CardTypes_Controls_DocLoad_FolderBehaviorsSettings'
                    ) {
                      return;
                    }
                    multipleGrid.removeGrid(grid);
                    grid.dispose();
                  });
                }

                const mewUiConfigurator = await this.getUIConfigurator(newBehaviorAlias);
                const settingsGrid = await mewUiConfigurator?.getSettingsControlsGrid(settings);
                if (settingsGrid) {
                  multipleGrid.addGrid(settingsGrid);
                }
              },
              { fireImmediately: true }
            )
          );
        });

        const propertyGrid = propertyGridBuilder.build();
        propertyGrid.title = '$CardTypes_Controls_DocLoad_FolderBehaviorsSettings';
        propertyGrid.toolbarVisibility = false;
        multipleGrid.addGrid(propertyGrid);

        try {
          // инициализация и отображение обозревателя свойств
          await multipleGrid.initialize();
          if (e.action === GridRowAction.Opening) {
            multipleGrid.buttons.clear();
            multipleGrid.buttons.add(
              Button.create({
                name: PropertyGridHelper.CancelButtonName,
                caption: '$UI_Common_Close',
                type: 'normal',
                theme: 'secondary',
                buttonAction: async () => {
                  await multipleGrid.close(false);
                }
              })
            );
          }

          const result = await PropertyGridHelper.showColumnMultipleDialog(multipleGrid, {
            autoSizeHeight: true,
            type: 'controls',
            maxWidth: '100%',
            autoSizeWidth: false
          });
          const subfolderSection = context.model.card.sections.tryGet('DocLoadSubfoldersSettings');
          e.row.set('BehaviorSettings', TypedField.tryCreateByValueType(JSON.stringify(settings)));
          if (result) {
            if (e.action === GridRowAction.Inserted) {
              subfolderSection?.rows.add(e.row);
            }
          }
        } finally {
          e.cancel = true;
          propertyGrid.dispose();
        }
      }
    });
  }

  //#endregion
}
