import { ExtensionRegistrator } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';
import { ExampleDropdownComponent, ExampleDropdownViewModel } from './buttonArticle';

export const ButtonArticleRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    ComponentsRegistry.instance.register(ExampleDropdownViewModel, ExampleDropdownComponent);
  }
};
