import { localize } from '@tessa/application';
import {
  CardHelper,
  SchemeType,
  SortColumn,
  ViewColumnMetadata,
  ViewCriteriaOperators,
  ViewMetadata,
  ViewParameterMetadata
} from '@tessa/platform';

export class ApiAccessTokensViewMetadata extends ViewMetadata {
  //#region constructors

  constructor() {
    super();
    this.initialize();
  }

  //#endregion

  //#region protected methods

  protected initialize(): void {
    this.alias = CardHelper.systemKeyPrefix + 'ApiAccessTokens';
    this.caption = localize('$Views_Names_Tokens_ApiAccessTokens');
    this.quickSearchParam = 'QuickSearch';
    this.treatAsSingleQuery = true;
    this.enableAutoWidth = true;
    this.defaultSortColumns = [new SortColumn('Created', true)];

    // columns
    this.createIdColumnMetadata();
    this.createDescriptionColumnMetadata();
    this.createScopeColumnMetadata();
    this.createCreatedByIdColumnMetadata();
    this.createCreatedByNameColumnMetadata();
    this.createCreatedColumnMetadata();
    this.createExpiresColumnMetadata();
    this.createLastActivityColumnMetadata();
    this.createUserIdColumnMetadata();
    this.createUserNameColumnMetadata();
    this.createHashColumnMetadata();

    // parameters
    this.createQuickSearchParameterMetadata();
  }

  protected createIdColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'ID';
    column.caption = localize('$Views_Tokens_ID');
    column.schemeType = SchemeType.Guid;
    column.disableGrouping = true;
    this.columns.set(column.alias, column);
  }

  protected createDescriptionColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'Description';
    column.caption = localize('$Views_Tokens_Description');
    column.schemeType = SchemeType.String;
    this.columns.set(column.alias, column);
  }

  protected createScopeColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'Scope';
    column.caption = localize('$Views_Tokens_Scope');
    column.schemeType = SchemeType.String;
    this.columns.set(column.alias, column);
  }

  protected createCreatedByIdColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'CreatedByID';
    column.caption = localize('$Views_Tokens_CreatedByID');
    column.schemeType = SchemeType.Guid;
    column.invisibleByDefault = true;
    column.hidden = true;
    this.columns.set(column.alias, column);
  }

  protected createCreatedByNameColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'CreatedByName';
    column.caption = localize('$Views_Tokens_CreatedByName');
    column.schemeType = SchemeType.String;
    column.sortBy = column.alias;
    this.columns.set(column.alias, column);
  }

  protected createCreatedColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'Created';
    column.caption = localize('$Views_Tokens_Created');
    column.schemeType = SchemeType.DateTime;
    column.sortBy = column.alias;
    this.columns.set(column.alias, column);
  }

  protected createExpiresColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'Expires';
    column.caption = localize('$Views_Tokens_Expires');
    column.schemeType = SchemeType.DateTime;
    column.sortBy = column.alias;
    column.treatValueAsUtc = true;
    this.columns.set(column.alias, column);
  }

  protected createLastActivityColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'LastActivity';
    column.caption = localize('$Views_Tokens_LastActivity');
    column.schemeType = SchemeType.NullableDateTime;
    column.sortBy = column.alias;
    this.columns.set(column.alias, column);
  }

  protected createUserIdColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'UserID';
    column.caption = localize('$Views_Tokens_UserID');
    column.schemeType = SchemeType.Guid;
    column.invisibleByDefault = true;
    column.hidden = true;
    this.columns.set(column.alias, column);
  }

  protected createUserNameColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'UserName';
    column.caption = localize('$Views_Tokens_UserName');
    column.schemeType = SchemeType.String;
    column.sortBy = column.alias;
    this.columns.set(column.alias, column);
  }

  protected createHashColumnMetadata(): void {
    const column = new ViewColumnMetadata();
    column.alias = 'Hash';
    column.caption = localize('$Views_Tokens_Hash');
    column.schemeType = SchemeType.String;
    this.columns.set(column.alias, column);
  }

  protected createQuickSearchParameterMetadata(): void {
    const parameter = new ViewParameterMetadata();
    parameter.alias = 'QuickSearch';
    parameter.caption = localize('$Views_Tokens_QuickSearch');
    parameter.schemeType = SchemeType.String;
    parameter.multiple = false;
    parameter.ignoreCase = false;
    parameter.allowedOperands = [ViewCriteriaOperators.EqualsTo.name];
    this.parameters.set(parameter.alias, parameter);
  }

  //#endregion
}
