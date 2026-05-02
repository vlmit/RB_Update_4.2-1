import { extension, inject } from '@tessa/application';
import { ApplicationExtension, IApplicationExtensionMetadataContext } from 'tessa';
import { showDeeplinkButtons } from './showDeeplinkButtons';
import Platform from 'common/platform';
import { StorageHelper } from '@tessa/core';
import { IUserSettings$ } from 'tessa/userSettings/userSettingsInjects';
import { IUserSettings } from 'tessa/userSettings/userSettingsTypes';

@extension({ name: 'MobileClientDeeplinkInitializationExtension' })
export class MobileClientDeeplinkInitializationExtension extends ApplicationExtension {
  constructor(@inject(IUserSettings$) readonly _userSettings: IUserSettings) {
    super();
  }
  async afterMetadataReceived(context: IApplicationExtensionMetadataContext): Promise<void> {
    if (!context.response || Platform.isNativeApplication() || !Platform.isMobile()) {
      return;
    }

    const { offerTransitionToMobileClientEnabled } = this._userSettings;

    const isAvailableMobileClient =
      StorageHelper.tryGet<boolean>(context.response.info, 'MobileClientIsAvailable') ?? false;

    // Получение данных
    const stayInBrowser = sessionStorage.getItem('StayInBrowser');

    if (!stayInBrowser && offerTransitionToMobileClientEnabled && isAvailableMobileClient) {
      showDeeplinkButtons();
    }
  }
  //#endregion
}
