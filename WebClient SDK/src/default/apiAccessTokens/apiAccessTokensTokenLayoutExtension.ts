import { extension } from '@tessa/application';
import { TextFieldViewModel } from 'ui/textField/textFieldViewModel';
import { UIButton } from 'tessa/ui/uiButton';
import { LabelViewModel } from 'tessa/ui/cards/controls/labelViewModel';
import { IDataProvider } from 'tessa/ui/formEditor/data/definitions';
import { IFormExtensionContext } from 'tessa/ui/formEditor/extensions/formExtension';
import { ApiAccessTokensBaseFormExtension } from './apiAccessTokensBaseFormExtension';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';

@extension({ name: 'ApiAccessTokensTokenLayoutExtension' })
export class ApiAccessTokensTokenLayoutExtension extends ApiAccessTokensBaseFormExtension {
  //#region base overrides

  override async initialized(context: IFormExtensionContext): Promise<void> {
    const value = ApiAccessTokensHelper.getToken(context.runtimeView?.info);
    if (!value) {
      return;
    }

    const block = context.runtimeView?.getRootBlock();
    const data = context.runtimeView?.rootDataProvider;
    if (!data || !block || !context.validationResult.isSuccessful) {
      return;
    }

    const tokenHint = this.getControl<LabelViewModel>(block, 'TokenHint');
    if (tokenHint) {
      tokenHint.type = 'message';
      tokenHint.theme = 'warning';
    }

    const token = this.getControl<TextFieldViewModel>(block, 'Token');
    if (token) {
      const mask = '****************';
      data.rawSetValue('Token', mask);
      token.toolbar.buttons.availability = 'enabled';
      token.toolbar.buttons.addRange(
        this.createCopyButton(value),
        this.createVisibilityButton(data, value, mask)
      );
    }
  }

  //#endregion

  //#region private methods

  private createVisibilityButton(data: IDataProvider, accessToken: string, mask: string): UIButton {
    return UIButton.create({
      name: 'Visibility',
      icon: 'm-eye',
      type: 'small',
      theme: 'control',
      buttonAction: button => {
        const isActive = (button.isActive = !button.isActive);
        button.theme = isActive ? 'primary' : 'control';
        data.rawSetValue('Token', isActive ? accessToken : mask);
      }
    });
  }

  //#endregion
}
