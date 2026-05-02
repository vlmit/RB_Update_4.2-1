import { extension } from '@tessa/application';
import { TreeItemExtension } from 'tessa/ui/views/extensions';
import { ITreeItem } from 'tessa/ui/views/workplaces/tree';

@extension()
export class AbTreeViewItemExtension extends TreeItemExtension {
  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.AbTest.AbTreeViewItemExtension';
  }

  public initialize(model: ITreeItem): void {
    console.log(model);
  }
}
