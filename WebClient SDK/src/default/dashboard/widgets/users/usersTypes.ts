/** Информация о пользователе. */
export interface IUserInfo {
  /** Уникальный идентификатор пользователя. */
  readonly userId: string;
}

/** Результат запроса информации о пользователях. */
export interface IUserInfoProviderResult {
  /** Коллекция с информацией о найденных пользователях. */
  readonly users: ReadonlyArray<IUserInfo>;
  /** Признак наличия элементов, не включенных в результат. */
  readonly overflow: boolean;
}

/** Настройки для получения информации о пользователях. */
export interface IUserInfoProviderSettings {
  /** Фильтр для поиска пользователей. */
  readonly filter?: string | null;
  /** Номер запроса данных в рамках того же фильтра. */
  readonly index?: number | null;
}

/** Предоставляет информацию о пользователях. */
export interface IUserInfoProvider {
  /**
   * Получает информацию о пользователях на основе настроек.
   * @param settings Настройки для получения информации о пользователях.
   * @returns Результат запроса информации о пользователях.
   */
  getInfo(settings: IUserInfoProviderSettings): Promise<IUserInfoProviderResult>;
}
