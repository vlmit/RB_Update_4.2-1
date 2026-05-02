/** Типы шаблонов для проверки значения. */
export enum OcrPatternTypes {
  /** Булево значение. */
  Boolean,
  /** Целое число. */
  Integer,
  /** Вещественное число. */
  Double,
  /** Дата и время. */
  DateTime,
  /** Только дата. */
  Date,
  /** Только время. */
  Time,
  /** Временной интервал. */
  Interval
}

/** Состояния запросов на распознавание текста в файле. */
export enum OcrRequestStates {
  /** Создан. */
  Created,
  /** Активен. */
  Active,
  /** Выполнен. */
  Completed,
  /** Прерван. */
  Interrupted
}
