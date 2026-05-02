import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ICAdESProvider$ } from 'tessa/cards/eds/edsInjects';
import { SignatureSettingsStoreExtension } from './signatureSettingsStoreExtension';
import { SignatureSettingsUIExtension } from './signatureSettingsUIExtension';
import { CryptoProEDSProvider } from './cryptoProEDSProvider';

export const EDSRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container.rebind(ICAdESProvider$).to(CryptoProEDSProvider).inSingletonScope();
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: SignatureSettingsStoreExtension,
        stage: ExtensionStage.BeforePlatform,
        singleton: true
      })
      .registerExtension({
        extension: SignatureSettingsUIExtension,
        stage: ExtensionStage.AfterPlatform
      });
  }
};
