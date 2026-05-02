import { ActionCompletionOption } from './actionCompletionOption';

/**
 * Объект, предоставляющий доступ к вариантам завершения действий.
 */
export interface IKrWorkflowActionCompletionOptionsProvider {
  /**
   * Возвращает доступный только для чтения словарь с вариантами завершения действий.
   * @returns Доступный только для чтения словарь с вариантами завершения действий.
   */
  getActionCompletionOptions(): ReadonlyMap<string, ActionCompletionOption>;
}
