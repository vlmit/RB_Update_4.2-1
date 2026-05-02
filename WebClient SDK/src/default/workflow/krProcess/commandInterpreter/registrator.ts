import { ExtensionRegistrator } from '@tessa/application';
import { IClientCommandInterpreter$ } from '@tessa/platform';

import { CreateCardViaDocTypeCommandHandler } from './createCardViaDocTypeCommandHandler';
import { CreateCardViaTemplateCommandHandler } from './createCardViaTemplateCommandHandler';
import { OpenCardClientCommandHandler } from './openCardClientCommandHandler';
import { ShowConfirmationDialogClientCommandHandler } from './showConfirmationDialogClientCommandHandler';
import { KrAdvancedDialogCommandHandler } from './krAdvancedDialogCommandHandler';
import { WeAdvancedDialogCommandHandler } from '../../workflowEngine/weAdvancedDialogCommandHandler';

export const CommandInterpreterRegistrator: ExtensionRegistrator = {
  async registerExtensions(container, diContainer) {
    const interpreter = diContainer.get(IClientCommandInterpreter$);

    interpreter.registerHandler(
      container,
      'ShowConfirmationDialog',
      ShowConfirmationDialogClientCommandHandler
    );
    interpreter.registerHandler(
      container,
      'CreateCardViaTemplate',
      CreateCardViaTemplateCommandHandler
    );
    interpreter.registerHandler(
      container,
      'CreateCardViaDocType',
      CreateCardViaDocTypeCommandHandler
    );
    interpreter.registerHandler(container, 'OpenCard', OpenCardClientCommandHandler);
    interpreter.registerHandler(container, 'ShowAdvancedDialog', KrAdvancedDialogCommandHandler);
    interpreter.registerHandler(container, 'WeShowAdvancedDialog', WeAdvancedDialogCommandHandler);
  }
};
