import { extension } from '@tessa/application';
import { moveTagToFirstColumn } from 'tessa/ui/tags/tagViewsHelper';
import { IWorkplaceViewComponent } from 'tessa/ui/views';
import { TableGridViewModel } from 'tessa/ui/views/content';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';

@extension({ name: 'TagsInFirstColumnWorkplaceViewComponentExtension' })
export class TagsInFirstColumnWorkplaceViewComponentExtension extends WorkplaceViewComponentExtension {
  getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.TagsInFirstColumnWorkplaceViewComponentExtension';
  }
  initialized(model: IWorkplaceViewComponent): void {
    const table = model.content.get('Table');
    if (table instanceof TableGridViewModel) {
      table.tagsInFirstColumn = true;

      if (moveTagToFirstColumn(table)) {
        table.rebuild();
      }
    }
  }
}
