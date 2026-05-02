import { IStorage } from 'tessa/platform/storage';
import { Paging } from '@tessa/platform';

export class FileControlCreationParams {
  categoriesViewAlias = 'FileCategoriesFiltered';

  previewControlName = '';

  isCategoriesEnabled = false;

  isManualCategoriesCreationDisabled = false;

  isNullCategoryCreationDisabled = false;

  isIgnoreExistingCategories = false;

  categoriesViewMapping: Array<IStorage> = [];

  pagingMode: Paging = Paging.No;

  pageLimit = 20;
}
