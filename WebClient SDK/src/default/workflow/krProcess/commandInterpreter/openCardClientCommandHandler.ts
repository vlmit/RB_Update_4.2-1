import { StorageHelper } from '@tessa/core';
import { extension } from '@tessa/application';
import { ClientCommandHandlerBase, IClientCommandHandlerContext } from '@tessa/platform';
import { openCard } from 'tessa/ui/uiHost';

@extension({ name: 'OpenCardClientCommandHandler' })
export class OpenCardClientCommandHandler extends ClientCommandHandlerBase {
  async handle(context: IClientCommandHandlerContext): Promise<void> {
    const cardId = StorageHelper.tryGet<string>(context.command.parameters, 'cardID');

    if (cardId) {
      await openCard({ cardId });
    }
  }
}
