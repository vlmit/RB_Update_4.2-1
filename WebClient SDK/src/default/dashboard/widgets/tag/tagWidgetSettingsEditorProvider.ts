import { reaction } from 'mobx';
import { Guid } from '@tessa/core';
import { ITagManager } from '@tessa/platform';
import { AutocompleteDataViewContext, AutocompleteMode } from 'ui/autocomplete';
import { selectOrCreateTag } from 'tessa/ui/tags/tagHelper';
import { updateTag } from 'tessa/ui/tags/tagMenuHelper';
import {
  DashboardWidgetSettingsHelper,
  IDashboardWidgetSettingsEditorProvider
} from 'tessa/ui/dashboard';
import {
  AutocompleteProperty,
  IPropertyGrid,
  PropertyGridBuilder,
  PropertyGridBuilderInstance,
  PropertyGridDataProvider,
  TextProperty
} from 'tessa/ui/propertyGrid';
import { NamedReferenceDataConverter } from '../../common/namedReferenceDataConverter';
import { DefaultWidgetNames } from '../widgetNames';
import { TagWidget } from './tagWidget';

/** Провайдер настроек виджета {@link TagWidget}. */
export class TagWidgetSettingsEditorProvider implements IDashboardWidgetSettingsEditorProvider {
  //#region constructors

  /**
   * Создаёт экземпляр класса {@link TagWidgetSettingsEditorProvider}.
   * @param _widget Виджет "Тег".
   * @param _tagManager Менеджер по работе с тегами.
   */
  constructor(
    private readonly _widget: TagWidget,
    private readonly _tagManager: ITagManager
  ) {}

  //#endregion

  //#region properties

  get hasSettingsEditor(): boolean {
    return true;
  }

  //#endregion

  //#region public methods

  async createEditor(modal: boolean): Promise<IPropertyGrid | null> {
    const provider = DashboardWidgetSettingsHelper.createSettingsProvider(this._widget, modal);

    const builder = PropertyGridBuilder.create(provider);
    this.addSpecialSettings(provider, builder);

    return builder.build();
  }

  //#endregion

  //#endregion private methods

  private static getPropertyGridTitle(value: string): string {
    const title = DefaultWidgetNames.TagTitle;
    return value ? `{${title}} "${value}"` : title;
  }

  private addSpecialSettings(
    data: PropertyGridDataProvider,
    builder: PropertyGridBuilderInstance
  ): PropertyGridBuilderInstance {
    return builder
      .startGroup('$Dashboard_Widget_SpecialSettings')
      .addAutocompleteProperty({
        data,
        alias: 'tag',
        caption: '$Dashboard_Widget_Tag_Settings_Tag',
        required: true,
        dataContext: new AutocompleteDataViewContext({
          viewAlias: 'Tags',
          idColumn: 'TagID',
          nameColumn: 'TagName',
          parameterAlias: 'Name'
        }),
        dataConverter: new NamedReferenceDataConverter(false),
        onInitialized: async ({ control }) => {
          control.availability = 'readonly';
          control.toolbar.buttons.availability = 'enabled';
          control.mode = AutocompleteMode.Modal;
          control.dropdown.modalButton.buttonAction = async () => {
            const tags = await selectOrCreateTag([]);
            if (tags?.length) {
              const tagInfo = tags[tags.length - 1];
              control.addRecord({ record: { id: tagInfo.id, name: tagInfo.name } });
            }
          };
          control.menu.openAction.action = async () => {
            const tagId = control.selectedRecord?.model.id as string;
            const result = Guid.isValid(tagId) && (await updateTag(tagId));
            if (result) {
              const isTagAccessible = await this._tagManager.canUseTagAsync(tagId);
              if (isTagAccessible && result.name !== control.selectedRecord?.model.name) {
                control.addRecord({ record: { id: tagId, name: result.name } });
              }
            }
          };
        }
      })
      .addTextProperty({
        data,
        alias: 'caption',
        caption: '$Dashboard_Widget_Tag_Settings_Caption',
        required: true
      })
      .addAutocompleteProperty({
        data,
        alias: 'displayTypes',
        caption: '$Dashboard_Widget_Tag_Settings_DisplayTypes',
        tooltip: '$Dashboard_Widget_Tag_Settings_DisplayTypes_Tooltip',
        dataContext: new AutocompleteDataViewContext({
          viewAlias: 'KrTypesEffective',
          refSection: 'KrCardTypesVirtual',
          idColumn: 'TypeID',
          nameColumn: 'TypeCaption',
          parameterAlias: 'Caption',
          multiple: true,
          unique: true
        }),
        dataConverter: new NamedReferenceDataConverter(false),
        onInitialized: async ({ control }) => {
          control.recordsLineBreak = true;
          control.mode = AutocompleteMode.NonDroppable;
          control.menu.openAction.isCollapsed = true;
        }
      })
      .onGridCreated(grid => {
        grid.toolbarVisibility = false;
        grid.leftCaption = false;
      })
      .onGridInitialized(grid => {
        const tagProperty = grid.findProperty<AutocompleteProperty>('tag')!;
        const captionProperty = grid.findProperty<TextProperty>('caption')!;

        grid.title = TagWidgetSettingsEditorProvider.getPropertyGridTitle(captionProperty.value);
        captionProperty.control.onTextChange.add(({ text }) => {
          grid.title = TagWidgetSettingsEditorProvider.getPropertyGridTitle(text);
        });

        return reaction(
          () => tagProperty.value,
          record => record && (captionProperty.value = record.display)
        );
      });
  }

  //#endregion
}
