import { extension, inject, localize } from '@tessa/application';
import { ITokenService, ITokenService$ } from '@tessa/platform';
import { getTessaIcon } from 'common/utility/uiHelpers';
import { MenuAction, showLoadingOverlay, showNotEmpty } from 'tessa/ui';
import { IViewContextMenuContext, IWorkplaceViewComponent } from 'tessa/ui/views';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';

@extension({ name: 'AccessTokensContextMenuExtension' })
export class AccessTokensContextMenuExtension extends WorkplaceViewComponentExtension {
  // #region ctor

  constructor(@inject(ITokenService$) private readonly tokenService: ITokenService) {
    super();
  }

  // #endregion

  //#region WorkplaceViewComponentExtension

  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.AccessTokensContextMenuExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    model.contextMenuGenerators.push(ctx => {
      this.revokeTokenAction(ctx);
    });
  }

  //#endregion

  //#region Private Methods

  private revokeTokenAction(ctx: IViewContextMenuContext) {
    const viewContext = ctx.viewContext;

    const view = viewContext.view;
    const selectedObject = ctx.row.data;
    if (!selectedObject || !view) {
      return;
    }

    const tokenHash = selectedObject.get('Hash');
    if (!tokenHash) {
      return;
    }

    ctx.menuActions.push(
      new MenuAction(
        'RevokeToken',
        localize('$Views_Tokens_RevokeToken'),
        getTessaIcon('Thin118'),
        async () => {
          await showLoadingOverlay(
            async () => {
              const result = await this.tokenService.revokeToken(tokenHash);
              await showNotEmpty(result);
              if (result.isSuccessful) {
                viewContext.refreshView();
              }
            },
            {
              text: localize('$CardTypes_RevokeAccessToken')
            }
          );
        },
        null,
        false
      )
    );
  }

  //#endregion
}
