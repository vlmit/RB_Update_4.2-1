import { StorageHelper } from '@tessa/core';
import { extension } from '@tessa/application';
import { ClientCommandHandlerBase, IClientCommandHandlerContext } from '@tessa/platform';
import { showMessage } from 'tessa/ui';

@extension({ name: 'ShowConfirmationDialogClientCommandHandler' })
export class ShowConfirmationDialogClientCommandHandler extends ClientCommandHandlerBase {
  async handle(context: IClientCommandHandlerContext): Promise<void> {
    const text = StorageHelper.tryGet<string>(context.command.parameters, 'text');

    if (text) {
      await showMessage(text);
    }
  }
}
