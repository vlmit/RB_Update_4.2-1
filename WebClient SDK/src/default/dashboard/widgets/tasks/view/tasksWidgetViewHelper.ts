import { CardFunctionRoles } from '@tessa/platform';

/**
 * Вспомогательные константы и методы для виджета {@link TasksWidget}.
 * @helper
 */
export namespace TasksWidgetViewHelper {
  export const ViewAlias = 'MyTasks';

  export const AuthorColumnName = 'Author';
  export const InfoColumnName = 'Info';
  export const PerformerColumnName = 'Performer';
  export const CompletionColumnName = 'Completion';

  export const AuthorIdColumnName = 'AuthorID';
  export const AuthorNameColumnName = 'AuthorName';
  export const RoleIdColumnName = 'RoleID';
  export const RoleNameColumnName = 'RoleName';
  export const RoleTypeIdColumnName = 'RoleTypeID';
  export const TypeCaptionColumnName = 'TypeCaption';
  export const CardTypeNameColumnName = 'CardTypeName';
  export const CardNameColumnName = 'CardName';
  export const CardSubjectColumnName = 'CardSubject';
  export const TaskInfoColumnName = 'TaskInfo';
  export const TimeToCompletionColumnName = 'TimeToCompletion';
  export const PlannedDateColumnName = 'PlannedDate';
  export const AppearanceColumnName = 'AppearanceColumn';

  export const StateParameterName = 'Status';
  export const AuthorParameterName = 'FunctionRoleAuthorParam';
  export const PerformerParameterName = 'FunctionRolePerformerParam';

  export const ByStateSubsetName = 'ByStatus';
  export const ByStateSubsetColumnIdName = 'StateID';
  export const ByStateSubsetColumnCountName = 'cnt';

  export const VisibleColumns: ReadonlyArray<string> = [
    AuthorColumnName,
    InfoColumnName,
    PerformerColumnName,
    CompletionColumnName
  ];

  export const DefaultParameters: ReadonlyMap<string, string> = new Map([
    [TasksWidgetViewHelper.AuthorParameterName, CardFunctionRoles.AuthorID],
    [TasksWidgetViewHelper.PerformerParameterName, CardFunctionRoles.PerformerID]
  ]);
}
