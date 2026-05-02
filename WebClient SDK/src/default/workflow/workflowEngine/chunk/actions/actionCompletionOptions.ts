/**
 * Варианты завершения действий.
 * @remarks В базе данных соответствующая информация хранится в таблице `KrWeActionCompletionOptions`.
 * @helper
 */
export namespace ActionCompletionOptions {
  /**
   * Согласовано.
   */
  export const approved = '4339a03f-234d-4a9a-a6e4-58a88a5a03ce';

  /**
   * Не согласовано.
   */
  export const disapproved = '6fbdd34b-be9a-40bf-90cb-1640d4abb9f5';

  /**
   * Подписано.
   */
  export const signed = 'fa94b7bf-7b99-46d6-9c65-b21a483ebc45';

  /**
   * Отказано.
   */
  export const declined = '4a1936c7-1f94-4897-9dae-934163e2fe1c';
}
