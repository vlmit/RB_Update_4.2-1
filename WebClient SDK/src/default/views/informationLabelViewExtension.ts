import { extension, localize } from '@tessa/application';
import { StorageHelper } from '@tessa/core';
import { IViewComponentBase, IWorkplaceViewComponent, ViewComponentBase } from 'tessa/ui/views';
import { WorkplaceViewComponentExtension } from 'tessa/ui/views/extensions';
import { ViewInformationLabelViewModel } from './viewInformationLabel/viewInformationLabelViewModel';
import { ContentPlaceArea, ContentPlaceOrder } from 'tessa/ui/views/content';
import { ViewRequestParameter } from '@tessa/platform';

@extension({ name: InformationLabelViewExtension.Name })
export class InformationLabelViewExtension extends WorkplaceViewComponentExtension {
  //#region Fields

  public static readonly Name = 'InformationLabelViewExtension';

  //#endregion

  //#region Base Overrides

  public getExtensionName(): string {
    return 'Tessa.Extensions.Default.Client.Views.InformationLabelViewExtension';
  }

  public initialize(model: IWorkplaceViewComponent): void {
    const labelText = StorageHelper.tryGet<string>(this.settingsStorage, 'LabelText');
    if (!labelText) {
      return;
    }

    const requiredParamsInfo = this.getRequiredParams();
    if (requiredParamsInfo === null) {
      return;
    }
    const [allParamsRequired, requiredParams] = requiredParamsInfo;
    if (!requiredParams) {
      return;
    }

    model.contentFactories.set(InformationLabelViewExtension.Name, viewComponent => {
      const viewModel = new ViewInformationLabelViewModel(
        viewComponent,
        ContentPlaceArea.HeaderContentPanel,
        ContentPlaceOrder.AfterAll
      );
      const parameters = viewComponent.parameters.parameters;
      viewModel.label.visibility = false;
      viewModel.label.text = localize(labelText);
      viewModel.label.type = 'message';
      viewModel.label.theme = 'warning';

      const refreshingDisposer = viewComponent.onRefreshing.addWithDispose(() => {
        viewModel.label.visibility = false;
      });
      const refreshedDisposer = viewComponent.onRefreshed.addWithDispose(() => {
        const masterView =
          viewComponent instanceof ViewComponentBase
            ? (viewComponent.masterView as IViewComponentBase | null)
            : null;
        viewModel.label.visibility =
          masterView?.isDataLoading ||
          InformationLabelViewExtension.isParametersGiven(
            parameters,
            requiredParams,
            allParamsRequired
          )
            ? false
            : true;
      });

      if (refreshingDisposer) {
        this.disposeList.add(refreshingDisposer);
      }

      if (refreshedDisposer) {
        this.disposeList.add(refreshedDisposer);
      }

      return viewModel;
    });
  }

  //#endregion

  //#region Private Methods

  private getRequiredParams(): [boolean, string[]] | null {
    const allRequired = StorageHelper.tryGet<boolean>(this.settingsStorage, 'AllParamsRequired');
    if (allRequired == null) {
      return null;
    }

    const requiredParams = StorageHelper.tryGet<string>(this.settingsStorage, 'RequiredParams');
    if (!requiredParams) {
      return null;
    }

    return [allRequired, requiredParams?.split(/[ ,;]/)];
  }

  private static isParametersGiven(
    parameters: readonly ViewRequestParameter[],
    requiredNames: string[],
    allRequired: boolean
  ): boolean {
    if (parameters.length === 0 && requiredNames.length === 0) {
      return true;
    }
    if (allRequired) {
      return requiredNames.every(name => parameters.find(p => p.name === name));
    } else {
      return parameters.some(p => requiredNames.includes(p.name));
    }
  }

  //#endregion
}
