import { ExtensionRegistrator, ExtensionStage } from '@tessa/application';
import { ComponentsRegistry } from '@tessa/ui';

import { CreateCardButtonViewModel, CreateCardExtension } from './createCardExtension';
import { OverrideFilterViewExtension } from './overrideFilterViewExtension';
import { ViewsContextMenuExtension } from './viewsContextMenuExtension';
import { AddTagButtonViewExtension } from './addTagButtonViewExtension';
import { CreateCardCopyButtonViewModel, CreateCardCopyExtension } from './createCardCopyExtension';
import { HelpViewExtension } from './helpViewExtension';
import { TagsInFirstColumnWorkplaceViewComponentExtension } from './tagsInFirstColumnWorkplaceViewComponentExtension';
import { UserAvatarInRowViewExtension } from './userAvatar/userAvatarInRowViewExtension';
import { AccessTokensContextMenuExtension } from './accessTokensContextMenuExtension';
import { InformationLabelViewExtension } from './informationLabelViewExtension';
import { ViewButton } from 'tessa/ui/views/components';
import { ViewInformationLabelViewModel } from './viewInformationLabel/viewInformationLabelViewModel';
import { ViewInformationLabel } from './viewInformationLabel/viewInformationLabel';

export const ViewsRegistrator: ExtensionRegistrator = {
  async registerTypes() {
    ComponentsRegistry.instance.register(CreateCardCopyButtonViewModel, ViewButton);
    ComponentsRegistry.instance.register(CreateCardButtonViewModel, ViewButton);
    ComponentsRegistry.instance.register(ViewInformationLabelViewModel, ViewInformationLabel);
  },
  async registerExtensions(container) {
    container
      .registerExtension({
        extension: AccessTokensContextMenuExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: CreateCardExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: CreateCardCopyExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: OverrideFilterViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: ViewsContextMenuExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: AddTagButtonViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: TagsInFirstColumnWorkplaceViewComponentExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: HelpViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: UserAvatarInRowViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      })
      .registerExtension({
        extension: InformationLabelViewExtension,
        stage: ExtensionStage.AfterPlatform,
        singleton: true
      });
  }
};
