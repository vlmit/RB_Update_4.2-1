import { injectable } from '@tessa/application';
import { ILinksProvider, ILinks } from './linksTypes';

const links = {
  doc: 'https://tessa.ru/docs/4.2/usr/user/mobile_client',
  playMarket: 'https://play.google.com/store/apps/details?id=com.syntellect.tessa',
  appleStore: 'https://apps.apple.com/ru/app/tessa-mobile-client/id6473719467'
};

@injectable()
export class LinksMobileProvider implements ILinksProvider {
  //#region public methods
  getLinks(): ILinks {
    // пока хардкодим ссылки, в дальнейшем можно будет их получать с сервера
    return links;
  }

  //#endregion
}
