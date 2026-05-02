import { FieldType, StorageHelper } from '@tessa/core';
import { extension, inject } from '@tessa/application';
import { ICardSingletonCache, ICardSingletonCache$ } from '@tessa/platform';
import { CardUIExtension, ICardUIExtensionContext } from 'tessa/ui/cards';

/**
 * Скрывать\показывать вкладку определенной карточки в зависимости:
 * - от наличия какого-то признака в info, пришедшем с сервера.
 * - от значения какого-то справочника, загруженного в init-стриме.
 * - от данных карточки.
 *
 * Результат работы расширения:
 * Для тестовой карточки "Автомобиль" скрываем вкладку "Сравнение файлов" в зависимости от:
 * - от наличия флажка "__HideForm" в info, пришедшем с сервера.
 * - от значения справочника "HideCommentForApprove".
 * - от наличия контрола "Базовый цвет" в тестовой карточке.
 */
@extension()
export class HideFormUIExtension extends CardUIExtension {
  constructor(@inject(ICardSingletonCache$) private _cardSingletonCache: ICardSingletonCache) {
    super();
  }

  override async initialized(context: ICardUIExtensionContext): Promise<void> {
    // пытаемся найти вкладку "Сравнение файлов"
    const testForm = context.model.forms.find(x => x.name === 'Files');
    if (!testForm) {
      return;
    }

    // пытаемся найти флажок в info карточки
    const hideFromInfo =
      StorageHelper.tryGetValue(context.card.info, '__HideForm', FieldType.Boolean) ?? false;

    // пытаемся найти флажок в справочнике
    let hideFromSettings = false;
    const settings = await this._cardSingletonCache.getCard('KrSettings');
    if (settings) {
      const section = settings.sections.tryGet('KrSettings');
      if (section) {
        hideFromSettings = !!section.fields.get('HideCommentForApprove');
      }
    }

    // смотрим на флажок в данных карточки
    let hideFromCard = false;
    const additionalInfo = context.card.sections.tryGet('AbCarAdditionalInfo');
    if (additionalInfo) {
      hideFromCard = !!additionalInfo.fields.get('IsBaseColor');
    }

    // скрываем или показываем вкладку
    testForm.isCollapsed = hideFromInfo || hideFromSettings || hideFromCard;
  }
}
