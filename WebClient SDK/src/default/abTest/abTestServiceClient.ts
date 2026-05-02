import { IStorage } from '@tessa/core';
import { IApiClient, IApiClient$, inject, injectable, createInjectToken } from '@tessa/application';

@injectable()
export class AbTestServiceClient {
  constructor(@inject(IApiClient$) private _apiClient: IApiClient) {}

  // Получает тестовую информацию при нажатии на кнопку тулбара "Получить таблицу" в карточке автомобиля.
  async getCarTableRequest(cardId: string): Promise<IStorage[]> {
    return await this._apiClient
      // запрос на сервер
      .get(`/abtest/car-table-request?id=${encodeURIComponent(cardId)}`)
      // чтение TypedJson
      .typedJson<IStorage[]>();
  }
}

export const AbTestServiceClient$ = createInjectToken<AbTestServiceClient>('AbTestServiceClient');
