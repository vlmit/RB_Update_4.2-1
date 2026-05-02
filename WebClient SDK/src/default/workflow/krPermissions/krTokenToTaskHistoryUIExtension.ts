import { extension, inject } from '@tessa/application';
import { Flags, TypedJsonConverter } from '@tessa/core';
import {
  IKrTypesCache,
  IKrTypesCache$,
  KrComponents,
  KrComponentsHelper,
  KrToken,
  ViewCriteriaOperators,
  ViewRequestParameterBuilder,
  IViewRepository,
  IViewRepository$
} from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

/**
 * Устанавливает в параметре <c>Token</c> представления <c>TaskHistory</c> информацию о токене безопасности {@link KrToken}, выданного для карточки.
 */
@extension({ name: 'KrTokenToTaskHistoryUIExtension' })
export class KrTokenToTaskHistoryUIExtension extends CardUIExtension {
  //#region ctor

  constructor(
    @inject(IKrTypesCache$) private readonly _krTypesCache: IKrTypesCache,
    @inject(IViewRepository$) private readonly _viewRepository: IViewRepository
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  public async initialized(context: ICardUIExtensionContext): Promise<void> {
    if (context.model.inSpecialMode) {
      return;
    }

    const taskHistory = context.model.tryGetTaskHistory();
    if (
      !taskHistory ||
      Flags.hasNotFlag(
        await KrComponentsHelper.getKrComponentsByCard(context.card, this._krTypesCache),
        KrComponents.Base
      )
    ) {
      return;
    }

    const card = context.model.card;

    taskHistory.modifyOpenViewRequestAction = async args => {
      const token = KrToken.tryGet(card.info);
      if (!token) {
        return args;
      }

      const view = await this._viewRepository.getByName(args.viewAlias);

      if (!view) {
        return args;
      }

      const viewMetadata = await view.getMetadata();

      const tokenMetadata = viewMetadata.parameters.tryGet('Token');
      if (!tokenMetadata) {
        return args;
      }

      const tokenString = TypedJsonConverter.serialize(token.getStorage());
      const tokenParameter = new ViewRequestParameterBuilder()
        .withMetadata(tokenMetadata)
        .addCriteria(ViewCriteriaOperators.EqualsTo, tokenString, tokenString)
        .asRequestParameter();

      args.parameters.push(tokenParameter);

      return args;
    };
  }

  //#endregion
}
