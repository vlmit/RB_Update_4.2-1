import { CardHelper } from '@tessa/platform';
import { IStorage, StorageHelper } from '@tessa/core';
import { localize } from '@tessa/application';
import { IAutocompleteRecord } from 'ui/autocomplete/core/autocompleteTypes';
import { DialogProps } from 'ui/dialog/definitions';
import { UIButton } from 'tessa/ui/uiButton';
import { showConfirm } from 'tessa/ui/tessaDialog/show';
import { IFormDialogManager, IRuntimeRootFormViewModel } from 'tessa/ui/formEditor/types';

/**
 * Helper for API access tokens.
 * @helper
 */
export namespace ApiAccessTokensHelper {
  /** API access tokens layout prefix. */
  export const ApiAccessTokensLayoutPrefix = 'ApiAccessTokens';

  /** API access tokens layout alias for tokens view. */
  export const ApiAccessTokensViewLayout = `${ApiAccessTokensLayoutPrefix}.View`;

  /** API access tokens layout alias for token info. */
  export const ApiAccessTokensInfoLayout = `${ApiAccessTokensLayoutPrefix}.Info`;

  /** API access tokens layout alias for access token. */
  export const ApiAccessTokensTokenLayout = `${ApiAccessTokensLayoutPrefix}.Token`;

  /** API access tokens layout alias for token revoke. */
  export const ApiAccessTokensRevokeLayout = `${ApiAccessTokensLayoutPrefix}.Revoke`;

  /** Scope for API read access. */
  export const ApiReadScope = 'api-read';

  /** Scope for API write access. */
  export const ApiWriteScope = 'api-write';

  /** Autocomplete scope record for API read access. */
  export const ApiReadScopeRecord: IAutocompleteRecord = {
    id: ApiReadScope,
    name: ApiReadScope
  };

  /** Autocomplete scope record for API write access. */
  export const ApiWriteScopeRecord: IAutocompleteRecord = {
    id: ApiWriteScope,
    name: ApiWriteScope
  };

  /**
   * Retrieves the `InDialogScope` value from the storage.
   * @remarks If storage is `undefined` or `null`, or key does not exist, it defaults to returning `false`.
   * @param storage An optional storage object.
   * @returns Value indicating whether the current scope is in a dialog.
   */
  export function getInDialogScope(storage?: IStorage | null): boolean {
    const key = CardHelper.systemKeyPrefix + 'InDialogScope';
    return StorageHelper.tryGet<boolean>(storage, key) ?? false;
  }

  /**
   * Sets the 'InDialogScope' value in the storage.
   * @remarks If storage is `undefined` or `null`, the value will not be set.
   * @param storage An optional storage object.
   * @param value Value to set. Default is `true`.
   */
  export function setInDialogScope(storage?: IStorage | null, value = true): void {
    const key = CardHelper.systemKeyPrefix + 'InDialogScope';
    storage && (storage[key] = value);
  }

  /**
   * Retrieves the `Token` value from the storage.
   * @remarks If storage is `undefined`, `null`, or the key does not exist, it returns `null`.
   * @param storage An optional storage object.
   * @returns The stored token string or `null` if not found.
   */
  export function getToken(storage?: IStorage | null): string | null {
    const key = CardHelper.systemKeyPrefix + 'Token';
    return StorageHelper.tryGet<string>(storage, key) ?? null;
  }

  /**
   * Sets the `Token` value in the storage.
   * @remarks If storage is `undefined` or `null`, the value will not be set.
   * @param storage An optional storage object.
   * @param value Token string to set. If not specified, key will be deleted from {@link storage}.
   */
  export function setToken(storage?: IStorage | null, value?: string | null): void {
    const key = CardHelper.systemKeyPrefix + 'Token';
    storage && value ? (storage[key] = value) : delete storage?.[key];
  }

  /**
   * Retrieves the `Hash` value from the storage.
   * @remarks If storage is `undefined`, `null`, or the key does not exist, it returns `null`.
   * @param storage An optional storage object.
   * @returns The stored hash string or `null` if not found.
   */
  export function getHash(storage?: IStorage | null): string | null {
    const key = CardHelper.systemKeyPrefix + 'Hash';
    return StorageHelper.tryGet<string>(storage, key) ?? null;
  }

  /**
   * Sets the `Hash` value in the storage.
   * @remarks If storage is `undefined` or `null`, the value will not be set.
   * @param storage Optional storage object.
   * @param value Hash string to set. If not specified, key will be deleted from {@link storage}.
   */
  export function setHash(storage?: IStorage | null, value?: string | null): void {
    const key = CardHelper.systemKeyPrefix + 'Hash';
    storage && value ? (storage[key] = value) : delete storage?.[key];
  }

  /**
   * Creates a layout dialog with the specified alias and
   * displays dialog based on given layout alias.
   * @param dialogManager The object to create and initialize layout dialog.
   * @param alias The alias of the layout to create.
   * @param options An object containing the layout dialog component properties. Allows to tweak the dialog's component behavior.
   * @param options.buttons List of buttons presented in the dialog.
   * @param options.initializeAction The action taken to change the card model before calling extensions.
   * @param parameters An object containing the layout dialog UI context data.
   * @param parameters.hash The hash of the current token.
   * @param parameters.token The access token of the current token.
   * @returns A promise that resolves to the result of the dialog interaction.
   */
  export async function openLayoutDialog<R = void>(
    dialogManager: IFormDialogManager,
    alias: string,
    options?: Partial<DialogProps> & {
      buttons?: UIButton[];
      initializeAction?: (form: IRuntimeRootFormViewModel, closeFunc: (result?: R) => void) => void;
    },
    parameters?: {
      hash?: string | null;
      token?: string | null;
    }
  ): Promise<R | void> {
    const info: IStorage = {};
    setHash(info, parameters?.hash);
    setToken(info, parameters?.token);
    let rootFormViewModel: IRuntimeRootFormViewModel | null = null;

    return await dialogManager.showDialog<R>({
      form: alias,
      initializeAction: (form, closeFunc) => {
        rootFormViewModel = form;
        options?.initializeAction?.(form, closeFunc);
      },
      buttons: options?.buttons,
      dialogOptions: {
        mainForm: true,
        backgroundHolder: true,
        showFullscreenButton: true,
        title: localize(options?.title ?? '')
      },
      onDialogClosing: async result => {
        return ((!result && rootFormViewModel?.rootDataProvider?.hasChanges) ?? false)
          ? await showConfirm('$UI_Common_CancelChanges')
          : true;
      },
      onDialogClosed: undefined,
      dialogComponentProps: {
        ...options,
        type: options?.type ?? 'card',
        autoSizeWidth: options?.autoSizeWidth ?? true,
        autoSizeHeight: options?.autoSizeHeight ?? true
      },
      info
    });
  }
}
