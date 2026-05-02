import { injectable } from '@tessa/application';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import {
  actions,
  drag,
  groups,
  groupsDirection,
  groupsMix,
  groupsPresets,
  items,
  selection,
  treeGroups,
  treeItems,
  types
} from './mock/data';
import { IListGroup, IListItem, List, ListItem, ListViewModel } from 'ui/list';
import { ValueDataSource } from '@tessa/ui';
import { TreeGroupMock } from './mock/treeGroupMock';
import { ImageItemMock } from './mock/imageItemMock';
import { TreeItemMock } from './mock/treeItemMock';
import { SearchBox } from 'ui/searchBox/searchBox';
import ControlContainer from 'ui/controlContainer';
import './mock/mock.scss';
import { Checkbox, Select } from 'ui';
import { runInAction } from 'mobx';
import { CheckboxViewModel } from 'ui/checkbox/checkboxViewModel';
import { SelectViewModel } from 'ui/select/selectViewModel';
import { MenuAction } from 'tessa/ui';

@injectable()
export class ListArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/List',
      description: `A hierarchical UI element that can display flat and nested groups of items.
      \r\nIn its most basic form it contains only items.`,
      order: 1
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: '',
      props: async () => {
        const itemsDataSource = new ValueDataSource<IListItem[]>(items());
        const viewModel = new ListViewModel(itemsDataSource);
        viewModel.drag = 'body';
        viewModel.drop = 'accept';
        viewModel.showHint = true;
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Items',
      description: 'There are three basic types of items.',
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(types());
        const viewModel = new ListViewModel(itemsDataSource);
        viewModel.showSearchBox = false;
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: '',
      description: 'Non-button items can be assigned actions.',
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(actions());
        const viewModel = new ListViewModel(itemsDataSource);
        viewModel.showSearchBox = false;
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: '',
      description:
        'Non-button items can be selected. The selection area can be controlled via the "selectionArea" property. It can have four values: none, body, checkbox, combined.',
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(selection());
        const viewModel = new ListViewModel(itemsDataSource);
        viewModel.showSearchBox = false;
        viewModel.selection = 'multi';
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: '',
      description:
        'Non-button items can be dragged. The drag type can be controlled via the "drag" property. It can have three values: none, body, handle.',
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(drag());
        const viewModel = new ListViewModel(itemsDataSource);
        viewModel.showSearchBox = false;
        viewModel.showHint = true;
        viewModel.drop = 'accept';
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Groups',
      description: 'Groups can be used to structure the list.',
      props: async () => {
        const itemsDataSource = new ValueDataSource<IListItem[]>(items());
        const groupsDataSource = new ValueDataSource<IListGroup[]>(groups());
        const viewModel = new ListViewModel(itemsDataSource, groupsDataSource);
        viewModel.filterSettings.groups = 'filter';
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: '',
      description: `There are two types of groups: "default" and "virtual". The default type is the normal collapsible group with a caption. The virtual type has no visual elements and is intended to be used for structuring.
      \r\nGroups can contain items and other groups. Items can be reused multiple times across the same or different groups.
      \r\nIn the following example, the groups "Documents" and "Movies" are changed to the virtual type. The group "Everything" contains all the items to demonstrate the reuse of the same items in different groups.`,
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(items());
        const groupsDataSource = new ValueDataSource<IListGroup[]>(groupsMix());
        const viewModel = new ListViewModel(itemsDataSource, groupsDataSource);
        viewModel.filterSettings.groups = 'filter';
        viewModel.drop = 'accept';
        viewModel.drag = 'body';
        viewModel.showHint = true;
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: '',
      description: `Groups have some additional properties to help with structuring the list. Their direction can be set to vertical (default) or horizontal. For horizontal groups, you can enable stretching. Groups with different orientations can be freely nested.`,
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(items());
        const groupsDataSource = new ValueDataSource<IListGroup[]>(groupsDirection());
        const viewModel = new ListViewModel(itemsDataSource, groupsDataSource);
        viewModel.filterSettings.groups = 'filter';
        viewModel.drag = 'body';
        viewModel.drop = 'accept';
        viewModel.showHint = true;
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Filtration',
      description: `There are three types of filtration settings: generic, visibility, and structure.
      \r\nGeneric settings allow you to control casing sensitivity, whether captions or descriptions should be checked, and whether to include groups in filtration. Additionally, a custom filter function can be provided to work with custom types.
      \r\nThe structure setting controls how the original structure should be presented during filtration. There are three options: hide — will only show items; flatten — will only show the immediate parent; preserve — will show all parents of the item or group.`,
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(items());
        const groupsDataSource = new ValueDataSource<IListGroup[]>(groups());
        const viewModel = new ListViewModel(itemsDataSource, groupsDataSource);
        viewModel.filterSettings.structure = 'preserve';
        viewModel.filterSettings.groups = 'ignore';
        await viewModel.initialize();

        const casing = new CheckboxViewModel();
        casing.type = 'switch';
        casing.caption = 'Casing';
        casing.checked = viewModel.filterSettings.casing === 'preserve';
        casing.onChange.add(() =>
          runInAction(() => {
            viewModel.filterSettings.casing = casing.checked ? 'preserve' : 'ignore';
          })
        );
        casing.initialize();

        const groupsSwitch = new CheckboxViewModel();
        groupsSwitch.type = 'switch';
        groupsSwitch.caption = 'Filter groups';
        groupsSwitch.checked = viewModel.filterSettings.groups !== 'ignore';
        groupsSwitch.onChange.add(() =>
          runInAction(() => {
            viewModel.filterSettings.groups = groupsSwitch.checked ? 'filter' : 'ignore';
          })
        );
        groupsSwitch.initialize();

        const captions = new CheckboxViewModel();
        captions.type = 'switch';
        captions.caption = 'Filter captions';
        captions.checked = viewModel.filterSettings.captions !== 'ignore';
        captions.onChange.add(() =>
          runInAction(() => {
            viewModel.filterSettings.captions = captions.checked ? 'filter' : 'ignore';
          })
        );
        captions.initialize();

        const descriptions = new CheckboxViewModel();
        descriptions.type = 'switch';
        descriptions.caption = 'Filter descriptions';
        descriptions.checked = viewModel.filterSettings.descriptions !== 'ignore';
        descriptions.onChange.add(() =>
          runInAction(() => {
            viewModel.filterSettings.descriptions = descriptions.checked ? 'filter' : 'ignore';
          })
        );
        descriptions.initialize();

        const struct = new SelectViewModel();
        struct.values.push(
          MenuAction.create({
            name: 'hide',
            caption: 'hide',
            action: () =>
              runInAction(() => {
                viewModel.filterSettings.structure = 'hide';
                struct.value = 'hide';
              })
          })
        );
        struct.values.push(
          MenuAction.create({
            name: 'preserve',
            caption: 'preserve',
            action: () =>
              runInAction(() => {
                viewModel.filterSettings.structure = 'preserve';
                struct.value = 'preserve';
              })
          })
        );
        struct.values.push(
          MenuAction.create({
            name: 'flatten',
            caption: 'flatten',
            action: () =>
              runInAction(() => {
                viewModel.filterSettings.structure = 'flatten';
                struct.value = 'flatten';
              })
          })
        );
        struct.dropdown.autocloseOnClick = true;
        struct.placeholder = 'Group structure';
        struct.selectType = 'normal';
        struct.themeOptions.border = 'permanent';

        return {
          viewModel,
          groups: groupsSwitch,
          casing,
          captions,
          descriptions,
          struct
        };
      },
      view: ({ viewModel, groups, casing, captions, descriptions, struct }) => (
        <DemoForm
          customStyles={css =>
            css({ flexDirection: 'column', gap: '15px', justifyContent: 'center' })
          }
        >
          <div className="list-filtration-layout-fix" style={{ display: 'flex', gap: '15px' }}>
            <Checkbox viewModel={casing} />
            <Checkbox viewModel={groups} />
            <Checkbox viewModel={captions} />
            <Checkbox viewModel={descriptions} />
            <div style={{ width: 'max-content' }}>
              <Select viewModel={struct} />
            </div>
          </div>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: '',
      description: `Visibility settings allow you to control whether a specific item should be visible with filtration and without filtration. 
      \r\nIt has five options: always — the item or group will always be visible; never — will never be visible during filtration; match — the default behavior; exclusive-always — the item or group will always be visible during filtration and never outside of filtration; exclusive-match — the item or group will only be visible during filtration if it matches the filter. Groups have an additional visibility setting, matchBehavior, which controls whether all its items should be displayed if the group is a match, or only those that match the filter, if any.`
    });

    this.addBlock({
      caption: 'Presets',
      description: `Presets can be used to control different aspects of groups and items in bulk, avoiding the need to define properties for each item or group individually. 
      \r\nGlobal presets can be set at the list level in two ways. The first is through a set of properties defined at the root level of the component interface. This is done to simplify usage in the most basic cases. The second is through the presets prop. Local presets are assigned per group.
      \r\nPresets are inheritable, meaning that each item below the level at which the preset was defined will inherit its value, unless it is redefined by another preset or by its own properties.
      \r\nPresets are set per the following component types: item, button, splitter, group. 
      \r\nFor example, the list below has global selectionArea and drag set to "checkbox" and "handle".`,
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(items());
        const groupsDataSource = new ValueDataSource<IListGroup[]>(groupsPresets());
        const viewModel = new ListViewModel(itemsDataSource, groupsDataSource);
        viewModel.showSearchBox = false;
        viewModel.selectionArea = 'checkbox';
        viewModel.drag = 'handle';
        viewModel.drop = 'accept';
        viewModel.showHint = true;
        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm customStyles={css => css({ flexDirection: 'column' })}>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });

    this.addBlock({
      caption: 'Custom components',
      description: `Custom types allow extending the list appearance beyond default components. To do that, you will need to provide a custom render function that will return custom components for the corresponding types.
      \r\nIt can be used to mock other components and introduce new types of content.`,
      props: async () => {
        const itemsDataSource = new ValueDataSource<ListItem[]>(treeItems());
        const groupsDataSource = new ValueDataSource<IListGroup[]>(treeGroups());
        const viewModel = new ListViewModel(itemsDataSource, groupsDataSource);
        viewModel.showSearchBox = false;
        viewModel.filterSettings.groups = 'filter';
        viewModel.classNames.add('tree-mock');
        viewModel.selection = 'single';

        viewModel.themeOptions.controlTheme.appendFragment({
          list: {
            gap: 'var(--tree-control-root-gap)',
            padding: '0',
            background: 'transparent',
            border: '1 solid transparent',

            items: {
              gap: 'var(--tree-control-root-gap)'
            }
          }
        });

        viewModel.render = type => {
          switch (type) {
            case 'image': {
              return ImageItemMock;
            }

            case 'tree-item': {
              return TreeItemMock;
            }

            case 'tree-group': {
              return TreeGroupMock;
            }
          }

          return undefined;
        };

        await viewModel.initialize();

        return {
          viewModel
        };
      },
      view: ({ viewModel }) => (
        <DemoForm
          customStyles={css =>
            css({ flexDirection: 'column', gap: 'var(--tree-control-root-gap)' })
          }
        >
          <ControlContainer border="permanent">
            <SearchBox viewModel={viewModel.searchBox} />
          </ControlContainer>
          <List viewModel={viewModel} />
        </DemoForm>
      )
    });
  }
}
