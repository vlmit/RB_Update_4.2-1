import { IStorage, StorageHelper } from '@tessa/core';
import { extension, IExtensionContainer, IExtensionContainer$, inject } from '@tessa/application';
import {
  CardRequestExtension,
  ICardRequestExtensionContext,
  IClientCommandInterpreter,
  IClientCommandInterpreter$,
  KrProcessClientCommand
} from '@tessa/platform';

@extension({ name: 'KrClientCommandCustomExtension' })
export class KrClientCommandCustomExtension extends CardRequestExtension {
  constructor(
    @inject(IExtensionContainer$) private _extensionContainer: IExtensionContainer,
    @inject(IClientCommandInterpreter$) private readonly _interpreter: IClientCommandInterpreter
  ) {
    super();
  }

  async afterRequest(context: ICardRequestExtensionContext): Promise<void> {
    if (context.requestIsSuccessful && context.response) {
      const commands: KrProcessClientCommand[] = [];
      const commandsStorage =
        StorageHelper.tryGet<IStorage | IStorage[]>(
          context.response.info,
          'KrProcessClientCommandInfoMark'
        ) ?? [];
      if (Array.isArray(commandsStorage)) {
        for (const tile of commandsStorage) {
          commands.push(new KrProcessClientCommand(tile));
        }
      } else {
        const names = Object.getOwnPropertyNames(commandsStorage);
        for (const name of names) {
          const tileStorage = commandsStorage[name] as IStorage;
          commands.push(new KrProcessClientCommand(tileStorage));
        }
      }

      await this._interpreter.interpret(this._extensionContainer, commands, context);
    }
  }
}
