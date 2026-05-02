import { createInjectToken } from '@tessa/application';
import { ITagDataManager } from './tag/tagTypes';
import { IUserInfoProvider } from './users/usersTypes';
import { IBirthdayInfoProvider } from './birthday/birthdayTypes';
import { IViewWidgetClipboardFilter, IViewWidgetDashboardClipboard } from './view/viewWidgetTypes';
import { ICardTypeSelectorTypesProvider } from './createCardByType/cardTypeSelector/cardTypeSelectorTypes';
import { ITasksWidgetViewParameterManager } from './tasks/tasksTypes';

/** @category injects */
export const ITagDataManager$ = createInjectToken<ITagDataManager>('ITagDataManager');

/** @category injects */
export const IBirthdayInfoProvider$ =
  createInjectToken<IBirthdayInfoProvider>('IBirthdayInfoProvider');

/** @category injects */
export const IUserInfoProvider$ = createInjectToken<IUserInfoProvider>('IUserInfoProvider');

/** @category injects */
export const IViewWidgetDashboardClipboard$ = createInjectToken<IViewWidgetDashboardClipboard>(
  'IViewWidgetDashboardClipboard'
);

/** @category injects */
export const IViewWidgetClipboardFilter$ = createInjectToken<IViewWidgetClipboardFilter>(
  'IViewWidgetClipboardFilter'
);

/** @category injects */
export const ICardTypeSelectorTypesProvider$ = createInjectToken<ICardTypeSelectorTypesProvider>(
  'ICardTypeSelectorTypesProvider'
);

/** @category injects */
export const ITasksWidgetViewParameterManager$ =
  createInjectToken<ITasksWidgetViewParameterManager>('ITasksWidgetViewParameterManager');

/** @category injects */
export const NotesRichTextBoxDependenciesFactoryName = 'NotesRichTextBoxDependenciesFactory';
