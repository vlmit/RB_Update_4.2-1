import { extension } from '@tessa/application';
import { tryGetFromSettings } from 'tessa/ui';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ViewControlViewModel } from 'tessa/ui/cards/controls';
import { KrToken } from 'tessa/workflow';
import {
  CardHelper,
  ICardTypeExtensionContext,
  CardTypeExtensionTypes,
  ViewRequestParameterBuilder,
  ViewCriteriaOperators,
  CardTypeExtensionSettings
} from '@tessa/platform';
import { StringHelper, TypedJsonConverter } from '@tessa/core';

/**
 * Устанавливает в параметре {@link CardTypeExtensionSettings.TokenParameterAlias} представления, указанного по ключу {@link CardTypeExtensionSettings.ViewControlAlias},
 * информацию о токене безопасности {@link KrToken}, выданного для карточки.
 */
@extension({ name: 'KrTokenToTaskHistoryViewUIExtension' })
export class KrTokenToTaskHistoryViewUIExtension extends CardUIExtension {
  //#region base overrides

  public async initializing(context: ICardUIExtensionContext): Promise<void> {
    const result = await CardHelper.executeTypeExtensions(
      CardTypeExtensionTypes.MakeViewTaskHistory,
      context.card,
      context.model.generalMetadata,
      KrTokenToTaskHistoryViewUIExtension.executeInitializingAction,
      context
    );

    context.validationResult.add(result);
  }

  //#endregion

  //#region private methods

  private static async executeInitializingAction(
    typeContext: ICardTypeExtensionContext
  ): Promise<void> {
    const context = typeContext.externalContext as ICardUIExtensionContext;
    if (!context) {
      return;
    }

    const settings = typeContext.settings;
    const viewControlAlias = tryGetFromSettings<string>(
      settings,
      CardTypeExtensionSettings.ViewControlAlias
    );

    if (!viewControlAlias) {
      return;
    }

    context.model.controlInitializers.push(async control => {
      const viewControl = control as ViewControlViewModel;

      if (!viewControl) {
        return;
      }

      if (viewControl.name !== viewControlAlias) {
        return;
      }

      const token = KrToken.tryGet(context.card.info);
      if (!token) {
        return;
      }

      const tokenParameterAlias = tryGetFromSettings<string>(
        settings,
        CardTypeExtensionSettings.TokenParameterAlias
      );
      if (StringHelper.isNullOrWhiteSpace(tokenParameterAlias)) {
        return;
      }

      const tokenMetadata = viewControl.viewMetadata?.parameters.tryGet(tokenParameterAlias);
      if (!tokenMetadata) {
        return;
      }

      const tokenString = TypedJsonConverter.serialize(token.getStorage());
      const tokenParameter = new ViewRequestParameterBuilder()
        .withMetadata(tokenMetadata)
        .addCriteria(ViewCriteriaOperators.EqualsTo, tokenString, tokenString)
        .asRequestParameter();
      viewControl.parameters.addParameters(tokenParameter);
    });
  }

  //#endregion
}
