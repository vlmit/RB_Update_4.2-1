import { ICacheSettingsProvider$ } from '@tessa/application';
import { OperationResponse } from '@tessa/platform';
import {
  showConfirmWithCancel,
  showError,
  showLoadingNetworkError,
  showLoadingOverlay,
  showNotEmpty
} from 'tessa/ui';
import { DeskiMobileOperation, DeskiMobileService } from './deskiMobileService';
import { DeskiMobileInitOperationRequest } from './deskiMobileInitOperationRequest';
import { localize } from 'tessa/localization';
import { DeskiMobileEnhanceOperationResponse } from './deskiMobileEnhanceOperationResponse';
import { DeskiMobileVerifyOperationResponse } from './deskiMobileVerifyOperationResponse';
import { DeskiMobileCancelOperationResponse } from './deskiMobileCancelOperationResponse';
import { ValidationResult, ValidationResultType } from '@tessa/core';
import { retry } from 'tessa/deski/common';
import Platform from 'common/platform';
import { PageLifecycleSingleton } from 'common';

/**
 * Объект, содержащий вспомогательные методы для работы с DeskiMobile.
 * @helper
 */
export namespace DeskiMobileHelper {
  /**
   * Обработчик ошибок для retry на android.
   * Так как на android 14 и выше, при сварачивании приложения сетевые запросы могут завершаться с ошибкой,
   * Проверяет, является ли ошибка сетевой, и если да, то переходим к следующему retry.
   * Если ошибка не является сетевой, то вызывает стандартный обработчик ошибок.
   *
   * @param err ошибка
   * @param networkListener слушатель для сетевых операций
   * @param controller контроллер для AbortController
   */
  const androidErrorHandler = async (
    err: Error,
    networkListener: (signal?: AbortSignal) => Promise<void>,
    controller?: AbortController
  ) => {
    const errorMessage = err?.message ?? '';

    const isNetworkError = errorMessage.includes('Failed to fetch');

    if (isNetworkError) {
      return Promise.resolve(console.error(err));
    } else if (!navigator.onLine) {
      // стандартное поведение
      await showLoadingNetworkError(err, networkListener, controller);
    } else {
      return Promise.reject(err);
    }
  };

  const getOnlyAndroidErrorHandler = () => (Platform.isAndroid() ? androidErrorHandler : undefined);

  const getOnlyAndroidOnline = () => (Platform.isAndroid() ? true : undefined);

  /**
   * Инициализация работы с deep link
   *
   * @param request информация о файлах, над которыми будет выполнена операция.
   * @param operationType тип операции
   * @returns при успешной инициализации:
   *  token - JWT токен для выполнения запросов привязанных к этой операции.
   *  link - ссылка, по которой TESSA Assistant должен подтвердить свое наличие на устройстве пользователя.
   */
  export const startOperation = async (
    request: DeskiMobileInitOperationRequest,
    operationType: 'sign' | 'verify'
  ): Promise<{ token: string; link: string } | null> => {
    const [link, signOperationResult] = await DeskiMobileService.instance.initOperation(
      request,
      operationType
    );

    if (!signOperationResult.isSuccessful) {
      await showNotEmpty(signOperationResult);
      return null;
    }

    if (!link) {
      await showError('Link is null.');
      return null;
    }

    const regex = /(?<=token=)(.*)(?=&url)/g;
    const match = regex.exec(link);
    if (!match) {
      await showError("Can't find token from link.");
      return null;
    }

    const token = match[0];
    return { token, link };
  };

  /**
   * Ожидание подтверждения от TESSA Assistant своего наличия на устройстве пользователя и парсинг результата.
   *
   * @param token JWT токен.
   * @param showError флаг, для показа ошибки при отслеживании Tessa Assistant, по умолчанию true.
   * @param isDeleteOperation флаг, для удаления операции в случае если Tessa Assistant не был запущен, по умолчанию true
   * @param isSuccessfulAssistantStart флаг, был ли запуск Tessa Assistant за последние два часа, по умолчанию false
   *
   * @returns JWT токен, если операция выполнилась успешно, null - при наличии ошибок.
   */
  export const validateTrackingDeskiMobile = async (
    token: string,
    showError = true,
    isDeleteOperation = true,
    isSuccessfulAssistantStart = false
  ): Promise<string | null> => {
    const validationResult = await trackingDeskiMobile(
      token,
      isSuccessfulAssistantStart ? 15000 : 10000,
      isDeleteOperation
    );

    if (!validationResult.isSuccessful) {
      if (showError) {
        await showNotEmpty(validationResult);
      }

      return null;
    }
    return token;
  };

  /**
   * Ожидание подтверждения от TESSA Assistant своего наличия на устройстве пользователя и парсинг результата.
   * В случае, если TESSA Assistant был успешно запущен за последние два часа, увеличиваем время ожидания ответа до 30с
   * и пытаемся вызвать повторно deeplink через 15с. Если успешный вызов TESSA Assistant был более 2 часов или вообще не вызывался,
   * то таймаут ожидания ответа равен 10с.
   *
   * @param operationResultLink deeplink.
   * @param operationResultToken JWT токен
   *
   * @returns JWT токен, если операция выполнилась успешно, null - при наличии ошибок.
   */
  export const handleTrackingDeskiMobile = async (
    operationResultLink: string,
    operationResultToken: string
  ): Promise<string | null> => {
    let token;

    // Проверяем был ли запущен Tessa Assistant за последние два часа
    const isSuccessfulAssistantStart = validateTimeSuccessfulStartAssistant();

    try {
      if (isSuccessfulAssistantStart) {
        token = await transitionAndValidateDeskiMobile(operationResultToken, operationResultLink);
        if (!token) {
          const result = await showConfirmWithCancel(
            localize('$UI_Common_RetryOperationLabel') + '?',
            localize('$UI_Common_DeskiMobileErrorStart')
          );
          if (result) {
            token = await transitionAndValidateDeskiMobile(
              operationResultToken,
              operationResultLink,
              true,
              true
            );
          } else {
            await DeskiMobileService.instance.deleteDeskiMobileOperationRequest(
              operationResultToken
            );
          }
        }
      } else {
        transitionByDeeplink(operationResultLink);
        token = await validateTrackingDeskiMobile(operationResultToken);
      }
    } catch (e) {
      await showError(e);
    }
    return token;
  };

  /**
   * Переход в TESSA Assistant по deeplink
   *
   * @param link deeplink.
   */
  export const transitionByDeeplink = (link: string): void => {
    const showConfirmBeforeUnload = PageLifecycleSingleton.instance.showConfirmBeforeUnload;
    PageLifecycleSingleton.instance.showConfirmBeforeUnload = false;
    window.location.assign(link);
    setTimeout(() => {
      PageLifecycleSingleton.instance.showConfirmBeforeUnload = showConfirmBeforeUnload;
    }, 100);
  };

  /**
   * Запись времени в localStorage, при успешном запуске Tessa Assistant
   *
   */
  export const setTimeSuccessfulStartAssistant = (): void => {
    const cacheSettings = window.tessa.diContainer.get(ICacheSettingsProvider$);
    const data = Date.now().toString();
    localStorage.setItem(cacheSettings.getCacheName('dateSuccessfulStartDeskiMobile'), data);
  };

  /**
   * Валидация времени последнего успешного запуска Tessa Assistant
   *
   * @returns true, если ассистент был запущен менее двух часов назад, иначе false
   */
  export const validateTimeSuccessfulStartAssistant = (): boolean => {
    const cacheSettings = window.tessa.diContainer.get(ICacheSettingsProvider$);
    const dateString = localStorage.getItem(
      cacheSettings.getCacheName('dateSuccessfulStartDeskiMobile')
    );
    if (dateString) {
      const currentTime = Date.now();
      const date = Number(dateString);
      const diffTime = currentTime - date;
      const hoursPassed = diffTime / (1000 * 60 * 60);
      if (hoursPassed < 2) {
        return true;
      }
    }
    return false;
  };

  /**
   * Отслеживание запуска TESSA Assistant.
   * Отображение пользователю loader'а и периодический опрос сервера на наличие изменения состояния операции.
   *
   * @param token JWT токен.
   * @param timeout максимальное время ожидания изменения состояния операции.
   * @param isDeleteOperation флаг для удаления операции в случае, если не удалось запустить TESSA Assistant
   * @returns ошибку, если она возникла.
   */
  export const trackingDeskiMobile = async (
    token: string,
    timeout = 10000,
    isDeleteOperation?: boolean
  ): Promise<ValidationResult> => {
    let responseResult = ValidationResult.empty;

    if (!DeskiMobileService.instance.deskiMobileEnabled) {
      responseResult = ValidationResult.fromError(
        localize(DeskiMobileService.instance.deskiMobileModuleNotAvailableMessage),
        ValidationResultType.Error
      );
      return responseResult;
    }

    try {
      const message = '$UI_Controls_FilesControl_ApplicationAvailabilityProgress';

      await showLoadingOverlay(
        async () =>
          retry(
            async () =>
              await DeskiMobileService.instance.initOperationStateListener(
                token,
                timeout,
                isDeleteOperation
              ),
            {
              errorHandler: getOnlyAndroidErrorHandler(),
              online: getOnlyAndroidOnline(),
              retries: 3
            }
          ),
        {
          text: localize(message)
        }
      );
    } catch (err) {
      responseResult = ValidationResult.fromError(err, ValidationResultType.Error);
    }

    return responseResult;
  };

  /**
   * Отслеживание выполнения операции в TESSA Assistant.
   * Отображение пользователю loader'а и периодический опрос сервера на наличие изменения состояния операции подписи (проверки подписи).
   *
   * @param operation тип операции.
   * @param token JWT токен.
   * @returns
   * - EDSMobileEnhanceOperationResponse - результат выполнения обогащения подписи;
   * - EDSMobileVerifyOperationResponse - результат выполнения проверки подписи;
   * - EDSMobileCancelOperationResponse - результат отмены операции;
   */
  export const trackingOperation = async (
    operation: DeskiMobileOperation,
    token: string
  ): Promise<
    [
      (
        | DeskiMobileEnhanceOperationResponse
        | DeskiMobileVerifyOperationResponse
        | DeskiMobileCancelOperationResponse
        | null
      ),
      ValidationResult
    ]
  > => {
    let responseResult = ValidationResult.empty;
    const controller = new AbortController();
    const label = '$UI_Common_CancelOperationLabel';
    let response: OperationResponse | null = null;

    if (!DeskiMobileService.instance.deskiMobileEnabled) {
      responseResult = ValidationResult.fromError(
        localize(DeskiMobileService.instance.deskiMobileModuleNotAvailableMessage),
        ValidationResultType.Error
      );
      return [null, responseResult];
    }

    let result:
      | DeskiMobileEnhanceOperationResponse
      | DeskiMobileVerifyOperationResponse
      | DeskiMobileCancelOperationResponse
      | null = null;

    try {
      let message = '';
      switch (operation) {
        case DeskiMobileOperation.sign:
          message = '$UI_Controls_FilesControl_DocumentSigningProgress';
          break;
        case DeskiMobileOperation.verify:
          message = '$UI_Controls_FilesControl_DocumentVerifyProgress';
          break;
        default:
          throw new Error('Operation must be "sign" or "verify"');
      }

      response = await showLoadingOverlay(
        async () =>
          retry(
            async () => {
              return await DeskiMobileService.instance.operationListener(token, controller.signal);
            },
            {
              errorHandler: getOnlyAndroidErrorHandler(),
              online: getOnlyAndroidOnline(),
              controller,
              retries: 4
            }
          ),
        {
          text: localize(message),
          button: [
            {
              text: localize(label),
              onClick: () => {
                controller.abort();
              }
            }
          ]
        }
      );

      result = await DeskiMobileService.instance.parseOperationResponse(response);
    } catch (err) {
      if (JSON.stringify(err).indexOf('The operation was aborted.') === -1) {
        responseResult = ValidationResult.fromError(err, ValidationResultType.Error);
      } else {
        result = new DeskiMobileCancelOperationResponse(true);
      }
    }

    return [result, responseResult];
  };

  const transitionAndValidateDeskiMobile = async (
    tokenJWT: string,
    link: string,
    showError = false,
    cancelOperation = true
  ): Promise<string | null> => {
    transitionByDeeplink(link);
    let token = await validateTrackingDeskiMobile(tokenJWT, false, false, true);
    // через 15 секунд, если приложение не открылось, попробовать отрыть еще раз deeplink
    if (!token) {
      transitionByDeeplink(link);
      token = await validateTrackingDeskiMobile(tokenJWT, showError, cancelOperation, true);
    }
    return token;
  };
}
