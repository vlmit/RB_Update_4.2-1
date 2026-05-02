import * as React from 'react';
import Dialog from 'ui/dialog/dialog';
import DialogContainer from 'ui/dialog/dialogContainer/dialogContainer';
import DialogContent from 'ui/dialog/dialogContent/dialogContent';
import { localize } from 'tessa/localization';
import { showViewModelDialog } from 'tessa/ui';
import Platform from 'common/platform';
import { Button } from 'ui/button/button';
import DialogFooter from 'ui/dialog/dialogFooter/dialogFooter';
import './cryptoPro.scss';

interface DialogCryptoProUnavailableProps {
  viewModel: DialogCryptoProUnavailableViewModel;
  onClose: () => void;
}

class DialogCryptoProUnavailable extends React.Component<DialogCryptoProUnavailableProps> {
  //#region react

  public render(): React.ReactElement {
    const { link } = this.props.viewModel;

    return (
      <Dialog
        isOpened={true}
        noPortal={true}
        autoSizeWidth={true}
        autoSizeHeight={true}
        onCloseRequest={this.handleCloseForm}
        showChrome={true}
      >
        <DialogContainer>
          <DialogContent>
            <div className="crypto-pro-dialog">
              <div>{localize('$Common_CryptoPro_Plugin_Unavailable_Message')}</div>
              <div>
                <a href={link} target="_blank" rel="noreferrer">
                  {localize('$Common_CryptoPro_Download_Plugin')}
                </a>
              </div>
            </div>
          </DialogContent>
          <DialogFooter className="default-footer">
            <Button
              onClick={this.handleCloseForm}
              caption="$UI_Common_Close"
              type="normal"
              theme="primary"
            />
          </DialogFooter>
        </DialogContainer>
      </Dialog>
    );
  }

  //#endregion

  //#region handlers

  private handleCloseForm = () => {
    this.props.onClose();
  };

  //#endregion
}

class DialogCryptoProUnavailableViewModel {
  constructor(link: string) {
    this._link = link;
  }

  private _link: string;

  public get link(): string {
    return this._link;
  }
}

enum CryptoProPluginLink {
  default = 'https://chrome.google.com/webstore/detail/cryptopro-extension-for-c/iifchhfnnmpdbibifmljnfjhpififfog?hl=ru',
  Firefox = 'https://www.cryptopro.ru/sites/default/files/products/cades/extensions/firefox_cryptopro_extension_latest.xpi',
  Safari = 'https://docs.cryptopro.ru/cades/plugin/plugin-installation-macos'
}

const getCryptoProPluginLink = (): string => {
  // дефолтное значение ссылки для Chrome, Edge, Opera и Yandex Browser
  let browserLink = CryptoProPluginLink.default;

  if (Platform.isFirefox) {
    browserLink = CryptoProPluginLink.Firefox;
  }

  if (Platform.isMacOsSafari) {
    browserLink = CryptoProPluginLink.Safari;
  }

  return browserLink;
};

export const showDialogCryptoProUnavailable = async (): Promise<void> => {
  const pluginLink = getCryptoProPluginLink();
  const dialogCryptoProViewModel = new DialogCryptoProUnavailableViewModel(pluginLink);
  await showViewModelDialog(dialogCryptoProViewModel, DialogCryptoProUnavailable);
};
