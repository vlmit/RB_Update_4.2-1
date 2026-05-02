import { observer } from 'mobx-react-lite';
import classNames from 'classnames';
import { extension, localize } from '@tessa/application';
import { TreeItemExtension } from 'tessa/ui/views/extensions';
import { ITreeItem, FolderTreeItem } from 'tessa/ui/views/workplaces/tree';
import { IContentProvider, IViewContext } from 'tessa/ui/views';

@extension()
export class AbCustomFolderViewExtension extends TreeItemExtension {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.AbTest.AbCustomFolderExtension';
  }

  public initialize(model: ITreeItem): void {
    model.switchExpandOnSingleClick = false;
    if (model instanceof FolderTreeItem) {
      model.hasContent = true;
    }
    model.contentProviderFactory = () => new AbCustomFolderContentProvider(model);
  }
}

class AbCustomFolderContentProvider implements IContentProvider<AbCustomFolderViewModel> {
  constructor(tree: ITreeItem) {
    this.viewModel = new AbCustomFolderViewModel(tree);
  }

  public readonly viewModel: AbCustomFolderViewModel;

  public readonly viewContext: IViewContext | null = null;

  public readonly components: ReadonlyMap<string, IViewContext> = new Map();

  public async refresh(): Promise<void> {}

  public dispose(): void {}
}

export class AbCustomFolderViewModel {
  constructor(tree: ITreeItem) {
    this.tree = tree;
  }

  public readonly tree: ITreeItem;
}

interface AbCustomFolderComponentProps {
  viewModel: AbCustomFolderViewModel;
}

export const AbCustomViewContentComponent = observer<AbCustomFolderComponentProps>(
  function AbCustomViewContentComponent(props) {
    const { viewModel } = props;
    let icon = viewModel.tree.isExpanded ? viewModel.tree.expandedIcon : viewModel.tree.icon;
    if (!icon) {
      icon = 'icon-thin-101';
    }
    return (
      <div
        style={{
          height: '90vh',
          fontSize: '80px',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center'
        }}
      >
        <div>
          <i className={classNames('icon ta', icon)} />
          <div
            style={{
              display: 'inline-block'
            }}
          >
            {localize(viewModel.tree.text)}
          </div>
        </div>
      </div>
    );
  }
);
