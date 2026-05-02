import { StorageHelper } from '@tessa/core';
import { extension } from '@tessa/application';
import { ApplicationExtension, IApplicationExtensionMetadataContext } from 'tessa';
import { DeskiMobileService } from './deskiMobileService';

@extension()
export class DeskiMobileApplicationInitializationExtension extends ApplicationExtension {
  //#region ApplicationExtension

  async afterMetadataReceived(context: IApplicationExtensionMetadataContext): Promise<void> {
    if (!context.response) {
      return;
    }

    const isAvailable =
      StorageHelper.tryGet<boolean>(context.response.info, 'MobileAssistantIsAvailable') ?? false;

    DeskiMobileService.instance.init(isAvailable);
  }

  //#endregion
}
