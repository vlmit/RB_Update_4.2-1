import { IStorage, ValidationResultBuilder } from '@tessa/core';
import {
  ApiAccessTokenDataBase,
  ApiAccessTokenViewRequest,
  IApiAccessTokenService,
  SchemeType,
  ViewRequestParameter
} from '@tessa/platform';
import {
  IViewControlDataProvider,
  ViewControlDataProviderRequest,
  ViewControlDataProviderResponse
} from 'tessa/ui/cards/controls/viewControl/viewControlDataProvider';
import { ApiAccessTokensViewSorter } from './apiAccessTokensViewSorter';

export class ApiAccessTokensViewDataProvider implements IViewControlDataProvider {
  //#region constructors

  constructor(private readonly _apiAccessTokenService: IApiAccessTokenService) {}

  //#endregion

  //#region IViewControlDataProvider

  async getDataAsync(
    request: ViewControlDataProviderRequest
  ): Promise<ViewControlDataProviderResponse> {
    const result = new ViewControlDataProviderResponse(new ValidationResultBuilder());
    this.initializeColumns(result);
    await this.initializeRows(request, result);
    return result;
  }

  //#endregion

  //#region private methods

  private initializeColumns(result: ViewControlDataProviderResponse): void {
    result.columns.push(['ID', SchemeType.Guid]);
    result.columns.push(['Description', SchemeType.String]);
    result.columns.push(['Scope', SchemeType.String]);
    result.columns.push(['CreatedByID', SchemeType.Guid]);
    result.columns.push(['CreatedByName', SchemeType.String]);
    result.columns.push(['Created', SchemeType.DateTime]);
    result.columns.push(['Expires', SchemeType.DateTime]);
    result.columns.push(['LastActivity', SchemeType.NullableDateTime]);
    result.columns.push(['UserID', SchemeType.Guid]);
    result.columns.push(['UserName', SchemeType.String]);
    result.columns.push(['Hash', SchemeType.String]);
  }

  private async initializeRows(
    request: ViewControlDataProviderRequest,
    result: ViewControlDataProviderResponse
  ): Promise<void> {
    const viewRequest = new ApiAccessTokenViewRequest();
    this.initializeViewParameters(request, viewRequest);

    const viewResponse = await this._apiAccessTokenService.view(viewRequest);
    const viewResult = viewResponse.validationResult.build();
    result.validationResult.add(viewResult);

    if (viewResult.isSuccessful && viewResponse.tokenData) {
      for (const tokenData of viewResponse.tokenData) {
        result.rows.push(this.createRowData(tokenData));
      }

      this.initializeSortingColumns(request, result);

      result.calculatedRowCount = result.rows.length;
    }
  }

  private initializeViewParameters(
    request: ViewControlDataProviderRequest,
    viewRequest: ApiAccessTokenViewRequest
  ): void {
    const parameters: ViewRequestParameter[] = [];
    for (const action of request.parametersActions) {
      action(parameters);
    }

    for (const parameter of parameters) {
      if (parameter.name === 'QuickSearch') {
        for (const requestCriteria of parameter.criteriaValues) {
          for (const criteriaValue of requestCriteria.values) {
            if (typeof criteriaValue.value === 'string') {
              viewRequest.filter = criteriaValue.value;
            }
          }
        }
      }
    }
  }

  private initializeSortingColumns(
    request: ViewControlDataProviderRequest,
    result: ViewControlDataProviderResponse
  ): void {
    const sorter = new ApiAccessTokensViewSorter(request);
    sorter.initialize();
    result.rows.sort(sorter.sort);
  }

  private createRowData(tokenData: ApiAccessTokenDataBase): IStorage {
    return {
      ['ID']: tokenData.id,
      ['Description']: tokenData.description,
      ['Scope']: tokenData.scope,
      ['CreatedByID']: tokenData.createdById,
      ['CreatedByName']: tokenData.createdByName,
      ['Created']: tokenData.created,
      ['Expires']: tokenData.expires,
      ['LastActivity']: tokenData.lastActivity,
      ['UserID']: tokenData.userId,
      ['UserName']: tokenData.userName,
      ['Hash']: tokenData.hash
    };
  }

  //#endregion
}
