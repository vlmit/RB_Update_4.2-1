import { createInjectToken } from '@tessa/application';
import type { IKrWorkflowActionCompletionOptionsProvider } from './chunk/actions/types';

/** @category injects */
export const IKrWorkflowActionCompletionOptionsProvider$ =
  createInjectToken<IKrWorkflowActionCompletionOptionsProvider>(
    'IKrWorkflowActionCompletionOptionsProvider'
  );
