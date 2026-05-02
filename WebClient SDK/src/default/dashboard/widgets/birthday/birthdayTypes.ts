import { IUserInfo } from '@tessa/platform';

export interface IBirthdayInfo {
  userId: string;
  birthday: string;
}

export interface IUserBirthdayInfo {
  user: IUserInfo;
  birthday: string;
}

export interface IBirthdayGroupDataSource {
  readonly birthdays: ReadonlyArray<IUserBirthdayInfo>;
}

export interface IBirthdayGroup {
  readonly name: string;
  title: string;
  expanded: boolean;
  birthdays: readonly IUserBirthdayInfo[];
}

export interface IBirthdayInfoProviderSettings {
  readonly departments?: readonly string[] | null;
  readonly includeSubsidiaryDepartments?: boolean | null;
  readonly daysAfter?: number | null;
  readonly daysBefore?: number | null;
}

export interface IBirthdayInfoProvider {
  getInfo(settings: IBirthdayInfoProviderSettings): Promise<readonly IBirthdayInfo[]>;
}
