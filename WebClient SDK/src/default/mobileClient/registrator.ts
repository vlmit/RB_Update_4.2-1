import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { MobileClientDeeplinkInitializationExtension } from './mobileClientDeeplinkInitializationExtension';
import { ILinksProvider$, LinksMobileProvider } from './links';

export const MobileClientRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container.bind(ILinksProvider$).to(LinksMobileProvider).inSingletonScope();
  },
  async registerExtensions(container) {
    container.registerExtension({
      extension: MobileClientDeeplinkInitializationExtension,
      stage: ExtensionStage.AfterPlatform,
      singleton: true
    });
  }
};
