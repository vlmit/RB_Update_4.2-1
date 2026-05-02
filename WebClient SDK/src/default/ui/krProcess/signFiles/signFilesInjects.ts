import { createInjectToken } from '@tessa/application';
import { ISignFilesProvider } from './signFilesTypes';

/** @category injects */
export const ISignFilesProvider$ = createInjectToken<ISignFilesProvider>('ISignFilesProvider');
