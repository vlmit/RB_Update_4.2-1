import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';

import { ManagerWorkplaceExtension } from './manager/managerWorkplaceExtension';
import { RefSectionExtension } from './refSectionExtension';
import { AutomaticNodeRefreshExtension } from './automaticNodeRefreshExtension';
import { ChartoViewExtension } from './chart-o/chartoExtension';
import { Charto, ChartoViewViewModel } from './chart-o/charto';
import { NoLicenseCharto, NoLicenseChartoViewViewModel } from './chart-o/noLicenceCharto';
import { ManagerWorkplace, ManagerWorkplaceViewModel } from './manager/managerWorkplace';

export const WorkplacesRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    ComponentsRegistry.instance.register(ChartoViewViewModel, Charto);
    ComponentsRegistry.instance.register(NoLicenseChartoViewViewModel, NoLicenseCharto);
    ComponentsRegistry.instance.register(ManagerWorkplaceViewModel, ManagerWorkplace);
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: ManagerWorkplaceExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: RefSectionExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: AutomaticNodeRefreshExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: ChartoViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
