import { reaction } from 'mobx';
import { extension, inject, localize } from '@tessa/application';
import {
  IViewRepository$,
  IViewRepository,
  IApiAccessTokenService,
  IApiAccessTokenService$,
  ViewCriteriaOperators,
  ViewRequestParameterBuilder
} from '@tessa/platform';
import { ViewService } from 'tessa/views/viewService';
import { showViewsDialog } from 'tessa/ui/uiHost';
import { IUIContext, UIContext } from 'tessa/ui/uiContext';
import { IFormDialogManager } from 'tessa/ui/formEditor/types';
import { IFormDialogManager$ } from 'tessa/ui/formEditor/injects';
import { IFormExtensionContext } from 'tessa/ui/formEditor/extensions/formExtension';
import { RuntimeItemWithStateViewModel } from 'tessa/ui/formEditor/controls/runtimeItemWithStateViewModel';
import { ViewButtonViewModel } from 'tessa/ui/views/content/viewButtonViewModel';
import { ViewControlViewModel } from 'tessa/ui/cards/controls/viewControl/viewControlViewModel';
import { ViewControlToolbarItem } from 'tessa/ui/cards/controls/viewControl/contents/viewControlToolbarItem';
import { CustomViewInitializationStrategy } from 'tessa/ui/cards/controls/viewControl/customViewInitializationStrategy';
import { ScopeContextInstance } from 'tessa/platform/scopes/scopeContext';
import { ApiAccessTokensBaseFormExtension } from './apiAccessTokensBaseFormExtension';
import { ApiAccessTokensViewDataProvider } from './apiAccessTokensViewDataProvider';
import { ApiAccessTokensViewMetadata } from './apiAccessTokensViewMetadata';
import { ApiAccessTokensHelper } from './apiAccessTokensHelper';

@extension({ name: 'ApiAccessTokensViewLayoutExtension' })
export class ApiAccessTokensViewLayoutExtension extends ApiAccessTokensBaseFormExtension {
  //#region constructors

  constructor(
    @inject(IViewRepository$) private readonly _viewRepository: IViewRepository,
    @inject(IFormDialogManager$) private readonly _dialogManager: IFormDialogManager,
    @inject(IApiAccessTokenService$) private readonly _apiAccessTokenService: IApiAccessTokenService
  ) {
    super();
  }

  //#endregion

  //#region base overrides

  override async initializing(context: IFormExtensionContext): Promise<void> {
    context.controlInitializers?.set('ApiAccessTokens', async runtimeItem => {
      const runtimeItemWithState = runtimeItem as RuntimeItemWithStateViewModel;
      const viewControl = runtimeItemWithState.getCurrent<ViewControlViewModel>();
      if (viewControl) {
        const [create, revoke, history] = this.initializeViewButtons(viewControl);
        this.initializeViewStrategy(viewControl);
        this.initializeViewToolbar(viewControl, create);
        this.initializeViewFooter(viewControl, revoke, history);
        this.initializeViewContextMenu(viewControl, revoke, history);
        this.initializeViewDoubleClick(viewControl);
        this.initializeViewFilter(viewControl);
      }
    });
  }

  //#endregion

  //#region private methods

  private initializeViewStrategy(viewControl: ViewControlViewModel): void {
    const options = {
      viewMetadata: new ApiAccessTokensViewMetadata(),
      dataProvider: new ApiAccessTokensViewDataProvider(this._apiAccessTokenService)
    };
    const strategy = new CustomViewInitializationStrategy(options, ViewService.instance);
    viewControl.initializeStrategy(strategy, true);
  }

  private initializeViewButtons(
    viewControl: ViewControlViewModel
  ): ViewButtonViewModel<ViewControlViewModel>[] {
    const createButton = this.createViewCreateButton(viewControl);
    const revokeButton = this.createViewRevokeButton(viewControl);
    const historyButton = this.createViewHistoryButton(viewControl);

    this.disposeList.add(
      reaction(
        () => !!viewControl.selectedRow,
        isSelected => (revokeButton.isEnabled = historyButton.isEnabled = isSelected),
        { fireImmediately: true }
      )
    );

    return [createButton, revokeButton, historyButton];
  }

  private initializeViewToolbar(
    viewControl: ViewControlViewModel,
    ...viewButtons: ViewButtonViewModel<ViewControlViewModel>[]
  ): void {
    for (const viewButton of viewButtons) {
      viewControl.topItems.push(new ViewControlToolbarItem(viewButton, 'right'));
    }
  }

  private initializeViewFooter(
    viewControl: ViewControlViewModel,
    ...viewButtons: ViewButtonViewModel<ViewControlViewModel>[]
  ): void {
    for (const viewButton of viewButtons) {
      viewControl.bottomItems.push(new ViewControlToolbarItem(viewButton, 'left'));
    }
  }

  private initializeViewContextMenu(
    viewControl: ViewControlViewModel,
    ...viewButtons: ViewButtonViewModel<ViewControlViewModel>[]
  ): void {
    const viewActions = viewButtons.map(button => button.toMenuAction());
    viewControl.table?.rowContextMenuGenerators.push(ctx => ctx.menuActions.push(...viewActions));
    viewControl.selectRowOnContextMenu = true;
  }

  private initializeViewDoubleClick(viewControl: ViewControlViewModel): void {
    viewControl.doubleClickAction = async () => {
      await ApiAccessTokensHelper.openLayoutDialog(
        this._dialogManager,
        ApiAccessTokensHelper.ApiAccessTokensInfoLayout,
        undefined,
        { hash: viewControl.selectedRow?.get('Hash') }
      );
    };
  }

  private initializeViewFilter(viewControl: ViewControlViewModel): void {
    const disposer = viewControl.filterTextViewModel?.parameters.onParameterAdded.add(e => {
      for (const parameter of e.parameters) {
        for (const requestCriteria of parameter.criteriaValues) {
          for (const criteriaValue of requestCriteria.values) {
            if (typeof criteriaValue.value === 'string') {
              criteriaValue.value = criteriaValue.value
                .replaceAll('\u200B', '')
                .replaceAll('\u00A0', '');
            }
          }
        }
      }
    });

    disposer && this.disposeList.add(disposer);
  }

  private createViewButton(
    viewControl: ViewControlViewModel
  ): ViewButtonViewModel<ViewControlViewModel> {
    const button = new ViewButtonViewModel(viewControl);
    button.captionPosition = 'after';
    button.theme = 'control';
    button.type = 'small';
    return button;
  }

  private createViewCreateButton(
    viewControl: ViewControlViewModel
  ): ViewButtonViewModel<ViewControlViewModel> {
    const button = this.createViewButton(viewControl);
    button.name = 'CreateToken';
    button.icon = 'm-plus';
    button.caption = localize('$CardTypes_Buttons_CreateToken');
    button.tooltip = localize('$CardTypes_Buttons_CreateToken_Tooltip');
    button.onClick = async () => {
      const created = await ApiAccessTokensHelper.openLayoutDialog<boolean>(
        this._dialogManager,
        ApiAccessTokensHelper.ApiAccessTokensInfoLayout
      );

      created && (await viewControl.refresh());
    };

    return button;
  }

  private createViewRevokeButton(
    viewControl: ViewControlViewModel
  ): ViewButtonViewModel<ViewControlViewModel> {
    const button = this.createViewButton(viewControl);
    button.name = 'RevokeToken';
    button.icon = 'm-reply';
    button.caption = localize('$CardTypes_Buttons_RevokeToken');
    button.tooltip = localize('$CardTypes_Buttons_RevokeToken_Tooltip');
    button.onClick = async () => {
      const revoked = await ApiAccessTokensHelper.openLayoutDialog<boolean>(
        this._dialogManager,
        ApiAccessTokensHelper.ApiAccessTokensRevokeLayout,
        undefined,
        { hash: viewControl.selectedRow?.get('Hash') }
      );

      revoked && (await viewControl.refresh());
    };

    return button;
  }

  private createViewHistoryButton(
    viewControl: ViewControlViewModel
  ): ViewButtonViewModel<ViewControlViewModel> {
    const button = this.createViewButton(viewControl);
    button.name = 'HistoryToken';
    button.icon = 'm-doc';
    button.caption = localize('$CardTypes_Buttons_TokenHistory');
    button.tooltip = localize('$CardTypes_Buttons_TokenHistory_Tooltip');
    button.onClick = async () => {
      const id = viewControl.selectedRow?.get('ID');
      const description = viewControl.selectedRow?.get('Description');

      const view = await this._viewRepository.getByName('ActionHistory');
      if (!view) {
        throw new Error(`Can not find view with alias 'ActionHistory' at metadata.`);
      }
      const viewParameterMetadata = (await view.getMetadata()).parameters.get('SessionID');
      if (!viewParameterMetadata) {
        throw new Error(`Can not find view parameter 'SessionID' at metadata.`);
      }
      const viewParameter = new ViewRequestParameterBuilder()
        .withMetadata(viewParameterMetadata)
        .addCriteria(ViewCriteriaOperators.EqualsTo, id, id)
        .readOnly(true)
        .asRequestParameter();

      let uiContextScope: ScopeContextInstance<IUIContext> | null = null;
      try {
        uiContextScope = UIContext.create(UIContext.current);
        ApiAccessTokensHelper.setInDialogScope(uiContextScope.context?.info);

        await showViewsDialog(
          'ActionHistory',
          () => Promise.resolve(),
          [viewParameter],
          undefined,
          {
            isAppPanelHidden: true,
            isTreeViewHidden: true,
            isSelectionMode: false,
            title: description
          }
        );
      } finally {
        uiContextScope?.dispose();
      }
    };

    return button;
  }

  //#endregion
}
