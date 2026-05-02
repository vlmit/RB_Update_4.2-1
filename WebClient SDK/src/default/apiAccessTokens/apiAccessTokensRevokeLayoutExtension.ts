import {
  ApiAccessTokenDeleteRequest,
  IApiAccessTokenService,
  IApiAccessTokenService$
} from '@tessa/platform';
import { extension, inject, localize } from '@tessa/application';
import { TextFieldViewModel } from 'ui/textField';
import { Button } from 'ui/button/buttonViewModel';
import { showLoadingOverlay } from 'tessa/ui/loadingOverlay';
import { showNotEmpty } from 'tessa/ui/tessaDialog/showNotEmpty';
import { IFormDialogManager } from 'tessa/ui/formEditor/types';
import { IFormDialogManager$ } from 'tessa/ui/formEditor/injects';
import { IFormExtensionContext } from 'tessa/ui/formEditor/extensions/formExtension';
import { ApiAccessTokensBaseFormExtension } from './apiAccessTokensBaseFormExtension';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';

@extension({ name: 'ApiAccessTokensRevokeLayoutExtension' })
export class ApiAccessTokensRevokeLayoutExtension extends ApiAccessTokensBaseFormExtension {
  //#region constructors

  constructor(
    @inject(IFormDialogManager$) private readonly _dialogManager: IFormDialogManager,
    @inject(IApiAccessTokenService$) private readonly _apiAccessTokenService: IApiAccessTokenService
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initialized(context: IFormExtensionContext): Promise<void> {
    const hash = ApiAccessTokensHelper.getHash(context.runtimeView?.info);
    if (!hash) {
      return;
    }

    const block = context.runtimeView?.getRootBlock();
    const data = context.runtimeView?.rootDataProvider;
    if (!data || !block || !context.validationResult.isSuccessful) {
      return;
    }

    const revoke = this.getControl<Button>(block, 'Revoke');
    if (revoke) {
      revoke.buttonAction = async () => {
        await showLoadingOverlay(
          async () => {
            const reason = this.getControl<TextFieldViewModel>(block, 'Reason');
            const request = new ApiAccessTokenDeleteRequest(hash, reason?.text);
            const response = await this._apiAccessTokenService.revoke(request);
            const validationResult = response.validationResult.build();
            validationResult.isSuccessful && (await this._dialogManager.closeDialog(true));
            await showNotEmpty(validationResult);
          },
          { text: localize('$CardTypes_RevokeAccessToken') }
        );
      };
    }

    const cancel = this.getControl<Button>(block, 'Cancel');
    if (cancel) {
      cancel.buttonAction = async () => {
        await this._dialogManager.closeDialog(false);
      };
    }
  }

  //#endregion
}
