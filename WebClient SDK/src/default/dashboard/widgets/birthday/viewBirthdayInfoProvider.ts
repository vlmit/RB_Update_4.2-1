import { inject, injectable } from '@tessa/application';
import {
  IViewRepository,
  IViewRepository$,
  ViewCriteriaOperators,
  ViewRequest
} from '@tessa/platform';
import {
  IBirthdayInfo,
  IBirthdayInfoProvider,
  IBirthdayInfoProviderSettings
} from './birthdayTypes';

@injectable()
export class ViewBirthdayInfoProvider implements IBirthdayInfoProvider {
  //#region ctor

  constructor(@inject(IViewRepository$) private readonly _viewRepository: IViewRepository) {}

  //#endregion

  //#region IBirthdayInfoProvider

  async getInfo(settings: IBirthdayInfoProviderSettings): Promise<readonly IBirthdayInfo[]> {
    const view = await this._viewRepository.getByName('UsersBirthdays');
    if (!view) {
      return [];
    }

    const metadata = await view.getMetadata();
    const request = new ViewRequest(metadata);

    if (!!settings.departments && settings.departments.length > 0) {
      request.addParameter(builder => {
        builder.withMetadata(metadata.parameters.get('Departments')!);

        for (const departmentId of settings.departments!) {
          builder.addCriteria(ViewCriteriaOperators.EqualsTo, departmentId, departmentId);
        }

        return builder.asRequestParameter();
      });
    }

    if (settings.includeSubsidiaryDepartments) {
      request.addParameter(builder =>
        builder
          .withMetadata(metadata.parameters.get('IncludeSubsidiaryDepartments')!)
          .addCriteria(ViewCriteriaOperators.IsTrue)
          .asRequestParameter()
      );
    }

    if (settings.daysBefore) {
      request.addParameter(builder =>
        builder
          .withMetadata(metadata.parameters.get('DaysBefore')!)
          .addCriteria(
            ViewCriteriaOperators.EqualsTo,
            settings.daysBefore!.toString(),
            settings.daysBefore!
          )
          .asRequestParameter()
      );
    }

    if (settings.daysAfter) {
      request.addParameter(builder =>
        builder
          .withMetadata(metadata.parameters.get('DaysAfter')!)
          .addCriteria(
            ViewCriteriaOperators.EqualsTo,
            settings.daysAfter!.toString(),
            settings.daysAfter!
          )
          .asRequestParameter()
      );
    }

    const result = await view.getData(request);
    const rows = result.getRowsAsMap();

    return rows.map(row => ({
      userId: row.getString('UserID')!,
      birthday: row.getString('Birthday')!
    }));
  }

  //#endregion
}
