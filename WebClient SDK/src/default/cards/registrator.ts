import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';

import { AcquaintanceClientStoreExtension } from './acquaintanceClientStoreExtension';
import { OpenFromKrDocStatesOnDoubleClickExtension } from './openFromKrDocStatesOnDoubleClickExtension';
import { KrDocStateClientDeleteExtension } from './krDocStateClientDeleteExtension';
import { CompletionOptionGetTypeIdListRequestExtension } from './completionOptionGetTypeIdListRequestExtension';
import { FunctionRoleGetTypeIdListRequestExtension } from './functionRoleGetTypeIdListRequestExtension';
import { KrPermissionsMandatoryStoreExtension } from './krPermissionsMandatoryStoreExtension';
import { KrCardTaskAssignedRolesAccessProvider } from './krCardTaskAssignedRolesAccessProvider';
import { CardTaskAssignedRolesAccessProviderFactory } from 'tessa/ui/cards';
import { DefaultCardTypeExtensionTypes } from './defaultCardTypeExtensionTypes';
import { OpenStageSettingsOnDoubleClickExtension } from './openStageSettingsOnDoubleClickExtension';
import { RemoveTagsCardStoreExtension } from './removeTagsCardStoreExtension';

export const CardsRegistrator: ExtensionRegistrator = {
  async registerTypes() {},
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: AcquaintanceClientStoreExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OpenFromKrDocStatesOnDoubleClickExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: KrDocStateClientDeleteExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: CompletionOptionGetTypeIdListRequestExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: FunctionRoleGetTypeIdListRequestExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: KrPermissionsMandatoryStoreExtension,
        stage: ExtensionStage.AfterPlatform
      })
      .registerExtension({
        extension: OpenStageSettingsOnDoubleClickExtension,
        stage: ExtensionStage.AfterPlatform,
        order: 6,
        singleton: true
      })
      .registerExtension({
        extension: RemoveTagsCardStoreExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true,
        order: 7
      });

    CardTaskAssignedRolesAccessProviderFactory.instance.setProviderFunc(
      () => new KrCardTaskAssignedRolesAccessProvider()
    );

    DefaultCardTypeExtensionTypes.register();
  }
};
