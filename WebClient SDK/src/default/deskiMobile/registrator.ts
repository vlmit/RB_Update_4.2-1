import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ISignaturesVerifier$ } from 'tessa/cards/eds/edsInjects';
import { DeskiMobileSignaturesVerifier } from './deskiMobileSignaturesVerifier';
import { DeskiMobileApplicationInitializationExtension } from './deskiMobileApplicationInitializationExtension';
import { DeskiMobileFileExtension } from './deskiMobileFileExtension';

export const DeskiMobileRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    container.rebind(ISignaturesVerifier$).to(DeskiMobileSignaturesVerifier).inSingletonScope();
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: DeskiMobileApplicationInitializationExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: DeskiMobileFileExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
