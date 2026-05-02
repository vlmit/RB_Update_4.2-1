import { TagInfo } from '@tessa/platform';
import { NamedReference } from '../../common/namedReference';

/** Данные, хранящие информацию о теге и количестве связанных с ним записей. */
export interface ITagData {
  /** Информация о теге. */
  readonly tagInfo: TagInfo;
  /** Количество записей, связанных с тегом. */
  readonly recordsCount: number;
}

/** Менеджер для получения и отображения данных о теге. */
export interface ITagDataManager {
  /**
   * Получает данные о теге по его идентификатору.
   * @param tag Ссылка на тег.
   * @param types Ссылки на отображаемые типы, если заданы.
   * @returns Данные, хранящие информацию о теге или `null`, если тег не был найден.
   */
  getTagData(tag: NamedReference, types?: NamedReference[]): Promise<ITagData | null>;
  /**
   * Отображает записи, связанные с тегом.
   * @param tag Ссылка на тег.
   * @param types Ссылки на отображаемые типы, если заданы.
   */
  showTagData(tag: NamedReference, types?: NamedReference[]): Promise<void>;
}
