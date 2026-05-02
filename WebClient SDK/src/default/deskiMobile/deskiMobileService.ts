import { StorageHelper, ValidationResultType } from '@tessa/core';
import { IApiClient, IApiClient$ } from '@tessa/application';
import Platform from 'common/platform';
import { localize } from 'tessa/localization';
import { IStorage } from 'tessa/platform/storage';
import { ValidationResult } from 'tessa/platform/validation';
import { OperationResponse, OperationState } from 'tessa/platform/operations';
import { DeskiMobileInitOperationRequest } from './deskiMobileInitOperationRequest';
import { DeskiMobileEnhanceOperationResponse } from './deskiMobileEnhanceOperationResponse';
import { DeskiMobileVerifyOperationResponse } from './deskiMobileVerifyOperationResponse';
import { DeskiMobileCancelOperationResponse } from './deskiMobileCancelOperationResponse';

export enum DeskiMobileOperation {
  sign = 'sign',
  verify = 'verify',
  preview = 'preview'
}

type DeskiMobileEDSOperation = Omit<DeskiMobileOperation, 'preview'>;

export class DeskiMobileService {
  //#region ctor

  private constructor() {
    this._apiClient = window.tessa.diContainer.get(IApiClient$);
    this._apiClient.defaultHooks.push({
      enhanceOptions: async ctx => {
        ctx.options.credentials = Platform.isNativeApplication() ? 'include' : 'same-origin';
        ctx.options.mode = 'cors';
      }
    });
  }

  //#endregion

  //#region instance

  private static _instance: DeskiMobileService;

  public static get instance(): DeskiMobileService {
    if (!DeskiMobileService._instance) {
      DeskiMobileService._instance = new DeskiMobileService();
    }
    return DeskiMobileService._instance;
  }

  //#endregion

  //#region constants

  public deskiMobileModuleNotAvailableMessage = '$UI_Common_DeskiMobileModuleNotAvailable';

  //#endregion

  //#region fields

  private _deskiMobileEnabled = false;

  private _apiClient: IApiClient;

  //#endregion

  //#region props

  /**
   * Разрешено взаимодействие с "Приложением-ассистент для мобильных устройств"
   */
  get deskiMobileEnabled(): boolean {
    return this._deskiMobileEnabled;
  }

  //#endregion

  //#region methods

  public init(isAvailable: boolean): void {
    this._deskiMobileEnabled = isAvailable;
  }

  /**
   * Создание операции.
   * Требуется для понимания наличия TESSA Assistant на устройстве пользователя.
   *
   * @param request информация о файлах, над которыми будет выполнена операция.
   * @param operation тип генерируемой операции.
   * @returns
   *  string | null - строка-ссылка для подтверждения наличия приложения (выполняется из TESSA Assistant).
   *  ValidationResult - результат выполнения метода.
   */
  public async initOperation(
    request: DeskiMobileInitOperationRequest,
    operation: DeskiMobileEDSOperation
  ): Promise<[string | null, ValidationResult]> {
    let responseResult: ValidationResult = ValidationResult.empty;

    if (!this.deskiMobileEnabled) {
      responseResult = ValidationResult.fromError(
        localize(this.deskiMobileModuleNotAvailableMessage),
        ValidationResultType.Error
      );
      return [null, responseResult];
    }

    let link: string | null = null;
    try {
      link = await this.initOperationRequest(request, operation);
    } catch (err) {
      responseResult = ValidationResult.fromError(err, ValidationResultType.Error);
    }

    return [link, responseResult];
  }

  /**
   * Запрос на сервер для удаления операции.
   * @param token JWT токен
   * @returns status 204 (No Content) - операция успешно удалена, throw - при ошибке.
   */
  public async deleteDeskiMobileOperationRequest(token: string): Promise<void> {
    const url = `/api/v1/mobile/created-operation`;
    await this._apiClient.delete(url, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  //#endregion

  //#region private methods

  public async parseOperationResponse(
    response: OperationResponse | null
  ): Promise<
    | DeskiMobileEnhanceOperationResponse
    | DeskiMobileVerifyOperationResponse
    | DeskiMobileCancelOperationResponse
  > {
    if (!response) {
      throw new Error('Operation was removed without OperationResponse');
    }

    const info = response.tryGetInfo();
    if (!info) {
      throw new Error('Info parameter not found in operation response.');
    }

    const type = StorageHelper.tryGet<string>(info, 'Type');
    if (!type) {
      throw new Error("Can't get type from OperationResponse.");
    }

    if (type.toLocaleLowerCase() === 'cancel') {
      return new DeskiMobileCancelOperationResponse(info);
    }

    if (type.toLocaleLowerCase() === 'enhance') {
      return new DeskiMobileEnhanceOperationResponse(info);
    }

    if (type.toLocaleLowerCase() === 'verify') {
      return new DeskiMobileVerifyOperationResponse(info);
    }

    throw new Error('The type from OperationResponse is invalid.');
  }

  /**
   * Отслеживание изменения состояния операции на Completed.
   *
   * @param token JWT токен.
   * @returns OperationResponse - результат выполнения операции, иначе - null.
   */
  public async operationListener(
    token: string,
    signal: AbortSignal
  ): Promise<OperationResponse | null> {
    return new Promise(async (resolve, reject) => {
      try {
        while (true) {
          if (signal.aborted) {
            const operation = await this.getOperationAndDeleteRequest(token, true);
            const response = operation.Response ? new OperationResponse(operation.Response) : null;
            resolve(response);
            return;
          }

          const operation = await this.getOperationAndDeleteRequest(token, false);
          if (operation.Response || operation.Deleted) {
            const response = operation.Response ? new OperationResponse(operation.Response) : null;
            resolve(response);
            return;
          }

          await this.sleep(1000);
        }
      } catch (err) {
        reject(err);
      }
    });
  }

  /**
   * Отслеживание изменения состояния операции на InProgress.
   *
   * @param token JWT токен.
   * @param timeout максимальное время ожидания изменения состояния операции на InProgress.
   * @param isDeleteOperation флаг для удаления операции в случае, если не удалось запустить TESSA Assistant, по умолчанию true
   * @returns throw ошибку, если она возникла.
   */
  public async initOperationStateListener(
    token: string,
    timeout: number,
    isDeleteOperation = true
  ): Promise<void> {
    const promises: Promise<void>[] = [];

    let timeoutID = 0;
    const controller = new AbortController();
    promises.push(
      new Promise(async (resolve, reject) => {
        try {
          while (true) {
            if (controller.signal.aborted) {
              return;
            }

            const state = await this.getDeskiMobileOperationStateRequest(token);
            if (state === OperationState.InProgress) {
              resolve();
              return;
            }

            await this.sleep(1000);
          }
        } catch (e) {
          reject(e);
        }
      })
    );

    promises.push(
      new Promise((resolve, reject) => {
        timeoutID = window.setTimeout(async () => {
          try {
            controller.abort();

            const state = await this.getDeskiMobileOperationStateRequest(token);
            if (state === OperationState.Completed || state === OperationState.InProgress) {
              resolve();
              return;
            }

            if (isDeleteOperation) {
              await this.deleteDeskiMobileOperationRequest(token);
              reject(
                'No response from assistant application for mobile devices.' +
                  'Check that you have installed the TESSA Assistant app and try again.'
              );
            } else {
              reject("Operation don't delete");
            }
          } catch (err) {
            reject(err);
          }
        }, timeout);
      })
    );

    try {
      return await Promise.race(promises);
    } finally {
      window.clearTimeout(timeoutID);
    }
  }

  /**
   * Метод для ожидания истечения ${timeout} в мс.
   * @param timeout время ожидания в мс
   */
  private async sleep(timeout: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, timeout));
  }

  /**
   * Запрос на сервер для получения статуса операции.
   * @param token JWT токен.
   * @returns статус операции.
   */
  private async getDeskiMobileOperationStateRequest(
    token: string
  ): Promise<OperationState | undefined> {
    const url = `/api/v1/mobile/operation-status`;
    const state: string | null = await this._apiClient
      .get(url, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      })
      .jsonOrNull();
    return state ? OperationState[state] : undefined;
  }

  /**
   * Запрос на сервер для получения полезной нагрузки операции с последующим удалением операции.
   * @param token JWT токен.
   * @param forceDelete Флаг для принудительного удаления операции.
   * @returns
   *    Deleted - была ли удалена операция.
   *    Response - полезная нагрузка операции.
   */
  private async getOperationAndDeleteRequest(
    token: string,
    forceDelete: boolean
  ): Promise<{
    Deleted: boolean;
    Response: IStorage | null;
  }> {
    const url = `/api/v1/mobile/try-get-response-and-delete?forceDelete=${forceDelete}`;
    return await this._apiClient
      .post(url, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      })
      .json();
  }

  /**
   * Запрос на сервер для создания операции.
   * @param request информация о файлах, над которыми будет выполнена операция.
   * @param operation тип генерируемой операции.
   * @returns строка-ссылка для подтверждения наличия приложения (выполняется из TESSA Assistant).
   */
  private async initOperationRequest(
    request: DeskiMobileInitOperationRequest,
    operation: DeskiMobileEDSOperation
  ): Promise<string> {
    const url = `/api/v1/mobile/init-operation?operation=${operation}`;
    return await this._apiClient.post(url, { typedJson: request.serializeToStorage() }).text();
  }

  //#endregion
}
