import { observer } from 'mobx-react-lite';
import { ApplicationExtension } from 'tessa';
import { TreeItemExtension } from 'tessa/ui/views/extensions';
import { ITreeItem, FolderTreeItem } from 'tessa/ui/views/workplaces/tree';
import { IContentProvider, IViewContext } from 'tessa/ui/views';
import { extension } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';

@extension()
export class IframeBlogViewExtension extends TreeItemExtension {
  public getExtensionName(): string {
    // Нужно добавить в ТК
    return 'Tessa.Extensions.Default.Client.Views.IframeBlogViewExtension';
  }

  public initialize(model: ITreeItem): void {
    model.switchExpandOnSingleClick = false;
    if (model instanceof FolderTreeItem) {
      model.hasContent = true;
    }
    model.contentProviderFactory = () => new IframeBlogContentProvider(model);
  }
}

@extension()
export class IframeBlogInitializeExtension extends ApplicationExtension {
  public async initialize(): Promise<void> {
    ComponentsRegistry.instance.register(IframeBlogViewModel, IframeBlogComponent);
  }
}

class IframeBlogContentProvider implements IContentProvider<IframeBlogViewModel> {
  constructor(tree: ITreeItem) {
    this.viewModel = new IframeBlogViewModel(tree);
  }

  readonly viewModel: IframeBlogViewModel;

  readonly viewContext: IViewContext | null = null;

  readonly components: ReadonlyMap<string, IViewContext> = new Map();

  async refresh(): Promise<void> {}

  dispose(): void {}
}

class IframeBlogViewModel {
  constructor(tree: ITreeItem) {
    this.tree = tree;
  }

  readonly tree: ITreeItem;
}

interface IframeBlogComponentProps {
  viewModel: IframeBlogViewModel;
}

const IframeBlogComponent = observer<IframeBlogComponentProps>(function IframeBlogComponent() {
  return (
    <div
      style={{
        height: '100vh'
      }}
    >
      <iframe src="https://blog.mytessa.ru/" style={{ width: '100%', height: '100%' }} />
    </div>
  );
});
