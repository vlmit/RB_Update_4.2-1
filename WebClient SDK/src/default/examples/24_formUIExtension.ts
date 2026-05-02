import { extension } from '@tessa/application';
import { FormUIExtension, IFormUIExtensionContext, showMessage, UIButton } from 'tessa/ui';
import { IFormWithBlocksViewModel } from 'tessa/ui/cards';

/**
 * Добавляет дополнительные элементы управления
 * и позволяет управлять соответствующими настройками форм диалога.
 *
 * Результат работы расширения:
 * В форму диалога (например, диалоговое окно загрузки Deski) добавляет тестовую кнопку,
 * при нажатии на которую отображается сообщение в диалоговом окне о нажатии кнопки.
 * Также, при закрытии формы диалога, появляется диалоговое окно с сообщением о закрытии формы.
 */
@extension()
export class ExampleFormUIExtension extends FormUIExtension {
  override async initialized(context: IFormUIExtensionContext): Promise<void> {
    console.info(
      `ExampleFormUIExtension.initialized started for ${
        (context.form as IFormWithBlocksViewModel).name
      }`
    );

    // можем задать настроки диалога
    context.dialogOptions = { hideTopCloseIcon: true };

    // есть возможность получить контрол через алиас
    // const someControl = context.model.controls.get('ALIAS') as ButtonViewModel;

    // добавляем новую кнопку с желаемым функционалом в форму
    context.buttons.push(
      UIButton.create({
        caption: 'TEST BTN FROM ExampleFormUIExtension',
        buttonAction: async () =>
          await showMessage('TestButton was clicked!', 'Test', { OKButtonText: 'OK!' })
      })
    );

    // добавляем функционал на закрытие диалогового окна
    context.onDialogClosed = async () => {
      await showMessage('Dialog was closed!', 'Test', { OKButtonText: 'OK!' });
    };
  }
}
