import { ShadowPropsHelper } from '@tessa/ui';
import { AutocompleteMode } from 'ui/autocomplete';
import { AutocompleteDataViewContext } from 'ui/autocomplete/core/autocompleteDataViewContext';
import {
  DashboardWidgetSettingsHelper,
  IDashboardWidgetSettingsEditorProvider
} from 'tessa/ui/dashboard';
import {
  AutocompleteProperty,
  BooleanProperty,
  IPropertyGrid,
  NumericProperty,
  PropertyGridBuilder,
  PropertyGridBuilderInstance,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import { NamedReferenceDataConverter } from '../../common/namedReferenceDataConverter';
import { DefaultWidgetNames } from '../widgetNames';
import { BirthdayWidget } from './birthdayWidget';

export class BirthdayWidgetSettingsEditorProvider implements IDashboardWidgetSettingsEditorProvider {
  //#region ctor

  constructor(private readonly _widget: BirthdayWidget) {}

  //#endregion

  //#region props

  get hasSettingsEditor(): boolean {
    return true;
  }

  //#endregion

  //#region methods

  async createEditor(modal: boolean): Promise<IPropertyGrid | null> {
    const provider = DashboardWidgetSettingsHelper.createSettingsProvider(this._widget, modal);

    const builder = PropertyGridBuilder.create(provider);
    DashboardWidgetSettingsHelper.addBaseContainerSettings(provider, builder);
    BirthdayWidgetSettingsEditorProvider.addSpecialSettings(provider, builder);

    return builder.build();
  }

  //#endregion

  //#region private methods

  private static addSpecialSettings(
    data: PropertyGridDataProvider,
    builder: PropertyGridBuilderInstance
  ): PropertyGridBuilderInstance {
    return builder
      .startGroup('$Dashboard_Widget_SpecialSettings')
      .addAutocompleteProperty({
        data,
        alias: 'departments',
        caption: '$Dashboard_Widget_Birthday_Settings_Departments',
        dataContext: new AutocompleteDataViewContext({
          unique: true,
          viewAlias: 'Departments',
          idColumn: 'RoleID',
          nameColumn: 'RoleName',
          parameterAlias: 'Name',
          refSection: 'DepartmentRoles',
          multiple: true
        }),
        dataConverter: new NamedReferenceDataConverter(false),
        onInitialized: async ({ control }) => {
          control.recordsLineBreak = true;
          control.maxVisibleRecords = 10;
          control.mode = AutocompleteMode.NonDroppable;
          control.menu.openAction.isCollapsed = true;
        }
      })
      .addBooleanProperty({
        data,
        alias: 'includeSubsidiaryDepartments',
        caption: '$Dashboard_Widget_Birthday_Settings_IncludeSubsidiaryDepartments',
        controlCaption: 'override'
      })
      .onGridInitialized(grid => {
        const departmentsProperty = grid.findProperty<AutocompleteProperty>('departments')!;
        const includeSubsidiaryProperty = grid.findProperty<BooleanProperty>(
          'includeSubsidiaryDepartments'
        )!;

        departmentsProperty.control.records.collectionChanged.add(() => {
          if (departmentsProperty.control.records.length === 0) {
            includeSubsidiaryProperty.value = false;
          }
        });

        ShadowPropsHelper.add(includeSubsidiaryProperty, 'visibility', () => {
          return departmentsProperty.control.records.length > 0;
        });
      })
      .addBooleanProperty({
        data,
        alias: 'showPast',
        caption: '$Dashboard_Widget_Birthday_Settings_ShowPast',
        controlCaption: 'override'
      })
      .addNumericProperty({
        data,
        alias: 'daysAfter',
        caption: '$Dashboard_Widget_Birthday_Settings_DaysAfter',
        controlType: 'Integer',
        tooltip: '$Dashboard_Widget_Birthday_Settings_DaysAfter_Tooltip',
        onInitialized: async ({ control }) => {
          control.minValue = 1;
          control.maxValue = 10;
          control.syncCommittedChanges = false;
        }
      })
      .addBooleanProperty({
        data,
        alias: 'showUpcoming',
        caption: '$Dashboard_Widget_Birthday_Settings_ShowUpcoming',
        controlCaption: 'override'
      })
      .addNumericProperty({
        data,
        alias: 'daysBefore',
        caption: '$Dashboard_Widget_Birthday_Settings_DaysBefore',
        controlType: 'Integer',
        tooltip: '$Dashboard_Widget_Birthday_Settings_DaysBefore_Tooltip',
        onInitialized: async ({ control }) => {
          control.minValue = 3;
          control.maxValue = 20;
          control.syncCommittedChanges = false;
        }
      })
      .onGridInitialized(grid => {
        const showPastProperty = grid.findProperty<BooleanProperty>('showPast')!;
        const daysAfterProperty = grid.findProperty<NumericProperty>('daysAfter')!;
        const showUpcomingProperty = grid.findProperty<BooleanProperty>('showUpcoming')!;
        const daysBeforeProperty = grid.findProperty<NumericProperty>('daysBefore')!;

        showPastProperty.control.onChange.add(() => {
          if (!showPastProperty.value) {
            daysAfterProperty.value = null;
          }
        });
        showUpcomingProperty.control.onChange.add(() => {
          if (!showUpcomingProperty.value) {
            daysBeforeProperty.value = null;
          }
        });

        ShadowPropsHelper.add(daysAfterProperty.control, 'required', () => {
          return showPastProperty.value;
        });
        ShadowPropsHelper.add(daysAfterProperty, 'visibility', () => {
          return showPastProperty.value;
        });
        ShadowPropsHelper.add(daysBeforeProperty.control, 'required', () => {
          return showUpcomingProperty.value;
        });
        ShadowPropsHelper.add(daysBeforeProperty, 'visibility', () => {
          return showUpcomingProperty.value;
        });
      })
      .onGridCreated(grid => {
        grid.title = DefaultWidgetNames.BirthdayTitle;
        grid.leftCaption = false;
      });
  }

  //#endregion
}
