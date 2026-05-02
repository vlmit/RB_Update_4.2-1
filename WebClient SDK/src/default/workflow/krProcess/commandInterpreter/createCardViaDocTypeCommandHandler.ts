import { FieldType, StorageAccessor, TypedField } from '@tessa/core';
import { createCard } from 'tessa/ui/uiHost';
import { extension } from '@tessa/application';
import { ClientCommandHandlerBase, IClientCommandHandlerContext } from '@tessa/platform';

@extension({ name: 'CreateCardViaDocTypeCommandHandler' })
export class CreateCardViaDocTypeCommandHandler extends ClientCommandHandlerBase {
  async handle(context: IClientCommandHandlerContext): Promise<void> {
    const command = context.command;
    const accessor = new StorageAccessor(command.parameters);
    const typeId = accessor.tryGetGuid('TypeID');

    if (typeId) {
      const info = {
        ['.NewBilletCard']: TypedField.create(accessor.tryGet<string>('NewCard'), FieldType.Binary),
        ['.NewBilletCardSignature']: TypedField.create(
          accessor.tryGet<string>('NewCardSignature'),
          FieldType.Binary
        )
      };

      const docTypeId = accessor.tryGetGuid('docTypeID');
      const docTypeTitle = accessor.tryGetString('docTypeTitle');

      if (docTypeId && docTypeTitle) {
        info['docTypeID'] = TypedField.createGuid(docTypeId);
        info['docTypeTitle'] = TypedField.createString(docTypeTitle);
      }
      await createCard({
        cardTypeId: typeId,
        info
      });
    }
  }
}
