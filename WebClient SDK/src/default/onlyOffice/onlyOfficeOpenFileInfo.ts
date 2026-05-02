/**
 * Представляет собой информацию об открытом файле в редакторе.
 */
import { IFileVersion } from 'tessa/files';

export type OnlyOfficeOpenFileInfo = {
  /**
   * Исходная версия файла.
   */
  readonly version: IFileVersion;

  /**
   * Идентификатор записи. Уникален на каждое отдельное открытие файла.
   */
  readonly cacheId: string;

  /**
   * Функция, позволяющая незамедлительно закрыть редактор.
   */
  readonly forceCloseCallback: () => void;

  /**
   * Идентификатор карточки. Может быть null. Может служить дополнительным признаком для отслеживания открытых файлов в карточке.
   */
  readonly cardId: string | null;

  /**
   * Признак того, что файл был открыт на редактирование.
   */
  readonly forEdit: boolean;
};
