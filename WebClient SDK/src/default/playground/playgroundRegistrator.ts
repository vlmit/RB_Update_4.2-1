import { ExtensionRegistrator } from '@tessa/application';
import { IPlaygroundRegistratorResolver$ } from 'tessa/ui/playground';

export const PlaygroundRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container.rebind(IPlaygroundRegistratorResolver$).toConstantValue(async () => {
      const articles = await import(/* webpackChunkName: "playground-articles" */ './articles');
      return Array.from(Object.values(articles));
    });
  }
};
