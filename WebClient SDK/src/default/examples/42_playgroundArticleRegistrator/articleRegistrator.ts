import { ExtensionRegistrator } from '@tessa/application';
import { PlaygroundArticle$ } from 'tessa/ui/playground';

export const ArticleRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    const chunkContainer = container.asLazy(
      () => import(/* webpackChunkName: "playground" */ './exampleArticle')
    );
    chunkContainer.bind(PlaygroundArticle$).to(m => m.ExampleArticle);
  }
};
