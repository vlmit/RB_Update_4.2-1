/**
 * Настройки, связанные с сессиями, которые не должны изменяться в проектных решениях.
 */
// Дубликат - src\Libraries\Tessa\Platform\Runtime\SessionInternalSettings.cs
export enum SessionInternalSettings {
  /**
   * Время в часах.
   *
   * Временной интервал, за который надо выполнить первую попытку переоткрытия сессии до того, как сессия истечёт.
   *
   * Например, если значение равно "1 час", то переоткрытие сессии первый раз произойдёт за час до истечения сессии.
   */
  ReopenBeforeExpirationTimeSpan = 1,
  /**
   * Время в минутах.
   *
   * Временной интервал, по истечении которого требуется переоткрыть сессию,
   * если сессия была неудачно переоткрыта в предыдущий раз.
   */
  ReopenAfterFailedTimeSpan = 5
}

export enum SessionExpireDialogResultVariant {
  Ok,
  Cancel,
  CancelNoNotify
}
