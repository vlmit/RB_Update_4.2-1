import { extension } from '@tessa/application';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { ExampleHeaderViewModel } from './31_exampleHeaderViewModel';

@extension()
export class ExampleHeaderUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // создаем вью-модель для хэдера и устанавливаем ее в соответствующее свойство модели карточки
    context.model.header = new ExampleHeaderViewModel('Добро пожаловать в карточку автомобиля.');
  }
}
