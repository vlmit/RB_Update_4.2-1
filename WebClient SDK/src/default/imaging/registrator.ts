import { ExtensionRegistrator } from '@tessa/application';
import {
  IDocLoadFilesBehaviorUIConfigurator$,
  IDocLoadFilesBehaviorUIConfiguratorResolver$
} from 'tessa/ui/imaging';
//import { DocLoadAiIncomingFilesBehaviorUIConfigurator } from './docLoadAiIncomingFilesBehaviorUIConfigurator';

export const ImagingRegistrator: ExtensionRegistrator = {
  async registerTypes(container) {
    // Регистрация Resolver-а конфигураторов
    container
      .bind(IDocLoadFilesBehaviorUIConfiguratorResolver$)
      .toFactory(({ container }) => (id: string) => {
        if (container.isBoundNamed(IDocLoadFilesBehaviorUIConfigurator$, id)) {
          return container.getNamed(IDocLoadFilesBehaviorUIConfigurator$, id);
        } else {
          return undefined;
        }
      });

    // Регистрация конфигураторов.
    // Пока закоментированно, т.к. настройки для DocLoadAiIncomingFilesBehavior с разделением по белой странцие не будут использованы.
    /**
    container
      .bind(IDocLoadFilesBehaviorUIConfigurator$)
      .to(DocLoadAiIncomingFilesBehaviorUIConfigurator)
      .inSingletonScope()
      .whenTargetNamed('DocLoadAiIncomingFilesBehavior');
    */
  }
};
