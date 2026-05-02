import { assertNotNull } from '@tessa/core';
import {
  IApiClient,
  IApiClient$,
  inject,
  injectable,
  HttpRequestHeaders,
  createInjectToken
} from '@tessa/application';
import { CardGetRequest, CardGetResponse } from '@tessa/platform';

/*
Пример данного расширения представляет собой клиент для обращения
к методам контроллера Tessa.Extensions.Server.Web/Controllers/ServiceController.

'Tessa-Session' в метод пробрасывается, только в демонстративных целях.
IApiClient может автоматически прокидывать токен.

Токен доступа к API необходимо получить у администратора системы
или сгенерировать с помощью пользовательского интерфейса.

Пример использования:
const client = new ServiceClient();
try {
  const token = await client.login('admin', 'admin');
  console.log(token);
  let data = await client.getData('hello!');
  console.log(data);
  data = await client.getDataWhenTokenInParameter(token, 'hello!');
  console.log(data);
  data = await client.getDataWithApiToken(apiToken, 'hello!');
  console.log(data);
  data = await client.getDataWithApiTokenWhenTokenInParameter(apiToken, 'hello!');
  console.log(data);
  data = await client.getDataWithoutCheckingToken('hello!');
  console.log(data);
  const cardId = '11111111-1111-1111-1111-111111111111';
  const cardRequest = new CardGetRequest();
  cardRequest.cardId = cardId;
  let cardResponse = await client.getCard(cardRequest);
  console.log(cardResponse);
  cardResponse = await client.getCardById(cardId);
  console.log(cardResponse);
  await client.logout();
} catch (err) {
  console.error(ValidationResult.fromError(err));
}

*/
@injectable()
export class ServiceClient {
  private _token: string | null;

  constructor(@inject(IApiClient$) private _apiClient: IApiClient) {
    this._token = null;
    // отключаем дефолтное прокидывание токена сессии в хедере
    // будем прокидывать хедер явно
    this._apiClient.defaultHooks.push({
      enhanceOptions: async ctx => {
        ctx.options.ignoreSessionTokenHeader = true;
      }
    });
  }

  // Выполняет вход в систему для интеграционного взаимодействия с веб-сервисом.
  // Возвращает строку с токеном сессии, которую можно использовать для передачи в другие методы для авторизации.
  async login(login: string, password: string): Promise<string> {
    this._token = await this._apiClient
      .post('/service/login', { json: { login, password } })
      .text();
    return this._token;
  }

  // Выходим из системы, закрывая сессию, которая описывается указанным токеном.
  async logout(token?: string): Promise<void> {
    const effectiveToken = token ?? this._token;
    if (!effectiveToken) {
      return;
    }

    await this._apiClient
      .post('/service/logout', {
        searchParams: {
          token: effectiveToken
        }
      })
      .execute();
    this._token = null;
  }

  // Метод сервиса. Может принимать и возвращать произвольные данные.
  async getData(parameter: string): Promise<string> {
    return await this._apiClient
      .get('/service/data', {
        headers: {
          [HttpRequestHeaders.Session]: assertNotNull(this._token)
        },
        searchParams: {
          p: parameter
        }
      })
      .text();
  }

  // Метод сервиса. Может принимать и возвращать произвольные данные.
  async getDataWhenTokenInParameter(token: string, parameter: string): Promise<string> {
    return await this._apiClient
      .get('/service/data', {
        searchParams: {
          p: parameter,
          token
        }
      })
      .text();
  }

  // Метод сервиса. Может принимать и возвращать произвольные данные.
  async getDataWithApiToken(token: string, parameter: string): Promise<string> {
    return await this._apiClient
      .get('/service/data-with-api-token', {
        headers: {
          Authorization: assertNotNull(token)
        },
        searchParams: {
          p: parameter
        }
      })
      .text();
  }

  // Метод сервиса. Может принимать и возвращать произвольные данные.
  async getDataWithApiTokenWhenTokenInParameter(token: string, parameter: string): Promise<string> {
    return await this._apiClient
      .get('/service/data-with-api-token', {
        searchParams: {
          p: parameter,
          token
        }
      })
      .text();
  }

  // Метод сервиса. Может принимать и возвращать произвольные данные.
  // При вызове метода не выполняется проверка наличия токена.
  async getDataWithoutCheckingToken(parameter: string): Promise<string> {
    return await this._apiClient
      .get('/service/data-without-login', {
        searchParams: {
          p: parameter
        }
      })
      .text();
  }

  // Открывает карточку и возвращает CardGetResponse по заданному request.
  async getCard(request: CardGetRequest): Promise<CardGetResponse> {
    const storage = await this._apiClient
      .post('/service/cards/get', {
        headers: {
          [HttpRequestHeaders.Session]: assertNotNull(this._token)
        },
        typedJson: request.getStorage()
      })
      .typedJson();
    return new CardGetResponse(storage);
  }

  // Открывает карточку и возвращает CardGetResponse по указанному ID.
  async getCardById(cardId: string, cardTypeName?: string): Promise<CardGetResponse> {
    const storage = await this._apiClient
      .get(`/service/cards/${encodeURIComponent(cardId)}`, {
        headers: {
          [HttpRequestHeaders.Session]: assertNotNull(this._token)
        },
        searchParams: {
          type: cardTypeName
        }
      })
      .typedJson();
    return new CardGetResponse(storage);
  }
}

export const ServiceClient$ = createInjectToken<ServiceClient>('ServiceClient');
