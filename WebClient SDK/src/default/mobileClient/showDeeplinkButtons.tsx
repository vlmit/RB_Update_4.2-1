import * as React from 'react';
import * as ReactDOM from 'react-dom';
import styled from 'styled-components';
import { render } from 'ui/renderer';
import { observable, runInAction } from 'mobx';
import { Application } from 'tessa';
import { Icon } from 'ui/icon/icon';
import { deeplinkSchema } from 'tessa/shared/deeplink';
import { localize } from '@tessa/application';
import { Button } from 'ui/button/button';
import { ILinksProvider$, ILinksProvider, ILinks } from './links';
import { PageLifecycleSingleton } from 'common';

const Container = styled.div`
  position: absolute;
  background-color: white;
  z-index: 1000;
  bottom: 0;
  height: 170px;
  width: 100%;
  padding: 10px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  border-top-left-radius: 15px;
  border-top-right-radius: 15px;
  border-top: 1px solid var(--buttons-primary-background);
  border-left: 1px solid var(--buttons-primary-background);
  border-right: 1px solid var(--buttons-primary-background);
`;

const ButtonAccept = styled(Button)`
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  border-width: 0px;
  gap: 5px;
`;

const ContainerButton = styled.div`
  height: 40px;
  width: 100%;
`;

const ContainerText = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  gap: 8px;
`;

const Title = styled.span`
  display: flex;
  justify-content: center;
  width: 100%;

  @media (max-width: 380px) {
    font-size: 15px;
  }
  @media (max-width: 355px) {
    font-size: 14px;
  }
`;

const Link = styled.a`
  padding: 0 2px;
`;

const saveFlagStayInBrowser = () => {
  // Получение данных
  const value = sessionStorage.getItem('StayInBrowser');

  if (!value) {
    sessionStorage.setItem('StayInBrowser', 'stay');
  }
};

const transitionByDeeplink = async (resolvePromise: () => Promise<void>) => {
  const showConfirmBeforeUnload = PageLifecycleSingleton.instance.showConfirmBeforeUnload;
  PageLifecycleSingleton.instance.showConfirmBeforeUnload = false;
  const domainOnBase64 = btoa(`${window.location.origin}${window.__BASE_PATH__}`);
  const pathnameOnBase64 = btoa(Application.instance.history.location.pathname);
  window.location.assign(
    `${deeplinkSchema}://OpenURL?Domain=${domainOnBase64}&Pathname=${pathnameOnBase64}`
  );
  setTimeout(() => {
    PageLifecycleSingleton.instance.showConfirmBeforeUnload = showConfirmBeforeUnload;
  }, 100);
  await resolvePromise();
};

const openDocs = () => {
  const showConfirmBeforeUnload = PageLifecycleSingleton.instance.showConfirmBeforeUnload;
  PageLifecycleSingleton.instance.showConfirmBeforeUnload = false;
  window.location.assign(Deeplink.instance.getLinks().doc);
  setTimeout(() => {
    PageLifecycleSingleton.instance.showConfirmBeforeUnload = showConfirmBeforeUnload;
  }, 100);
};

class Deeplink {
  //#region ctor

  private constructor() {
    this._linksProvider = window.tessa.diContainer.get(ILinksProvider$);
    this._links = this._linksProvider.getLinks();
  }

  private readonly _linksProvider: ILinksProvider;

  private _links: ILinks;

  //#endregion

  //#region instance

  private static _instance: Deeplink;

  static get instance(): Deeplink {
    if (!Deeplink._instance) {
      Deeplink._instance = new Deeplink();
    }
    return Deeplink._instance;
  }

  //#endregion

  //#region fields

  @observable
  private _showButtonAccept = false;

  //#endregion

  //#region props

  getLinks() {
    return this._links;
  }

  get showButtonAccept(): boolean {
    return this._showButtonAccept;
  }
  set showButtonAccept(value: boolean) {
    runInAction(() => (this._showButtonAccept = value));
  }

  //#endregion
}

export const showDeeplinkButtons = async (): Promise<void> => {
  let resolvePromise;
  const promise = new Promise(_resolve => {
    resolvePromise = _resolve;
  });

  if (Deeplink.instance.showButtonAccept) {
    resolvePromise();
    return;
  }

  const div = document.createElement('div');
  document.body.appendChild(div);
  Deeplink.instance.showButtonAccept = true;

  const form: React.ReactElement = (
    <React.Fragment>
      <Container>
        <ContainerText>
          <Title>{localize('$UI_Cards_MobileClient_TitleOpenApp')}</Title>
          <div onClick={() => resolvePromise()}>
            <Icon icon="m-cross" size="m" />
          </div>
        </ContainerText>
        <ContainerText>
          <Title>
            {localize('$UI_Cards_MobileClient_DownloadApp')}
            <Link href={Deeplink.instance.getLinks().appleStore}>
              {localize('$UI_Cards_MobileClient_TitleAppStore')}
            </Link>
            |
            <Link href={Deeplink.instance.getLinks().playMarket}>
              {localize('$UI_Cards_MobileClient_TitleGooglePlay')}
            </Link>
          </Title>
          <div onClick={openDocs}>
            <Icon icon="icon-thin-229" size="m" />
          </div>
        </ContainerText>
        <ContainerButton>
          <ButtonAccept theme="primary" onClick={() => transitionByDeeplink(resolvePromise)}>
            <span>{localize('$UI_Cards_MobileClient_OpenApp')}</span>
          </ButtonAccept>
        </ContainerButton>
        <ContainerButton>
          <ButtonAccept
            theme="transparent"
            onClick={async () => {
              saveFlagStayInBrowser();
              resolvePromise();
            }}
          >
            <span>{localize('$UI_Cards_MobileClient_StayInBrowser')}</span>
          </ButtonAccept>
        </ContainerButton>
      </Container>
    </React.Fragment>
  ) as React.ReactElement;

  render(form, div);

  return promise.then(() => {
    ReactDOM.unmountComponentAtNode(div);
    document.body.removeChild(div);
    Deeplink.instance.showButtonAccept = false;
    return Promise.resolve();
  });
};
