export interface ILinks {
  doc: string;
  playMarket: string;
  appleStore: string;
}

export interface ILinksProvider {
  /**
   * Возвращаем объект ссылок
   */
  getLinks(): ILinks;
}
