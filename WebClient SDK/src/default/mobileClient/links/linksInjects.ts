import { createInjectToken } from '@tessa/application';
import { ILinksProvider } from './linksTypes';

/** @category injects */
export const ILinksProvider$ = createInjectToken<ILinksProvider>('ILinksProvider');
