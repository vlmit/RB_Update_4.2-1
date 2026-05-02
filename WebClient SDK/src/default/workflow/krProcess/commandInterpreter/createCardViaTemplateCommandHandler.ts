import { FieldType, StorageAccessor, TypedField } from '@tessa/core';
import { extension } from '@tessa/application';
import { ClientCommandHandlerBase, IClientCommandHandlerContext } from '@tessa/platform';
import { createFromTemplate } from 'tessa/ui/uiHost';

/**
 * Обработчик клиентской команды CreateCardViaTemplate.
 */
@extension({ name: 'CreateCardViaTemplateCommandHandler' })
export class CreateCardViaTemplateCommandHandler extends ClientCommandHandlerBase {
  async handle(context: IClientCommandHandlerContext): Promise<void> {
    const command = context.command;
    const accessor = new StorageAccessor(command.parameters);
    const templateId = accessor.tryGetGuid('TemplateID');

    if (templateId) {
      // Запоминаем запрос на создание карточки по шаблону, т.к. данный обработчик выполняется только при создании карточки на клиенте без сохранения.
      await createFromTemplate(templateId, {
        info: {
          ['.NewBilletCard']: TypedField.create(
            accessor.tryGet<string>('NewCard'),
            FieldType.Binary
          ),
          ['.NewBilletCardSignature']: TypedField.create(
            accessor.tryGet<string>('NewCardSignature'),
            FieldType.Binary
          )
        },
        saveCreationRequest: true
      });
    }
  }
}
