import { ODTestButton } from './CertificateButton';
import { ODOpenUserCard } from './OpenUserCard';
import { ODGetCertinfo } from './GetCertInfoFromProfile';
import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

export const Registrator: ExtensionRegistrator = {
  async registerTypes() { },
  async registerExtensions(container) {
    container.registerExtension({
      extension: ODTestButton,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODOpenUserCard,
      stage: ExtensionStage.AfterPlatform
    }).registerExtension({
      extension: ODGetCertinfo,
      stage: ExtensionStage.AfterPlatform
    });
  }
}
