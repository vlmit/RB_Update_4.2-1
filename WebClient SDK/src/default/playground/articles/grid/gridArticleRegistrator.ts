import { StorageHelper } from '@tessa/core';
import { ExtensionRegistrator } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';
import { GridViewModel } from 'ui/grid';
import { HeadlessGrid } from './gridLayoutArticle';
import { registerRowDetails } from './gridRowDetailsArticle';

export const GridArticleRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    registerRowDetails();

    ComponentsRegistry.instance.registerFactory(GridViewModel, {
      componentFactory: () => HeadlessGrid,
      when: viewModel => {
        return (
          viewModel instanceof GridViewModel &&
          !!StorageHelper.tryGet<boolean>(viewModel.info, 'withoutHeader')
        );
      }
    });
  }
};
