import { injectable } from '@tessa/application';
import { Tree, TreeItemViewModel, TreeViewModel } from 'ui';
import { PlaygroundArticle, DemoForm } from 'tessa/ui/playground/chunk';
import { PlaygroundArticleSettings } from 'tessa/ui/playground';
import { orderableDataSource } from './mock/data';
import { MenuAction, UIButton } from 'tessa/ui';
import {
  PropertyGridBuilder,
  PropertyGridComponent,
  PropertyGridDataProvider
} from 'tessa/ui/propertyGrid';
import { ExpandIconPosition, ExpandIconVariant, NodeIconPosition } from 'ui/tree/definitions';
import { reaction } from 'mobx';
import { TextFieldViewModel } from 'ui/textField';
import { TextFieldView } from 'ui/textField/textFieldView';
import { Visibility } from 'tessa/platform';
import { ButtonComponent } from 'ui/button/button';

@injectable()
export class TreeCustomArticle extends PlaygroundArticle {
  override getSettings(): PlaygroundArticleSettings {
    return {
      name: 'Controls/Tree/Customization',
      description: 'Customize your Tree-control',
      order: 4
    };
  }

  override async initialize(): Promise<void> {
    this.addBlock({
      caption: 'Sandbox',
      props: async () => {
        const viewModel = new TreeViewModel(orderableDataSource);
        viewModel.onContextMenu.add(({ actions, item }) => {
          const itemViewModel: TreeItemViewModel | undefined = viewModel.selectedItems
            .values()
            .next().value;

          actions.push(
            MenuAction.create({
              type: 'normal',
              name: 'Select',
              caption: 'Select',
              isEnabled: !item.isReadonly,
              action: async () => {
                viewModel.selectItem(item);
              }
            }),
            MenuAction.create({
              type: 'normal',
              name: 'Delete',
              caption: 'Delete',
              isEnabled: !item.isReadonly,
              action: async () => {
                viewModel.removeItems(item);
              }
            }),
            MenuAction.create({
              type: 'normal',
              name: 'Disable',
              caption: `${item.isReadonly ? 'Enable' : 'Disable'}`,
              action: async () => {
                item.isReadonly = !item.isReadonly;
              }
            }),
            MenuAction.create({
              type: 'normal',
              name: 'Move',
              caption: !itemViewModel
                ? 'Select the element'
                : `Move ${itemViewModel.caption} to ${item.caption}`,

              isEnabled: !!itemViewModel,
              action: async () => {
                if (!itemViewModel) {
                  return;
                }
                viewModel.traverseItem(itemViewModel, item);
              }
            })
          );
        });
        viewModel.treeItemSlotExtension.after = item => (
          <>
            {item.orderDownAvailable && (
              <ButtonComponent
                name="moveDown"
                icon="m-drop"
                type="small"
                theme="transparent"
                onClick={() => {
                  item.orderDown();
                }}
              />
            )}
            {item.orderUpAvailable && (
              <ButtonComponent
                name="moveUp"
                icon="m-up"
                type="small"
                theme="transparent"
                onClick={() => {
                  item.orderUp();
                }}
              />
            )}
          </>
        );
        viewModel.canReorder = true;
        await viewModel.initialize();

        const propertyGrid = await this.initializePropertyGrid(viewModel);

        const textFieldViewModel = new TextFieldViewModel();
        textFieldViewModel.placeholder = 'Enter element name and select the node';
        const addNewElementButton = UIButton.create({
          buttonAction: async () => {
            const { text } = textFieldViewModel;
            const item = Array.from(viewModel.selectedItems.values())[0];
            const newItem = await viewModel.addItem({
              name: text,
              caption: text,
              parent: item.caption ?? null
            });
            console.log(newItem);
          },
          caption: () => {
            const item = Array.from(viewModel.selectedItems.values())[0];
            return `Add to ${item?.caption ?? ''}`;
          },
          visibility: () => {
            const { text } = textFieldViewModel;
            const item = Array.from(viewModel.selectedItems.values())[0];
            return text && item ? Visibility.Visible : Visibility.Hidden;
          },
          type: 'small',
          theme: 'control'
        });
        textFieldViewModel.toolbar.buttons.add(addNewElementButton);
        textFieldViewModel.initialize();

        return {
          viewModel,
          propertyGrid,
          textFieldViewModel
        };
      },
      view: ({ viewModel, propertyGrid, textFieldViewModel }) => {
        return (
          <DemoForm customStyles={css => css({ flexDirection: 'column', gap: '10px' })}>
            <PropertyGridComponent modal={false} viewModel={propertyGrid} />
            <TextFieldView viewModel={textFieldViewModel} />
            <Tree viewModel={viewModel} />
          </DemoForm>
        );
      }
    });
  }

  private async initializePropertyGrid(viewModel: TreeViewModel) {
    const dataProvider = new PropertyGridDataProvider({
      showIconProp: true,
      smallNodeCaption: false,
      searchBoxVisibility: false,
      nodeIconPosition: 'start',
      expandIconPosition: 'start',
      expandIconVariant: 'plusMinus',
      searchBoxTheme: 'embedded'
    });
    const propertyGrid = PropertyGridBuilder.create(dataProvider)
      .addBooleanProperty({
        alias: 'showIconProp',
        caption: 'Show icon',
        data: dataProvider,
        onInitialized: async property => {
          property.control.onChange.add(() => {
            viewModel.showIcon = !viewModel.showIcon;
          });
        }
      })
      .addBooleanProperty({
        alias: 'smallNodeCaption',
        caption: 'Small node caption',
        data: dataProvider,
        onInitialized: async property => {
          viewModel.styles.add(
            css => css`
              & .tree__caption {
                font-size: ${property.control.checked ? 12 : 18}px;
              }
            `
          );
        }
      })
      .addBooleanProperty({
        alias: 'searchBoxVisibility',
        caption: 'Show search',
        data: dataProvider,
        onInitialized: async property => {
          property.control.onChange.add(() => {
            viewModel.searchBoxVisibility = !viewModel.searchBoxVisibility;
          });
        }
      })
      .addSelectorProperty({
        alias: 'nodeIconPosition',
        caption: 'Node icon position',
        data: dataProvider,
        items: new Map([
          ['start', 'start'],
          ['end', 'end']
        ]),
        onInitialized: async property => {
          reaction(
            () => property.value,
            value =>
              (viewModel.treeIconSettings.nodeIconPosition = value?.model
                .id as unknown as NodeIconPosition)
          );
        }
      })
      .addSelectorProperty({
        alias: 'expandIconVariant',
        items: new Map([
          ['plusMinus', 'plusMinus'],
          ['arrow', 'arrow']
        ]),
        caption: 'Expand Icon Variant',
        data: dataProvider,
        onInitialized: async property => {
          reaction(
            () => property.value,
            value =>
              (viewModel.treeIconSettings.expandIconVariant = value?.model
                .id as unknown as ExpandIconVariant)
          );
        }
      })
      .addSelectorProperty({
        alias: 'expandIconPosition',
        items: new Map([
          ['start', 'start'],
          ['end', 'end']
        ]),
        caption: 'Expand Icon position',
        data: dataProvider,
        onInitialized: async property => {
          reaction(
            () => property.value,
            value =>
              (viewModel.treeIconSettings.expandIconPosition = value?.model
                .id as ExpandIconPosition)
          );
        }
      })
      .addSelectorProperty({
        alias: 'searchBoxTheme',
        items: new Map([
          ['embedded', 'embedded'],
          ['standalone', 'standalone']
        ]),
        caption: 'Search element theme',
        data: dataProvider,
        onInitialized: async property => {
          reaction(
            () => property.value,
            value => (viewModel.searchBoxTheme = value?.model.name as 'standalone' | 'embedded')
          );
        }
      })

      .build();

    await propertyGrid.initialize();
    return propertyGrid;
  }
}
