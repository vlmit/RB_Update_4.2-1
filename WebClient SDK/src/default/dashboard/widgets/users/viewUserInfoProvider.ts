import { inject, injectable } from '@tessa/application';
import {
  IViewRepository,
  IViewRepository$,
  IViewSpecialParameters,
  IViewSpecialParameters$,
  Paging,
  ViewRequest,
  ViewCriteriaOperators
} from '@tessa/platform';
import {
  IUserInfoProvider,
  IUserInfoProviderResult,
  IUserInfoProviderSettings
} from './usersTypes';
import { UsersWidget } from './usersWidget';

/** Предоставляет информацию о пользователях из представления. */
@injectable()
export class ViewUserInfoProvider implements IUserInfoProvider {
  //#region ctor

  /**
   * Создаёт экземпляр класса {@link ViewUserInfoProvider}.
   * @param _viewRepository Предоставляет доступ к представлениям доступным в системе.
   * @param _specialParameters Предоставляет методы внедрения специальных параметров представлений в список параметров.
   */
  constructor(
    @inject(IViewRepository$) private readonly _viewRepository: IViewRepository,
    @inject(IViewSpecialParameters$) private readonly _specialParameters: IViewSpecialParameters
  ) {}

  //#endregion

  //#endregion

  //#region IUserInfoProvider

  async getInfo(settings: IUserInfoProviderSettings): Promise<IUserInfoProviderResult> {
    const filter = settings.filter ?? '';
    if (filter.length < UsersWidget.minFilterLength) {
      return { users: [], overflow: false };
    }

    const view = await this._viewRepository.getByName('UsersDictionary');
    if (!view) {
      return { users: [], overflow: false };
    }

    const metadata = await view.getMetadata();
    const request = new ViewRequest(metadata);
    request.addParameter(builder =>
      builder
        .withMetadata(metadata.parameters.get('QuickSearch')!)
        .addCriteria(ViewCriteriaOperators.Contains, filter, filter)
        .asRequestParameter()
    );

    const pageIndex = settings.index ?? 0;
    if (pageIndex > 0 && metadata.paging !== Paging.No) {
      this._specialParameters.providePageOffsetParameter(
        request.parameters,
        metadata.paging,
        pageIndex,
        metadata.pageLimit,
        metadata.paging === Paging.Optional
      );
      this._specialParameters.providePageLimitParameter(
        request.parameters,
        metadata.paging,
        metadata.pageLimit + 1,
        metadata.paging === Paging.Optional
      );
    }

    const result = await view.getData(request);
    const rows = result.getRowsAsMap();

    const overflow = rows.length > metadata.pageLimit;
    overflow && rows.splice(-1, 1);

    return { users: rows.map(row => ({ userId: row.getString('UserID')! })), overflow };
  }

  //#endregion
}
