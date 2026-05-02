import { inject, injectable } from '@tessa/application';
import { IDbEnumerationProvider, IDbEnumerationProvider$ } from '@tessa/platform';
import { IKrWorkflowActionCompletionOptionsProvider } from './types';
import { ActionCompletionOption } from './actionCompletionOption';
import { DbKrWeActionCompletionOption } from '../../../../enumerations/dbKrWeActionCompletionOption';

@injectable()
export class KrWorkflowActionCompletionOptionsProvider implements IKrWorkflowActionCompletionOptionsProvider {
  //#region ctor

  constructor(
    @inject(IDbEnumerationProvider$) protected readonly _enumerationProvider: IDbEnumerationProvider
  ) {}

  //#endregion

  //#region IKrWorkflowActionCompletionOptionsProvider members

  getActionCompletionOptions(): ReadonlyMap<string, ActionCompletionOption> {
    const records = this._enumerationProvider.getAll(DbKrWeActionCompletionOption);

    if (!records) {
      return new Map<string, ActionCompletionOption>();
    }

    const result = new Map<string, ActionCompletionOption>();
    for (const record of records) {
      const co = new ActionCompletionOption(record.id, record.caption);

      result.set(co.id, co);
    }

    return result;
  }

  //#endregion
}
