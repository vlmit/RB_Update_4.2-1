/**
 * Предоставляет информацию о варианте завершения действия.
 */
export class ActionCompletionOption {
  /**
   * Инициализирует новый экземпляр класса.
   * @param id Идентификатор варианта завершения действия.
   * @param caption Отображаемое название варианта завершения действия.
   */
  constructor(
    readonly id: string,
    readonly caption: string
  ) {}
}
