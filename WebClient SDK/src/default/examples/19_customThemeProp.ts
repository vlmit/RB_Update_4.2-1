import { extension } from '@tessa/application';
import { ApplicationExtension, IApplicationExtensionMetadataContext } from 'tessa';
import { ThemeManager } from 'tessa/ui/themes';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';
import { TextBoxViewModel } from 'tessa/ui/cards/controls';

/**
 * Добавляем дополнительное свойство в темы
 * в расширении ApplicationExtension с последующим использованием для выбранного типа карточки.
 *
 * Результат работы расширения:
 * Данное расширение добавляет дополнительное свойство "MyCustomBackgroundProp" в настройки текущей темы,
 * которое, в свою очередь, устанавливается в качестве фона для заголовка контрола "Марка автомобиля"
 * тестовой карточки "Автомобиль".
 */
@extension()
export class CustomThemePropApplicationExtension extends ApplicationExtension {
  override async afterMetadataReceived(
    _context: IApplicationExtensionMetadataContext
  ): Promise<void> {
    // проверяем наличие инициализированной темы
    if (!ThemeManager.instance.isInitialized) {
      return;
    }

    // в зависимости от названия темы добавляем соответствующий цвет в ее настройки
    for (const [name, theme] of ThemeManager.instance.themes) {
      const backgroundColor =
        name === 'Cold' ? 'rgba(150, 150, 150, 0.5)' : 'rgba(66, 88, 111, 0.75)';
      theme.settings.common['MyCustomBackgroundProp'] = backgroundColor;
    }
  }
}

@extension()
export class CustomThemePropUIExtension extends CardUIExtension {
  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся найти контрол "Марка автомобиля"
    const carNameControl = context.model.controls.get('CarName') as TextBoxViewModel;
    if (!carNameControl) {
      return;
    }

    // для заголовка полученного контрола устанавливаем цвет фона, добавленный ранее в настроки темы
    carNameControl.captionStyle.add(
      css => css`
        background: ${ThemeManager.instance.currentTheme.settings.common['MyCustomBackgroundProp']};
      `
    );
  }
}
